namespace GbxTools3D.Health;

public sealed class TmIoHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://trackmania.io/";
}
