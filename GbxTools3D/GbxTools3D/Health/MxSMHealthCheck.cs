namespace GbxTools3D.Health;

public sealed class MxSMHealthCheck(HttpClient http) : HttpDegradedHealthCheck(http)
{
    public override string Url => "https://sm.mania.exchange/";
}
