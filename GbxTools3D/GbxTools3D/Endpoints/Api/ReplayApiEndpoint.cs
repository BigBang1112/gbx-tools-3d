using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Enums;
using GbxTools3D.External;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class ReplayApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/tmx/{site}/{replayId}", GetReplayFromTmx)
            .RequireRateLimiting("fixed-external-downloads");
        group.MapGet("/tmx/{site}/{replayId}/{trackId}", GetReplayFromTmxWithMapInfo)
            .RequireRateLimiting("fixed-external-downloads");
        group.MapGet("/mx/{site}/{replayId}", GetReplayFromMx)
            .RequireRateLimiting("fixed-external-downloads");
        group.MapGet("/mx/{site}/{replayId}/{mapId}", GetReplayFromMxWithMapInfo)
            .RequireRateLimiting("fixed-external-downloads");
        // tmuf/userid/mapuid
    }

    private static async Task<Results<Ok<ReplayContentDto>, NotFound, StatusCodeHttpResult>> GetReplayFromTmx(
        HttpContext context,
        HttpClient http,
        TmxSite site,
        ulong replayId,
        CancellationToken cancellationToken)
    {
        var siteUrl = ExternalUtils.GetSiteUrl(site);

        using var response = await http.GetAsync($"https://{siteUrl}/recordgbx/{replayId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        response.EnsureSuccessStatusCode();

        var etag = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{site}-{replayId}-{response.Content.Headers.ContentLength}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var replayData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new ReplayContentDto
        {
            Content = replayData
        });
    }

    private static async Task<Results<Ok<ReplayContentDto>, NotFound, StatusCodeHttpResult>> GetReplayFromTmxWithMapInfo(
        HttpContext context,
        AppDbContext db,
        IHttpClientFactory httpFactory,
        TmxSite site,
        ulong replayId,
        ulong trackId,
        CancellationToken cancellationToken)
    {
        var siteUrl = ExternalUtils.GetSiteUrl(site);

        var http = httpFactory.CreateClient("exchange");

        using var trackInfoResponseTask = http.GetAsync($"https://{siteUrl}/api/tracks?id={trackId}&fields=TrackId%2CTrackName%2CUploader.UserId%2CUploader.Name%2CAuthors%5B%5D%2CUpdatedAt%2CUnlimiterVersion", cancellationToken);
        using var replayInfoResponseTask = http.GetAsync($"https://{siteUrl}/api/replays?trackId={trackId}&from={replayId}&count=1&fields=User.UserId%2CUser.Name%2CPosition%2CReplayAt", cancellationToken);
        using var replayResponse = await http.GetAsync($"https://{siteUrl}/recordgbx/{replayId}", cancellationToken);

        if (replayResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        replayResponse.EnsureSuccessStatusCode();

        var replayContentLength = replayResponse.Content.Headers.ContentLength;

        var etag = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{site}-{replayId}-{replayContentLength}-{trackId}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var replayData = await replayResponse.Content.ReadAsByteArrayAsync(cancellationToken);

        using var trackInfoResponse = await trackInfoResponseTask;

        var mapInfoDto = default(MapInfoDto);

        if (trackInfoResponse.IsSuccessStatusCode)
        {
            var trackInfo = await trackInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.MxResponseTmxTrackInfo, cancellationToken);

            if (trackInfo?.Results.Length > 0)
            {
                var track = trackInfo.Results[0];

                mapInfoDto = new MapInfoDto
                {
                    Name = track.TrackName,
                    UploaderId = track.Uploader.UserId.ToString(),
                    UploaderName = track.Uploader.Name,
                    UpdatedAt = track.UpdatedAt,
                    Unlimiter = track.UnlimiterVersion,
                    OnlineMapId = null,
                    MxId = track.TrackId,
                    // TODO: add authors
                };
            }
        }

        using var replayInfoResponse = await replayInfoResponseTask;

        var replayInfoDto = default(ReplayInfoDto);

        if (replayInfoResponse.IsSuccessStatusCode)
        {
            var replayInfoData = await replayInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.MxResponseTmxReplayInfo, cancellationToken);
            if (replayInfoData?.Results.Length > 0)
            {
                var replay = replayInfoData.Results[0];
                replayInfoDto = new ReplayInfoDto
                {
                    UploaderId = replay.User.UserId.ToString(),
                    UploaderName = replay.User.Name,
                    Position = replay.Position,
                    UploadedAt = replay.ReplayAt
                };
            }
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new ReplayContentDto
        {
            Map = mapInfoDto,
            Replay = replayInfoDto,
            Content = replayData
        });
    }

    private static async Task<Results<Ok<ReplayContentDto>, NotFound, StatusCodeHttpResult>> GetReplayFromMx(
        HttpContext context,
        IHttpClientFactory httpFactory,
        MxSite site,
        ulong replayId,
        CancellationToken cancellationToken)
    {
        var siteUrl = ExternalUtils.GetSiteUrl(site);

        var http = httpFactory.CreateClient("exchange");

        using var response = await http.GetAsync($"https://{siteUrl}/recordgbx/{replayId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        response.EnsureSuccessStatusCode();

        var etag = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{site}-{replayId}-{response.Content.Headers.ContentLength}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var replayData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new ReplayContentDto
        {
            Content = replayData
        });
    }

    private static async Task<Results<Ok<ReplayContentDto>, NotFound, StatusCodeHttpResult>> GetReplayFromMxWithMapInfo(
        HttpContext context,
        AppDbContext db,
        IHttpClientFactory httpFactory,
        MxSite site,
        ulong replayId,
        ulong mapId,
        CancellationToken cancellationToken)
    {
        var siteUrl = ExternalUtils.GetSiteUrl(site);

        var http = httpFactory.CreateClient("exchange");

        using var mapInfoResponseTask = http.GetAsync($"https://{siteUrl}/api/maps?id={mapId}&fields=MapId%2CName%2CUploader.UserId%2CUploader.Name%2CAuthors%5B%5D%2CUpdatedAt%2COnlineMapId", cancellationToken);
        using var replayInfoResponseTask = http.GetAsync($"https://{siteUrl}/api/replays?mapId={mapId}&from={replayId}&count=1&fields=User.UserId%2CUser.Name%2CPosition%2CReplayAt", cancellationToken);
        using var replayResponse = await http.GetAsync($"https://{siteUrl}/recordgbx/{replayId}", cancellationToken);

        if (replayResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        replayResponse.EnsureSuccessStatusCode();

        var replayContentLength = replayResponse.Content.Headers.ContentLength;

        var etag = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{site}-{replayId}-{replayContentLength}-{mapId}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var replayData = await replayResponse.Content.ReadAsByteArrayAsync(cancellationToken);

        using var mapInfoResponse = await mapInfoResponseTask;

        var mapInfoDto = default(MapInfoDto);

        if (mapInfoResponse.IsSuccessStatusCode)
        {
            var mapInfo = await mapInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.MxResponseMxMapInfo, cancellationToken);

            if (mapInfo?.Results.Length > 0)
            {
                var map = mapInfo.Results[0];

                mapInfoDto = new MapInfoDto
                {
                    Name = map.Name,
                    UploaderId = map.Uploader.UserId.ToString(),
                    UploaderName = map.Uploader.Name,
                    UpdatedAt = map.UpdatedAt,
                    Unlimiter = null,
                    OnlineMapId = map.OnlineMapId,
                    MxId = map.MapId
                    // TODO: add authors
                };
            }
        }

        using var replayInfoResponse = await replayInfoResponseTask;

        var replayInfoDto = default(ReplayInfoDto);

        if (replayInfoResponse.IsSuccessStatusCode)
        {
            var replayInfoData = await replayInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.MxResponseTmxReplayInfo, cancellationToken);
            if (replayInfoData?.Results.Length > 0)
            {
                var replay = replayInfoData.Results[0];
                replayInfoDto = new ReplayInfoDto
                {
                    UploaderId = replay.User.UserId.ToString(),
                    UploaderName = replay.User.Name,
                    Position = replay.Position,
                    UploadedAt = replay.ReplayAt
                };
            }
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new ReplayContentDto
        {
            Map = mapInfoDto,
            Replay = replayInfoDto,
            Content = replayData
        });
    }
}
