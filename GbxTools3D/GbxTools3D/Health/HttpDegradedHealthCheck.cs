using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GbxTools3D.Health;

public abstract class HttpDegradedHealthCheck(HttpClient http) : IHealthCheck
{
    private readonly HttpClient http = http;

    public abstract string Url { get; }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(Url, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return HealthCheckResult.Healthy();
        }

        return HealthCheckResult.Degraded();
    }
}
