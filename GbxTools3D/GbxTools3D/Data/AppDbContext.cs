using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GbxTools3D.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public required DbSet<Collection> Collections { get; set; }
    public required DbSet<BlockInfo> BlockInfos { get; set; }
    public required DbSet<BlockVariant> BlockVariants { get; set; }
    public required DbSet<Mesh> Meshes { get; set; }

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
    }
}
