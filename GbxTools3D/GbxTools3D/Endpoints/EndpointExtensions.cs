namespace GbxTools3D.Endpoints;

public static class EndpointExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        ApiEndpoint.Map(app.MapGroup("/api"));
    }
}
