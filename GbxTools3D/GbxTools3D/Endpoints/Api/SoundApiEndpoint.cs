using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Enums;
using GbxTools3D.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class SoundApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetSoundInfo)
            .CacheOutput(x => x.Tag("sound"));
        group.MapGet("/{hash}", GetSoundByHash); // cache output questionable due to larger memory usage
    }

    private static async Task<Ok<SoundInfoDto>> GetSoundInfo(AppDbContext db, CancellationToken cancellationToken)
    {
        var count = await db.Sounds.CountAsync(cancellationToken);

        return TypedResults.Ok(new SoundInfoDto
        {
            Count = count
        });
    }

    private static readonly Func<AppDbContext, string, Task<SoundHiddenData?>> SoundFirstOrDefaultAsync = EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Sounds
        .Select(x => new SoundHiddenData { Hash = x.Hash, Data = x.Data, UpdatedAt = x.UpdatedAt, Type = x.Type })
        .FirstOrDefault(x => x.Hash == hash));

    private static async Task<Results<FileContentHttpResult, NotFound>> GetSoundByHash(
        AppDbContext db,
        string hash,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var sound = await SoundFirstOrDefaultAsync(db, hash);

        if (sound is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        var (mimeType, ext) = sound.Type switch
        {
            SoundType.Ogg => ("audio/ogg", "ogg"),
            SoundType.Wav => ("audio/wav", "wav"),
            _ => throw new NotSupportedException($"Sound type {sound.Type} is not supported")
        };

        return TypedResults.File(sound.Data, mimeType, $"{hash}.{ext}", lastModified: sound.UpdatedAt);
    }

    private sealed class SoundHiddenData : CacheableHiddenData
    {
        public required SoundType Type { get; init; }
    }
}