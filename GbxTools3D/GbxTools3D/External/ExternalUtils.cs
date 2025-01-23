using GbxTools3D.Enums;

namespace GbxTools3D.External;

internal static class ExternalUtils
{
    public static string GetSiteUrl(TmxSite site) => site switch
    {
        TmxSite.TMNF => "tmnf.exchange",
        TmxSite.TMUF => "tmuf.exchange",
        TmxSite.Nations => "nations.tm-exchange.com",
        TmxSite.Sunrise => "sunrise.tm-exchange.com",
        TmxSite.Original => "original.tm-exchange.com",
        _ => throw new ArgumentOutOfRangeException(nameof(site), site, "Invalid site")
    };

    public static string GetSiteUrl(MxSite site) => site switch
    {
        MxSite.TM2 => "tm.mania.exchange",
        MxSite.SM => "sm.mania.exchange",
        MxSite.TM2020 => "trackmania.exchange",
        _ => throw new ArgumentOutOfRangeException(nameof(site), site, "Invalid site")
    };
}
