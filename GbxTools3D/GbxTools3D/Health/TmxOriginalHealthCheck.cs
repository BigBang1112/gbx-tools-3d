
namespace GbxTools3D.Health;

public sealed class TmxOriginalHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://original.tm-exchange.com/";
}
