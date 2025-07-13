using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Enums;
using GbxTools3D.External;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class MapApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/tmx/{site}/{trackId}", GetMapFromTmx)
            .RequireRateLimiting("fixed-external-downloads");
        group.MapGet("/mx/{site}/{mapId}", GetMapFromMx)
            .RequireRateLimiting("fixed-external-downloads");
    }

    private static async Task<Results<Ok<MapContentDto>, NotFound, StatusCodeHttpResult>> GetMapFromTmx(
        HttpContext context,
        IHttpClientFactory httpFactory,
        TmxSite site,
        ulong trackId,
        CancellationToken cancellationToken)
    {
        var siteUrl = ExternalUtils.GetSiteUrl(site);

        var http = httpFactory.CreateClient("exchange");

        using var trackInfoResponseTask = http.GetAsync($"https://{siteUrl}/api/tracks?id={trackId}&fields=TrackName%2CUploader.UserId%2CUploader.Name%2CAuthors%5B%5D%2CUpdatedAt%2CUnlimiterVersion", cancellationToken);
        using var trackResponse = await http.GetAsync($"https://{siteUrl}/trackgbx/{trackId}", cancellationToken);

        if (trackResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        trackResponse.EnsureSuccessStatusCode();

        var trackContentLength = trackResponse.Content.Headers.ContentLength;

        var etag = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{site}-{trackId}-{trackContentLength}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var trackData = await trackResponse.Content.ReadAsByteArrayAsync(cancellationToken);

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
                    OnlineMapId = null
                    // TODO: add authors
                };
            }
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new MapContentDto
        {
            Map = mapInfoDto,
            Content = trackData
        });
    }

    private static async Task<Results<Ok<MapContentDto>, NotFound, StatusCodeHttpResult>> GetMapFromMx(
        HttpContext context,
        IHttpClientFactory httpFactory,
        MxSite site,
        ulong mapId,
        CancellationToken cancellationToken)
    {
        var siteUrl = ExternalUtils.GetSiteUrl(site);

        var http = httpFactory.CreateClient("exchange");

        using var mapInfoResponseTask = http.GetAsync($"https://{siteUrl}/api/maps?id={mapId}&fields=Name%2CUploader.UserId%2CUploader.Name%2CAuthors%5B%5D%2CUpdatedAt%2COnlineMapId", cancellationToken);
        using var mapResponse = await http.GetAsync($"https://{siteUrl}/mapgbx/{mapId}", cancellationToken);

        if (mapResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        mapResponse.EnsureSuccessStatusCode();

        var mapContentLength = mapResponse.Content.Headers.ContentLength;

        var etag =  Convert.ToBase64String(Encoding.ASCII.GetBytes($"{site}-{mapId}-{mapContentLength}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var mapData = await mapResponse.Content.ReadAsByteArrayAsync(cancellationToken);

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
                    OnlineMapId = map.OnlineMapId
                    // TODO: add authors
                };
            }
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new MapContentDto
        {
            Map = mapInfoDto,
            Content = mapData
        });
    }
}
