using GBX.NET;
using GbxTools3D.Client.Deserializers;
using GbxTools3D.Client.Models;
using GbxTools3D.Data.Entities;
using GbxTools3D.Enums;
using GbxTools3D.Serializers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Immutable;
using System.Text.Json;

namespace GbxTools3D.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const int MaxJsonLength = 1024;

    private static readonly ValueComparer<ImmutableArray<BlockUnit>> blockUnitValueComparer = new(
        (x, y) => x.SequenceEqual(y, default(IEqualityComparer<BlockUnit>)),
        x => x.Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode())),
        x => ImmutableArray.CreateRange(x));

    private static readonly ValueComparer<ImmutableDictionary<string, string>> dictionaryStringStringValueComparer = new(
        (x, y) => x!.SequenceEqual(y!),
        x => x.Aggregate(0, (current, item) => HashCode.Combine(current, item.Key.GetHashCode(), item.Value.GetHashCode())),
        x => ImmutableDictionary.CreateRange(x));

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
            entity.Property(x => x.AirUnits)
                .HasConversion(
                    obj => SerializeUnits(obj),
                    json => DeserializeUnits(json),
                    blockUnitValueComparer)
                .HasMaxLength(3000);
            entity.Property(x => x.GroundUnits)
                .HasConversion(
                    obj => SerializeUnits(obj),
                    json => DeserializeUnits(json),
                    blockUnitValueComparer)
                .HasMaxLength(3000);

            entity.ComplexProperty(x => x.SpawnLocAir, x => ApplyIso4(x, "SpawnLocAir"));
            entity.ComplexProperty(x => x.SpawnLocGround, x => ApplyIso4(x, "SpawnLocGround"));
        });
        
        modelBuilder.Entity<DecorationSize>(entity =>
        {
            entity.Property(x => x.Scene)
                .HasConversion(
                    obj => JsonSerializer.Serialize(obj, DbJsonContext.Default.ImmutableArraySceneObject),
                    json => JsonSerializer.Deserialize(json, DbJsonContext.Default.ImmutableArraySceneObject),
                    new ValueComparer<ImmutableArray<SceneObject>>(
                        (x, y) => x.SequenceEqual(y, default(IEqualityComparer<SceneObject>)),
                        x => x.Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode())),
                        x => ImmutableArray.CreateRange(x)))
                .HasMaxLength(4096);
        });
        
        modelBuilder.Entity<Decoration>(entity =>
        {
            entity.Property(x => x.Musics)
                .HasConversion(
                    obj => JsonSerializer.Serialize(obj, DbJsonContext.Default.ImmutableDictionaryStringString),
                    json => JsonSerializer.Deserialize(json, DbJsonContext.Default.ImmutableDictionaryStringString) ?? ImmutableDictionary<string, string>.Empty,
                    dictionaryStringStringValueComparer)
                .HasMaxLength(MaxJsonLength);
            entity.Property(x => x.Sounds)
                .HasConversion(
                    obj => JsonSerializer.Serialize(obj, DbJsonContext.Default.ImmutableDictionaryStringString),
                    json => JsonSerializer.Deserialize(json, DbJsonContext.Default.ImmutableDictionaryStringString) ?? ImmutableDictionary<string, string>.Empty,
                    dictionaryStringStringValueComparer)
                .HasMaxLength(MaxJsonLength);
        });
        
        modelBuilder.Entity<Material>(entity =>
        {
            entity.Property(x => x.Textures)
                .HasConversion(
                    obj => JsonSerializer.Serialize(obj, DbJsonContext.Default.ImmutableDictionaryStringString),
                    json => JsonSerializer.Deserialize(json, DbJsonContext.Default.ImmutableDictionaryStringString) ?? ImmutableDictionary<string, string>.Empty,
                    dictionaryStringStringValueComparer)
                .HasMaxLength(MaxJsonLength);
        });

        modelBuilder.Entity<ObjectLink>(entity =>
        {
            entity.ComplexProperty(x => x.Loc, x => ApplyIso4(x, "Loc"));
            entity.ComplexProperty(x => x.SoundLoc, x => ApplyIso4(x, "SoundLoc"));
        });
    }

    private static void ApplyIso4(ComplexPropertyBuilder<Iso4> x, string prefix)
    {
        x.Property("XX").HasColumnName($"{prefix}_XX");
        x.Property("XY").HasColumnName($"{prefix}_XY");
        x.Property("XZ").HasColumnName($"{prefix}_XZ");
        x.Property("YX").HasColumnName($"{prefix}_YX");
        x.Property("YY").HasColumnName($"{prefix}_YY");
        x.Property("YZ").HasColumnName($"{prefix}_YZ");
        x.Property("ZX").HasColumnName($"{prefix}_ZX");
        x.Property("ZY").HasColumnName($"{prefix}_ZY");
        x.Property("ZZ").HasColumnName($"{prefix}_ZZ");
        x.Property("TX").HasColumnName($"{prefix}_TX");
        x.Property("TY").HasColumnName($"{prefix}_TY");
        x.Property("TZ").HasColumnName($"{prefix}_TZ");
    }

    private static byte[] SerializeUnits(ImmutableArray<BlockUnit> units)
    {
        using var ms = new MemoryStream();
        using var w = new AdjustedBinaryWriter(ms);

        w.Write7BitEncodedInt(units.Length);
        foreach (var unit in units)
        {
            w.Write(unit.Offset.X);
            w.Write(unit.Offset.Y);
            w.Write(unit.Offset.Z);

            if (unit.Clips.HasValue)
            {
                w.Write((byte)unit.Clips.Value.Length);
                foreach (var clip in unit.Clips.Value)
                {
                    w.Write((byte)(int)clip.Dir);
                    w.WriteRepeatingString(clip.Id);
                }
            }
            else
            {
                w.Write((byte)0); // No clips
            }

            w.Write(unit.AcceptPylons ?? 255);
            w.Write(unit.PlacePylons ?? 0);
        }

        if (ms.Length > 2048)
        {
            throw new InvalidOperationException($"Serialized block units exceed maximum length of 2048 bytes ({ms.Length} bytes).");
        }

        return ms.ToArray();
    }

    private static ImmutableArray<BlockUnit> DeserializeUnits(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var r = new AdjustedBinaryReader(ms);

        var count = r.Read7BitEncodedInt();
        var units = ImmutableArray.CreateBuilder<BlockUnit>(count);
        for (int i = 0; i < count; i++)
        {
            var offset = new Byte3(r.ReadByte(), r.ReadByte(), r.ReadByte());

            var clipCount = r.ReadByte();
            var clips = default(ImmutableArray<BlockClip>.Builder?);
            if (clipCount > 0)
            {
                clips = ImmutableArray.CreateBuilder<BlockClip>(clipCount);
                for (int j = 0; j < clipCount; j++)
                {
                    clips.Add(new BlockClip
                    {
                        Dir = (ClipDir)r.ReadByte(),
                        Id = r.ReadRepeatingString()
                    });
                }
            }

            byte? acceptPylons = r.ReadByte();
            if (acceptPylons == 255) acceptPylons = null;
            byte? placePylons = r.ReadByte();
            if (placePylons == 0) placePylons = null;

            units.Add(new BlockUnit
            {
                Offset = offset,
                Clips = clips?.ToImmutable(),
                AcceptPylons = acceptPylons,
                PlacePylons = placePylons
            });
        }

        return units.ToImmutable();
    }
}
