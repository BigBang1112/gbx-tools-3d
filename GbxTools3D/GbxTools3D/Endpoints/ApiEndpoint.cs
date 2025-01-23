using GbxTools3D.Endpoints.Api;

namespace GbxTools3D.Endpoints;

public static class ApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Welcome to 3D Gbx Tools API!"
            });
        });

        MeshApiEndpoint.Map(group.MapGroup("/mesh"));
        ReplayApiEndpoint.Map(group.MapGroup("/replay"));
        MapApiEndpoint.Map(group.MapGroup("/map"));
        ItemApiEndpoint.Map(group.MapGroup("/item"));
    }
}
