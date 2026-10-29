using Microsoft.Extensions.Caching.Hybrid;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace GbxTools3D.Endpoints;

public static class BlenderEndpoint
{
    private const string Repository = "BigBang1112/blender-gbx-tools";
    private const string AssetName = "BlenderGbxTools.zip";
    private const string ManifestPath = "BlenderPlugin/blender_manifest.toml";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/index.json", GetIndex);
    }

    private static async Task<IResult> GetIndex(
        IHttpClientFactory httpFactory,
        HybridCache cache,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var index = await cache.GetOrCreateAsync("blender:index", async token =>
        {
            var http = httpFactory.CreateClient("github");
            return await CreateIndexAsync(http, token);
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";
        return TypedResults.Text(index, "application/json");
    }

    /// <summary>
    /// Builds the Blender extension repository index from the latest GitHub release: the manifest at the release tag, plus the release archive details.
    /// </summary>
    private static async Task<string> CreateIndexAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var release = await http.GetFromJsonAsync<JsonElement>($"https://api.github.com/repos/{Repository}/releases/latest", cancellationToken);
        var tag = release.GetProperty("tag_name").GetString()!;
        var asset = release.GetProperty("assets").EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("name").GetString() == AssetName);

        if (asset.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Release {tag} does not contain {AssetName}.");
        }

        var archiveUrl = asset.GetProperty("browser_download_url").GetString()!;
        var archiveSize = asset.GetProperty("size").GetInt64();

        // GitHub provides the digest for newer releases only
        var archiveHash = asset.TryGetProperty("digest", out var digest) && digest.ValueKind == JsonValueKind.String
            ? digest.GetString()!
            : await ComputeArchiveHashAsync(http, archiveUrl, cancellationToken);

        var manifestToml = await http.GetStringAsync($"https://raw.githubusercontent.com/{Repository}/{tag}/{ManifestPath}", cancellationToken);
        var manifest = TomlSerializer.Deserialize<TomlTable>(manifestToml)!;
        manifest.Remove("build");

        var entry = (JsonObject)ToJsonNode(manifest)!;
        entry["archive_url"] = archiveUrl;
        entry["archive_size"] = archiveSize;
        entry["archive_hash"] = archiveHash;

        var index = new JsonObject
        {
            ["version"] = "v1",
            ["blocklist"] = new JsonArray(),
            ["data"] = new JsonArray(entry)
        };

        return index.ToJsonString();
    }

    private static async Task<string> ComputeArchiveHashAsync(HttpClient http, string archiveUrl, CancellationToken cancellationToken)
    {
        await using var stream = await http.GetStreamAsync(archiveUrl, cancellationToken);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }

    private static JsonNode? ToJsonNode(object? value) => value switch
    {
        TomlTable table => new JsonObject(table.Select(x => KeyValuePair.Create(x.Key, ToJsonNode(x.Value)))),
        TomlArray array => new JsonArray(array.Select(ToJsonNode).ToArray()),
        _ => JsonSerializer.SerializeToNode(value)
    };
}
