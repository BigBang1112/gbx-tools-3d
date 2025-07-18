
namespace GbxTools3D.Health;

public sealed class TmxNationsHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://nations.tm-exchange.com/";
}
