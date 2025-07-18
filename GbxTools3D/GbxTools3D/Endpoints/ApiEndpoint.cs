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
        BlockApiEndpoint.Map(group.MapGroup("/blocks"));
        DecorationApiEndpoint.Map(group.MapGroup("/decorations"));
        MaterialApiEndpoint.Map(group.MapGroup("/materials"));
        TextureApiEndpoint.Map(group.MapGroup("/texture"));
        DataImportApiEndpoint.Map(group.MapGroup("/dataimport"));
        CollectionApiEndpoint.Map(group.MapGroup("/collections"));
        IconApiEndpoint.Map(group.MapGroup("/icon"));
        VehicleApiEndpoint.Map(group.MapGroup("/vehicles"));
        SoundApiEndpoint.Map(group.MapGroup("/sound"));
        SkinApiEndpoint.Map(group.MapGroup("/skin"));
        GhostApiEndpoint.Map(group.MapGroup("/ghost"));
    }
}
