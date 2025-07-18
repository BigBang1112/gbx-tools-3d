using System.Text;

namespace GbxTools3D.Components.Pages;

public partial class Widgets
{
    private string selectedExternal = "tmx";
    private string selectedTmx = "TMNF";
    private string selectedMx = "tm2";
    private string mapId = "6276734";
    private string replayId = "9932884";

    private string maniaParkId = "FMSqt-WKVUSa18UzGo_m5A";
    private string selectedGame = "TMF";
    private string vehicleName = "";

    private string selectedContentType = "collections";
    private string environment = "Stadium";
    private string selectedAssetType = "blocks";
    private string assetName = "StadiumRoadMainCheckpoint";
    private string sceneName = "";

    private int width = 800;
    private int height = 450;

    private string GetViewMapRelativeUrl()
    {
        var sb = new StringBuilder("view/map?");

        switch (selectedExternal)
        {
            case "tmx":
                sb.Append("tmx=");
                sb.Append(selectedTmx);
                break;
            case "mx":
                sb.Append("mx=");
                sb.Append(selectedMx);
                break;
            default:
                throw new InvalidOperationException("Invalid external type selected.");
        }

        if (!string.IsNullOrWhiteSpace(mapId))
        {
            sb.Append("&id=");
            sb.Append(mapId);
        }

        return sb.ToString();
    }

    private string GetViewReplayRelativeUrl()
    {
        var sb = new StringBuilder("view/replay?");

        switch (selectedExternal)
        {
            case "tmx":
                sb.Append("tmx=");
                sb.Append(selectedTmx);
                break;
            case "mx":
                sb.Append("mx=");
                sb.Append(selectedMx);
                break;
            default:
                throw new InvalidOperationException("Invalid external type selected.");
        }

        if (!string.IsNullOrWhiteSpace(replayId))
        {
            sb.Append("&id=");
            sb.Append(replayId);
        }

        if (!string.IsNullOrWhiteSpace(mapId))
        {
            sb.Append("&mapid=");
            sb.Append(mapId);
        }

        return sb.ToString();
    }

    private string GetViewSkinRelativeUrl()
    {
        var sb = new StringBuilder("view/skin?");

        sb.Append("mp=");
        sb.Append(maniaParkId);

        if (!string.IsNullOrWhiteSpace(selectedGame))
        {
            sb.Append("&game=");
            sb.Append(selectedGame);
        }

        if (!string.IsNullOrWhiteSpace(vehicleName))
        {
            sb.Append("&vehicle=");
            sb.Append(vehicleName);
        }

        return sb.ToString();
    }

    private string GetCatalogRelativeUrl(bool isFrame)
    {
        var sb = new StringBuilder("catalog/");
        sb.Append(selectedGame);
        sb.Append('/');
        sb.Append(selectedContentType);
        sb.Append('/');
        sb.Append(environment);
        sb.Append('/');
        sb.Append(selectedAssetType);
        sb.Append("?selected=");
        sb.Append(assetName);

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            sb.Append($"&scene={sceneName}");
        }

        if (isFrame)
        {
            sb.Append($"&nocatalog=true");
        }

        return sb.ToString();
    }

    private string GetFullUrl(string relativeUrl)
    {
        return $"{NavManager.BaseUri}{relativeUrl}";
    }

    private string GetFrameHtml(string relativeUrl)
    {
        return $"""<iframe src="{GetFullUrl(relativeUrl)}" width="{width}" height="{height}"></iframe>""";
    }

    private string GetTmxTrackLink(string trackId) => selectedTmx switch
    {
        "TMNF" => $"https://tmnf.exchange/trackshow/{trackId}",
        "TMUF" => $"https://tmuf.exchange/trackshow/{trackId}",
        "Nations" => $"https://nations.tm-exchange.com/trackshow/{trackId}",
        "Sunrise" => $"https://sunrise.tm-exchange.com/trackshow/{trackId}",
        "Original" => $"https://original.tm-exchange.com/trackshow/{trackId}",
        _ => "#"
    };

    private string GetMxMapLink(string mapId) => selectedMx switch
    {
        "TM2020" => $"https://trackmania.exchange/mapshow/{mapId}",
        "TM2" => $"https://tm.mania.exchange/mapshow/{mapId}",
        "SM" => $"https://sm.mania.exchange/mapshow/{mapId}",
        _ => "#"
    };
}
