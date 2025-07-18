namespace GbxTools3D.Health;

public sealed class MxTM2020HealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://trackmania.exchange/";
}
