namespace GbxTools3D.Client;

public static class TmxUtils
{
    public static string? GetExchangeSite(string? tmxSite, string? mxSite, string exchangeId)
    {
        if (!string.IsNullOrEmpty(tmxSite))
        {
            return tmxSite switch
            {
                "TMNF" => "tmnf.exchange",
                "TMUF" => "tmuf.exchange",
                "Nations" => "nations.tm-exchange.com",
                "Sunrise" => "sunrise.tm-exchange.com",
                "Original" => "original.tm-exchange.com",
                _ => null
            } + "/trackshow/" + exchangeId;
        }

        if (!string.IsNullOrEmpty(mxSite))
        {
            return mxSite switch
            {
                "TM2" => "tm.mania",
                "SM" => "sm.mania",
                "TM2020" => "trackmania",
                _ => null
            } + ".exchange/mapshow/" + exchangeId;
        }

        return null;
    }

    public static string? GetExchangeGlowClass(string? tmxSite, string? mxSite)
    {
        if (!string.IsNullOrEmpty(tmxSite))
        {
            return "tmx";
        }

        if (!string.IsNullOrEmpty(mxSite))
        {
            return mxSite switch
            {
                "TM2" or "SM" => "mp",
                "TM2020" => "tm2020x",
                _ => null
            };
        }

        return null;
    }

    public static string? GetExchangeImage(string? tmxSite, string? mxSite)
    {
        if (!string.IsNullOrEmpty(tmxSite))
        {
            return "tmx";
        }

        if (!string.IsNullOrEmpty(mxSite))
        {
            return mxSite switch
            {
                "TM2" or "SM" => "mx",
                "TM2020" => "tm2020x",
                _ => null
            };
        }

        return null;
    }
}
