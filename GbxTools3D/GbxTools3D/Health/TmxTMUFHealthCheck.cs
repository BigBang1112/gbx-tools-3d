
namespace GbxTools3D.Health;

public sealed class TmxTMUFHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://tmuf.exchange/";
}
