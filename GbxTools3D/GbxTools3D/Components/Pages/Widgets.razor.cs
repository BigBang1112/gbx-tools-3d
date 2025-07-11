using System.Text;

namespace GbxTools3D.Components.Pages;

public partial class Widgets
{
    private string selectedExternal = "tmx";
    private string selectedTmx = "TMNF";
    private string selectedMx = "tm2";
    private string mapId = "6276734";
    private string replayId = "9932884";

    private string selectedGame = "TMF";
    private string selectedContentType = "collections";
    private string environment = "Stadium";
    private string selectedAssetType = "blocks";
    private string assetName = "StadiumRoadMainCheckpoint";
    private string sceneName = "";
    private bool noCatalog;

    private int width = 800;
    private int height = 450;

    private string GetViewMapFrameRelativeUrl()
    {
        var sb = new StringBuilder("view/map?");

        switch (selectedExternal)
        {
            case "tmx":
                sb.Append($"tmx={selectedTmx}");
                break;
            case "mx":
                sb.Append($"mx={selectedMx}");
                break;
            default:
                throw new InvalidOperationException("Invalid external type selected.");
        }

        if (!string.IsNullOrWhiteSpace(mapId))
        {
            sb.Append($"&id={mapId}");
        }

        return sb.ToString();
    }

    private string GetViewReplayFrameRelativeUrl()
    {
        var sb = new StringBuilder("view/replay?");

        switch (selectedExternal)
        {
            case "tmx":
                sb.Append($"tmx={selectedTmx}");
                break;
            case "mx":
                sb.Append($"mx={selectedMx}");
                break;
            default:
                throw new InvalidOperationException("Invalid external type selected.");
        }

        if (!string.IsNullOrWhiteSpace(replayId))
        {
            sb.Append($"&id={replayId}");
        }

        if (!string.IsNullOrWhiteSpace(mapId))
        {
            sb.Append($"&mapid={mapId}");
        }

        return sb.ToString();
    }

    private string GetCatalogFrameRelativeUrl()
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

        sb.Append($"&nocatalog=true");

        return sb.ToString();
    }

    private string GetFrameUrl(string relativeUrl)
    {
        return $"{NavManager.BaseUri}{relativeUrl}";
    }

    private string GetFrameHtml(string relativeUrl)
    {
        return $"""<iframe src="{GetFrameUrl(relativeUrl)}" width="{width}" height="{height}"></iframe>""";
    }
}
