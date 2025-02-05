using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Enums;
using GbxTools3D.External;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public class ItemApiEndpoint
{
    private ItemApiEndpoint()
    {
        
    }

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/ix/{id}", GetItemFromIx)
            .RequireRateLimiting("fixed-external-downloads");
    }

    private static async Task<Results<Ok<ItemContentDto>, NotFound, StatusCodeHttpResult>> GetItemFromIx(
        HttpContext context,
        AppDbContext db,
        HybridCache cache,
        IHttpClientFactory httpFactory,
        ulong id,
        ILogger<ItemApiEndpoint> logger,
        CancellationToken cancellationToken)
    {
        var http = httpFactory.CreateClient("exchange");

        using var itemInfoResponseTask = http.GetAsync($"https://item.exchange/api/item/get_item_info/multi/{id}", cancellationToken);
        using var itemResponse = await http.GetAsync($"https://item.exchange/item/download/{id}", cancellationToken);

        if (itemResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        itemResponse.EnsureSuccessStatusCode();

        var itemContentLength = itemResponse.Content.Headers.ContentLength;

        var etag = Convert.ToBase64String(Encoding.ASCII.GetBytes($"ix-{id}-{itemContentLength}"));

        context.Response.Headers.ETag = etag;

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var itemData = await itemResponse.Content.ReadAsByteArrayAsync(cancellationToken);

        using var itemInfoResponse = await itemInfoResponseTask;

        var itemInfoDto = default(ItemInfoDto);

        if (itemInfoResponse.IsSuccessStatusCode)
        {
            var itemInfos = await itemInfoResponse.Content.ReadFromJsonAsync(AppJsonContext.Default.IxItemInfoArray, cancellationToken);

            if (itemInfos?.Length > 0)
            {
                var item = itemInfos[0];

                itemInfoDto = new ItemInfoDto
                {
                    Name = item.Name,
                    UploaderId = item.UserID.ToString(),
                    UploaderName = item.Username,
                    UpdatedAt = item.Updated,
                    Score = item.Score,
                    Set = item.SetID == 0 ? null : new ItemSetInfoDto
                    {
                        Id = item.SetID,
                        Name = item.SetName ?? "",
                        Directory = item.Directory ?? "",
                        ZipIndex = item.ZipIndex ?? ""
                    },
                };
            }
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(new ItemContentDto
        {
            Item = itemInfoDto,
            Content = itemData
        });
    }
}
