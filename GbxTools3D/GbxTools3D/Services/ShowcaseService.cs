using GbxTools3D.Data;
using GbxTools3D.Enums;
using GbxTools3D.External;
using GbxTools3D.Models;
using ManiaAPI.TMX;
using System.Collections.Immutable;

namespace GbxTools3D.Services;

internal sealed class ShowcaseService
{
    private readonly ImmutableDictionary<ManiaAPI.TMX.TmxSite, TMX> tmxs;
    private readonly IHttpClientFactory httpFactory;
    private readonly ILogger<ShowcaseService> logger;

    public ShowcaseService(ImmutableDictionary<ManiaAPI.TMX.TmxSite, TMX> tmxs, IHttpClientFactory httpFactory, ILogger<ShowcaseService> logger)
    {
        this.tmxs = tmxs;
        this.httpFactory = httpFactory;
        this.logger = logger;
    }

    public async Task<List<Showcase>> CreateDailyMapShowcaseAsync(CancellationToken cancellationToken)
    {
        var showcases = new List<Showcase>();

        var http = httpFactory.CreateClient("exchange");

        var tmnfResponseTask = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://tmnf.exchange/trackrandom"), cancellationToken);
        var tmufResponseTask = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://tmuf.exchange/trackrandom"), cancellationToken);
        var tm2ResponseTask = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://tm.mania.exchange/maprandom"), cancellationToken);
        var smResponseTask = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://sm.mania.exchange/maprandom"), cancellationToken);
        var tm2020ResponseTask = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://trackmania.exchange/maprandom"), cancellationToken);

        try
        {
            var showcase = await CreateTmxMapShowcaseAsync(Enums.TmxSite.TMNF, http, tmnfResponseTask, cancellationToken);
            if (showcase is not null)
            {
                showcases.Add(showcase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create TMNF showcase");
        }

        try
        {
            var showcase = await CreateTmxMapShowcaseAsync(Enums.TmxSite.TMUF, http, tmufResponseTask, cancellationToken);
            if (showcase is not null)
            {
                showcases.Add(showcase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create TMUF showcase");
        }

        try
        {
            var showcase = await CreateMxMapShowcaseAsync(MxSite.TM2, http, tm2ResponseTask, "tm.mania", cancellationToken);
            if (showcase is not null)
            {
                showcases.Add(showcase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create TM2 showcase");
        }

        try
        {
            var showcase = await CreateMxMapShowcaseAsync(MxSite.SM, http, smResponseTask, "sm.mania", cancellationToken);
            if (showcase is not null)
            {
                showcases.Add(showcase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create SM showcase");
        }

        try
        {
            var showcase = await CreateMxMapShowcaseAsync(MxSite.TM2020, http, tm2020ResponseTask, "trackmania", cancellationToken);
            if (showcase is not null)
            {
                showcases.Add(showcase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create TM2020 showcase");
        }

        return showcases;
    }

    private static async Task<Showcase?> CreateTmxMapShowcaseAsync(Enums.TmxSite site, HttpClient http, Task<HttpResponseMessage> responseTask, CancellationToken cancellationToken)
    {
        var trackId = (await responseTask).RequestMessage?.RequestUri?.Segments.LastOrDefault();
        var siteUrl = ExternalUtils.GetSiteUrl(site);
        var trackInfoResponse = await http.GetAsync($"https://{siteUrl}/api/tracks?id={trackId}&fields=TrackId%2CTrackName%2CUploader.UserId%2CUploader.Name%2CAuthors%5B%5D%2CUpdatedAt%2CUnlimiterVersion", cancellationToken);
        var trackInfos = await trackInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.MxResponseTmxTrackInfo, cancellationToken);
        
        if (trackInfos?.Results.Length > 0)
        {
            var siteStr = site.ToString();
            var trackInfo = trackInfos.Results[0];
            return new Showcase(
                siteStr,
                trackInfo.TrackName, 
                trackInfo.Uploader.Name, 
                $"view/map?tmx={siteStr}&id={trackId}", 
                $"https://{siteStr.ToLowerInvariant()}.exchange/trackshow/{trackId}/image/1");
        }

        return null;
    }

    private static async Task<Showcase?> CreateMxMapShowcaseAsync(MxSite site, HttpClient http, Task<HttpResponseMessage> responseTask, string prefix, CancellationToken cancellationToken)
    {
        var mapId = (await responseTask).RequestMessage?.RequestUri?.Segments.LastOrDefault();
        var siteUrl = ExternalUtils.GetSiteUrl(site);
        using var mapInfoResponse = await http.GetAsync($"https://{siteUrl}/api/maps?id={mapId}&fields=MapId%2CName%2CUploader.UserId%2CUploader.Name%2CAuthors%5B%5D%2CUpdatedAt%2COnlineMapId", cancellationToken);
        var mapInfos = await mapInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.MxResponseMxMapInfo, cancellationToken);
        
        if (mapInfos?.Results.Length > 0)
        {
            var siteStr = site.ToString();
            var mapInfo = mapInfos.Results[0];
            return new Showcase(
                siteStr, 
                mapInfo.Name,
                mapInfo.Uploader.Name, 
                $"view/map?mx={siteStr}&id={mapId}", 
                $"https://{prefix}.exchange/mapimage/{mapId}/1");
        }

        return null;
    }

    public async Task<List<Showcase>> CreateLatestWorldRecordsShowcaseAsync(ManiaAPI.TMX.TmxSite site, CancellationToken cancellationToken)
    {
        var exchange = tmxs[site];
        
        var showcases = new List<Showcase>();

        var wrs = await exchange.SearchTracksAsync(new()
        {
            Count = 5,
            LbType = LeaderboardType.Nadeo,
            Order1 = TrackOrder.WorldRecordSetMostRecent,
            PrimaryType = TrackType.Race,
        }, cancellationToken);

        var siteStr = site.ToString();

        foreach (var wr in wrs.Results)
        {
            showcases.Add(new Showcase(
                siteStr, 
                $"{wr.WRReplay?.ReplayTime.ToString(useHundredths: true)} on {wr.TrackName}", 
                wr.WRReplay?.User.Name ?? "", 
                $"view/replay?tmx={site}&id={wr.WRReplay?.ReplayId}&mapid={wr.TrackId}",
                $"https://{siteStr.ToLowerInvariant()}.exchange/trackshow/{wr.TrackId}/image/1"));
        }

        return showcases;
    }
}
