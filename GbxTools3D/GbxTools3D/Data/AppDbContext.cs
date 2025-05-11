using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GbxTools3D.Client.Models;

namespace GbxTools3D.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public required DbSet<Collection> Collections { get; set; }
    public required DbSet<BlockInfo> BlockInfos { get; set; }
    public required DbSet<BlockVariant> BlockVariants { get; set; }
    public required DbSet<Mesh> Meshes { get; set; }
    public required DbSet<ObjectLink> ObjectLinks { get; set; }
    public required DbSet<Decoration> Decorations { get; set; }
    public required DbSet<DecorationSize> DecorationSizes { get; set; }
    public required DbSet<Material> Materials { get; set; }
    public required DbSet<Texture> Textures { get; set; }
    public required DbSet<Icon> Icons { get; set; }
    public required DbSet<DataImport> DataImports { get; set; }
    public required DbSet<Vehicle> Vehicles { get; set; }
    public required DbSet<Sound> Sounds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlockInfo>(entity =>
        {
            entity.Property(x => x.AirUnits).HasConversion(
                obj => JsonSerializer.Serialize(obj, AppJsonContext.Default.BlockUnitArray),
                json => JsonSerializer.Deserialize(json, AppJsonContext.Default.BlockUnitArray) ?? Array.Empty<BlockUnit>()
            );
            entity.Property(x => x.GroundUnits).HasConversion(
                obj => JsonSerializer.Serialize(obj, AppJsonContext.Default.BlockUnitArray),
                json => JsonSerializer.Deserialize(json, AppJsonContext.Default.BlockUnitArray) ?? Array.Empty<BlockUnit>()
            );
        });
        
        modelBuilder.Entity<DecorationSize>(entity =>
        {
            entity.Property(x => x.Scene).HasConversion(
                obj => JsonSerializer.Serialize(obj, AppJsonContext.Default.SceneObjectArray),
                json => JsonSerializer.Deserialize(json, AppJsonContext.Default.SceneObjectArray) ?? Array.Empty<SceneObject>()
            );
        });
        
        modelBuilder.Entity<Decoration>(entity =>
        {
            entity.Property(x => x.Musics).HasConversion(
                obj => JsonSerializer.Serialize(obj, AppJsonContext.Default.DictionaryStringString),
                json => JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString) ?? new Dictionary<string, string>()
            );
            entity.Property(x => x.Sounds).HasConversion(
                obj => JsonSerializer.Serialize(obj, AppJsonContext.Default.DictionaryStringString),
                json => JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString) ?? new Dictionary<string, string>()
            );
        });
        
        modelBuilder.Entity<Material>(entity =>
        {
            entity.Property(x => x.Textures).HasConversion(
                obj => JsonSerializer.Serialize(obj, AppJsonContext.Default.DictionaryStringString),
                json => JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString) ?? new Dictionary<string, string>()
            );
        });
    }
}
