using GBX.NET;
using GBX.NET.Engines.Plug;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using GBX.NET.Components;
using static GBX.NET.Engines.Plug.CPlugMaterialCustom;
using System.Collections.Immutable;

namespace GbxTools3D.Services;

internal sealed class MaterialService
{
    private readonly AppDbContext db;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<MaterialService> logger;
    
    private readonly SemaphoreSlim semaphore = new(10);

    public MaterialService(
        AppDbContext db,
        IServiceScopeFactory serviceScopeFactory, 
        IOutputCacheStore outputCache, 
        ILogger<MaterialService> logger)
    {
        this.db = db;
        this.serviceScopeFactory = serviceScopeFactory;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    public async Task<IEnumerable<Material>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken)
    {
        return await db.Materials
            .Include(x => x.Shader)
            .Where(x => x.GameVersion == gameVersion)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task CreateOrUpdateMaterialsAsync(string gamePath, GameVersion gameVersion, Dictionary<string, CPlugMaterial?> materials, CancellationToken cancellationToken)
    {        
        var alreadyProcessedTexturePaths = new HashSet<string>();
        
        logger.LogInformation("Processing materials...");

        foreach (var (path, node) in materials)
        {
            if (node is null)
            {
                continue;
            }

            await AddOrUpdateMaterialAsync(gamePath, gameVersion, path, node, alreadyProcessedTexturePaths, cancellationToken);
        }
        
        logger.LogInformation("Saving materials...");
        await db.SaveChangesAsync(cancellationToken);
        await outputCache.EvictByTagAsync("material", cancellationToken);
        await outputCache.EvictByTagAsync("texture", cancellationToken);
        
        var alreadyProcessedShaders = new Dictionary<string, Material>();

        logger.LogInformation("Processing shader materials...");
        foreach (var (parentPath, parentNode) in materials)
        {
            if (parentNode?.Shader is not CPlugMaterial shaderMaterialNode)
            {
                continue;
            }
            
            var path = Path.GetRelativePath(gamePath, parentNode.ShaderFile?.GetFullPath() ?? throw new Exception("Shader has instance but no file"));
            
            if (!alreadyProcessedShaders.TryGetValue(path, out var shaderMaterial))
            {
                shaderMaterial = await AddOrUpdateMaterialAsync(gamePath, gameVersion, path, shaderMaterialNode, alreadyProcessedTexturePaths, cancellationToken);
                alreadyProcessedShaders.Add(path, shaderMaterial);
            }
            
            var parentName = GbxPath.ChangeExtension(parentPath, null);
            var material = await db.Materials.FirstOrDefaultAsync(x =>
                x.GameVersion == gameVersion && x.Name == parentName, cancellationToken) ?? throw new Exception("Parent material not found");

            material.Shader = shaderMaterial;
        }
        
        logger.LogInformation("Saving shader materials...");
        await db.SaveChangesAsync(cancellationToken);
        await outputCache.EvictByTagAsync("material", cancellationToken);
        await outputCache.EvictByTagAsync("texture", cancellationToken); // probably not needed, but just to be sure
    }

    private async Task<Material> AddOrUpdateMaterialAsync(string gamePath, GameVersion gameVersion,
        string materialPath, CPlugMaterial node, HashSet<string> alreadyProcessedTexturePaths, CancellationToken cancellationToken)
    {
        var name = GbxPath.ChangeExtension(materialPath, null);
            
        var material = await db.Materials.FirstOrDefaultAsync(x =>
            x.GameVersion == gameVersion && x.Name == name, cancellationToken);

        if (material is null)
        {
            material = new Material
            {
                Name = name,
                GameVersion = gameVersion
            };
            await db.Materials.AddAsync(material, cancellationToken);
        }
            
        material.SurfaceId = node.SurfaceId;
            
        if (node.CustomMaterial is not null)
        {
            // regular material tweaked from Shader
            material.IsShader = false;
                
            var textures = ImmutableDictionary.CreateBuilder<string, string>();
                
            foreach (var bitmap in node.CustomMaterial.Textures ?? [])
            {
                var textureName = bitmap.Name ?? throw new Exception("Texture has no name");

                if (bitmap.Texture is not CPlugBitmap texture)
                {
                    continue;
                }

                ProcessTexture(gamePath, gameVersion, texture, bitmap.TextureFile, textureName, textures, alreadyProcessedTexturePaths, cancellationToken);
            }
                
            material.Textures = textures.ToImmutable();
        }
        else if (node.DeviceMaterials?.Length > 0)
        {
            // is shader-material!
            material.IsShader = true;

            if (gameVersion != GameVersion.MP4) // some random StackOverflowException in MP4 when trying to access Shader1
            {
                // lookup into shader (SportCar situation)
                var pc0 = node.DeviceMaterials[0].Shader1 as CPlugShaderApply;

                if (pc0?.BitmapAddresses?.Length > 0)
                {
                    var bitmapAddress = pc0.BitmapAddresses[0];

                    if (bitmapAddress.Bitmap is not null)
                    {
                        var textures = ImmutableDictionary.CreateBuilder<string, string>();

                        ProcessTexture(gamePath, gameVersion, bitmapAddress.Bitmap, bitmapAddress.BitmapFile, "Diffuse", textures, alreadyProcessedTexturePaths, cancellationToken);

                        material.Textures = textures.ToImmutable();
                    }
                }
            }
        }
        else
        {
            throw new Exception("Material has no custom material or device materials");
        }

        return material;
    }

    private void ProcessTexture(
        string gamePath,
        GameVersion gameVersion,
        CPlugBitmap texture, 
        GbxRefTableFile? textureFile,
        string textureName,
        ImmutableDictionary<string, string>.Builder textures,
        HashSet<string> alreadyProcessedTexturePaths,
        CancellationToken cancellationToken)
    {
        var textureFullPath = textureFile?.GetFullPath() ?? throw new Exception("Texture has no file");
        var textureRelativePath = Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(textureFullPath, null));
        textures.Add(textureName, textureRelativePath);

        var imageFullPath = texture?.ImageFile?.GetFullPath();
        if (imageFullPath is null)
        {
            return;
        }

        if (!alreadyProcessedTexturePaths.Add(textureRelativePath))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                await ProcessTextureAsync(gamePath, gameVersion, textureName, textureRelativePath, imageFullPath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process texture {ImageFullPath}", imageFullPath);
            }
            finally
            {
                semaphore.Release();
            }
        }, cancellationToken);
    }

    private async Task ProcessTextureAsync(
        string gamePath,
        GameVersion gameVersion,
        string textureName, 
        string textureRelativePath,
        string imageFullPath, 
        CancellationToken cancellationToken)
    {
        byte[] data;

        using (var image = DdsUtils.ToImageSharp(imageFullPath))
        using (var ms = new MemoryStream())
        {
            if (image.Width > 512 || image.Height > 512)
            {
                var newWidth = image.Width / 2;
                var newHeight = image.Height / 2;

                // some textures have 0 alpha colors, PremultiplyAlpha has to be set to false to preserve this color
                var resizeOptions = new ResizeOptions
                {
                    PremultiplyAlpha = false,
                    Size = new Size(newWidth, newHeight)
                };

                image.Mutate(x => x.Resize(resizeOptions));
            }

            if (textureName == "Normal")
            {
                if (image is Image<Bgra32> imageRgba)
                {
                    imageRgba.ProcessPixelRows(accessor =>
                    {
                        for (var y = 0; y < accessor.Height; y++)
                        {
                            var row = accessor.GetRowSpan(y);
                            for (var x = 0; x < row.Length; x++)
                            {
                                var pixel = row[x];
                                pixel.R = pixel.A;
                                pixel.A = 255;
                                row[x] = pixel;
                            }
                        }
                    });
                }
                else if (image is Image<Bgr24> imageRgb)
                {
                    imageRgb.ProcessPixelRows(accessor =>
                    {
                        for (var y = 0; y < accessor.Height; y++)
                        {
                            var row = accessor.GetRowSpan(y);
                            for (var x = 0; x < row.Length; x++)
                            {
                                var pixel = row[x];

                                // Invert red and green channels
                                var invertedRed = (byte)(255 - pixel.R);
                                var invertedGreen = (byte)(255 - pixel.G);

                                // Convert to grayscale
                                var grayscaleInvertedRed = (byte)(0.3 * invertedRed + 0.59 * invertedGreen + 0.11 * pixel.B);
                                var grayscaleOriginal = (byte)(0.3 * pixel.R + 0.59 * pixel.G + 0.11 * pixel.B);

                                // Blend the two grayscale values (overlay logic)
                                var blended = (byte)((grayscaleOriginal < 128)
                                    ? (2 * grayscaleOriginal * grayscaleInvertedRed / 255)
                                    : (255 - 2 * (255 - grayscaleOriginal) * (255 - grayscaleInvertedRed) / 255));

                                // Invert the blended result to create the blue channel
                                pixel.B = (byte)(255 - blended);

                                row[x] = pixel;
                            }
                        }
                    });
                }
                else
                {
                    logger.LogWarning("Normal map {ImageFullPath} is not RGBA32 - {Type}", imageFullPath, image.GetType());
                }
            }

            await image.SaveAsWebpAsync(ms, new WebpEncoder
            {
                Method = WebpEncodingMethod.Fastest,
                TransparentColorMode = WebpTransparentColorMode.Preserve // preserve 0 alpha color often used for specularity on textures
            },
                cancellationToken);
            data = ms.ToArray();
        }

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var texture = await scopedDb.Textures.FirstOrDefaultAsync(x =>
            x.GameVersion == gameVersion && x.Path == textureRelativePath, cancellationToken);

        var hash =
            $"GbxTools3D|Texture|{gameVersion}|{textureRelativePath}|PeopleOnTheBusLikeDMCA".Hash();

        if (texture is null)
        {
            texture = new Texture
            {
                Hash = hash,
                Data = data,
                GameVersion = gameVersion,
                Path = textureRelativePath
            };
            await scopedDb.Textures.AddAsync(texture, cancellationToken);
        }

        texture.Hash = hash;
        texture.Data = data;
        texture.ImagePath = Path.GetRelativePath(gamePath, imageFullPath);
        texture.UpdatedAt = DateTime.UtcNow;

        await scopedDb.SaveChangesAsync(cancellationToken);
    }
}