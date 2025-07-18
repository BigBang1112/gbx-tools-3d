
namespace GbxTools3D.Health;

public sealed class TmxSunriseHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://sunrise.tm-exchange.com/";
}
