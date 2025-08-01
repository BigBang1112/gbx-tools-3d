﻿using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class TextureApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetTextureInfo)
            .CacheOutput(x => x.Tag("texture"));
        group.MapGet("/{hash}", GetTextureByHash); // cache output questionable due to larger memory usage
    }

    private static async Task<Ok<TextureInfoDto>> GetTextureInfo(AppDbContext db, CancellationToken cancellationToken)
    {
        var textureCount = await db.Textures.CountAsync(cancellationToken);

        return TypedResults.Ok(new TextureInfoDto
        {
            Count = textureCount,
            Format = "webp"
        });
    }
    
    private static readonly Func<AppDbContext, string, Task<CacheableHiddenData?>> TextureFirstOrDefaultAsync = EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Textures
        .Select(x => new CacheableHiddenData { Hash = x.Hash, Data = x.Data, UpdatedAt = x.UpdatedAt })
        .FirstOrDefault(x => x.Hash == hash));

    private static async Task<Results<FileContentHttpResult, NotFound>> GetTextureByHash(
        AppDbContext db, 
        string hash,  
        HttpContext context, 
        CancellationToken cancellationToken)
    {
        var texture = await TextureFirstOrDefaultAsync(db, hash);

        if (texture is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.File(texture.Data, "image/webp", $"{hash}.webp", lastModified: texture.UpdatedAt);
    }
}