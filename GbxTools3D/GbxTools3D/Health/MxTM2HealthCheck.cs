namespace GbxTools3D.Health;

public sealed class MxTM2HealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://tm.mania.exchange/";
}
