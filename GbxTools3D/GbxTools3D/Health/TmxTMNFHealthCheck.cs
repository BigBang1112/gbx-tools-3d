
namespace GbxTools3D.Health;

public sealed class TmxTMNFHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://tmnf.exchange/";
}
