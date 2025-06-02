using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Plug;
using System.IO.Compression;

namespace GbxTools3D.Serializers;

public class MeshSerializer
{
    private readonly CPlugSolid? solid;
    private readonly CPlugSolid2Model? solid2;
    private readonly byte? lod;
    private readonly bool collision;
    private readonly CPlugVehicleVisModelShared? vehicle;
    private readonly bool isDeco;

    private MeshSerializer(CPlugSolid solid, byte? lod, bool collision, CPlugVehicleVisModelShared? vehicle, bool isDeco)
    {
        this.solid = solid;
        this.collision = collision;
        this.vehicle = vehicle;
        this.isDeco = isDeco;

        this.lod = vehicle is not null && lod >= vehicle.VisualVehicles.Length
            ? (byte)(vehicle.VisualVehicles.Length - 1) : lod;
    }

    public static void Serialize(Stream stream, CPlugSolid solid, string gamePath, byte? lod = null, bool collision = false, CPlugVehicleVisModelShared? vehicle = null, bool isDeco = false)
    {
        var serializer = new MeshSerializer(solid, lod, collision, vehicle, isDeco);
        serializer.Serialize(stream, gamePath);
    }

    public static byte[] Serialize(CPlugSolid solid, string gamePath, byte? lod = null, bool collision = false, CPlugVehicleVisModelShared? vehicle = null, bool isDeco = false)
    {
        using var ms = new MemoryStream();
        Serialize(ms, solid, gamePath, lod, collision, vehicle, isDeco);
        return ms.ToArray();
    }

    private void Serialize(Stream stream, string gamePath)
    {
        if (solid is not null && solid.Tree is not CPlugTree)
        {
            throw new Exception("Not tree");
        }

        using var wd = new AdjustedBinaryWriter(stream);

        wd.Write([0xD4, 0x54, 0x35, 0x84, 0x03, 0xCD]);
        wd.Write7BitEncodedInt(0); // version

        wd.Write(lod ?? 255);

        using var deflate = new DeflateStream(stream, CompressionLevel.SmallestSize);
        using var w = new AdjustedBinaryWriter(deflate);

        var fileWriteTime = solid?.FileWriteTime ?? solid2?.FileWriteTime;

        w.Write(fileWriteTime.HasValue);
        if (fileWriteTime.HasValue)
        {
            w.Write(fileWriteTime.Value.ToFileTimeUtc());
        }

        if (solid?.Tree is CPlugTree tree)
        {
            WriteTree(w, tree, gamePath, isRoot: true);
        }

        /*if (solid2 is not null)
        {
            tree = (CPlugTree)Activator.CreateInstance(typeof(CPlugTree), true)!;
            tree.Visual = solid2.Visuals[0];
            WriteTree(w, tree);
        }*/
    }

    private void WriteTree(AdjustedBinaryWriter w, CPlugTree tree, string gamePath, bool isRoot = false)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var isSpecialDeco = isDeco && lod is null && tree.Children.Count == 2 && tree.Children[0].Name == "High" && tree.Children[1].Name == "Low";

        w.Write7BitEncodedInt(tree.Children.Count(x => ShouldIncludeTree(x, isRoot) && !isSpecialDeco));

        WriteTranslation(w, tree.Location);
        var hasVisual = WriteVisual(w, tree.Visual);

        if (hasVisual)
        {
            WriteMaterial(w, tree.Shader as CPlugMaterial, tree.ShaderFile, gamePath);
        }

        if (vehicle is not null && isRoot && lod is null)
        {
            var visualMip = new CPlugTreeVisualMip();

            foreach (var level in tree.Children)
            {
                // may not work for boats and custom skins properly
                if (int.TryParse(level.Name, out var treeIndex))
                {
                    var lod = treeIndex * 16;
                    visualMip.Levels.Add(new CPlugTreeVisualMip.Level(lod, level));
                }
            }

            visualMip.Levels.Sort((x, y) => x.FarZ.CompareTo(y.FarZ));

            WriteVisualMip(w, visualMip, gamePath);
        }
        else if (isSpecialDeco)
        {
            var high = tree.Children[0];
            var low = tree.Children[1];

            WriteVisualMip(w, new CPlugTreeVisualMip
            {
                Levels = [
                    new CPlugTreeVisualMip.Level(0, high),
                    new CPlugTreeVisualMip.Level(4096, low)
                ]
            }, gamePath);
        }
        else
        {
            WriteVisualMip(w, tree as CPlugTreeVisualMip, gamePath);
        }

        WriteSurface(w, tree.Surface as CPlugSurface);

        w.Write(tree.Name ?? "");

        foreach (var node in tree.Children.Where(x => ShouldIncludeTree(x, isRoot) && !isSpecialDeco))
        {
            WriteTree(w, node, gamePath);
        }
    }

    private bool ShouldIncludeTree(CPlugTree tree, bool isRoot)
    {
        if (vehicle is null || !isRoot)
        {
            return true;
        }

        // may not work for boats and custom skins properly
        if (int.TryParse(tree.Name, out var treeIndex))
        {
            if (lod != treeIndex - 1)
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteTranslation(AdjustedBinaryWriter w, Iso4? trans)
    {
        if (trans is null)
        {
            w.Write(false);
            w.Write(false);
            return;
        }

        if (trans.Value is { XX: 1, XY: 0, XZ: 0, YX: 0, YY: 1, YZ: 0, ZX: 0, ZY: 0, ZZ: 1 })
        {
            w.Write(false);
        }
        else
        {
            w.Write(true);
            w.Write(trans.Value.XX);
            w.Write(trans.Value.XY);
            w.Write(trans.Value.XZ);
            w.Write(trans.Value.YX);
            w.Write(trans.Value.YY);
            w.Write(trans.Value.YZ);
            w.Write(trans.Value.ZX);
            w.Write(trans.Value.ZY);
            w.Write(trans.Value.ZZ);
        }

        if (trans.Value is { TX: 0, TY: 0, TZ: 0 })
        {
            w.Write(false);
        }
        else
        {
            w.Write(true);
            w.Write(trans.Value.TX);
            w.Write(trans.Value.TY);
            w.Write(trans.Value.TZ);
        }
    }

    private bool WriteVisual(AdjustedBinaryWriter w, CPlugVisual? visual)
    {
        if (collision || visual is null || visual is CPlugVisualSprite || visual is not CPlugVisualIndexed indexed)
        {
            w.Write(false);
            return false;
        }

        w.Write(true);

        var visual3d = indexed;

        if (visual3d.VertexStreams.Count == 0)
        {
            var hasNormals = visual3d.Vertices.FirstOrDefault().Normal.HasValue;
            
            w.Write(hasNormals);
            w.Write7BitEncodedInt(visual3d.Vertices.Length);
            w.Write7BitEncodedInt(visual3d.TexCoords.Length);

            foreach (var texSet in visual.TexCoords)
            {
                foreach (var tex in texSet.TexCoords)
                {
                    w.Write(tex.UV.X);
                    w.Write(tex.UV.Y);
                }
            }

            foreach (var v in visual3d.Vertices)
            {
                w.Write(v.Position.X);
                w.Write(v.Position.Y);
                w.Write(v.Position.Z);
            }

            if (hasNormals)
            {
                foreach (var v in visual3d.Vertices)
                {
                    w.Write(v.Normal.GetValueOrDefault().X);
                    w.Write(v.Normal.GetValueOrDefault().Y);
                    w.Write(v.Normal.GetValueOrDefault().Z);
                }
            }
        }
        else
        {
            var vertStream = visual3d.VertexStreams[0];

            var hasNormals = vertStream.Normals?.Length > 0;
            w.Write(hasNormals); // has normals (it has, just not exposed) (exposed as of June 2 build)

            w.Write7BitEncodedInt(vertStream.Positions?.Length ?? 0);

            w.Write7BitEncodedInt(vertStream.UVs.Count);

            foreach (var (index, uvSet) in vertStream.UVs)
            {
                foreach (var uv in uvSet)
                {
                    w.Write(uv.X);
                    w.Write(uv.Y);
                }
            }

            foreach (var v in vertStream.Positions ?? [])
            {
                w.Write(v.X);
                w.Write(v.Y);
                w.Write(v.Z);
            }

            if (hasNormals)
            {
                foreach (var n in vertStream.Normals ?? [])
                {
                    w.Write(n.X);
                    w.Write(n.Y);
                    w.Write(n.Z);
                }
            }
        }

        if (visual is not CPlugVisualIndexed visualIndexed)
        {
            throw new Exception("Visual is not indexed");
        }

        var indices = visualIndexed.IndexBuffer?.Indices ?? [];

        w.Write7BitEncodedInt(indices.Length);

        // write int size
        byte intSize = indices.Max() switch
        {
            < 256 => 1,
            < 65536 => 2,
            _ => 4
        };
        w.Write(intSize);

        foreach (var index in indices)
        {
            switch (intSize)
            {
                case 1: w.Write((byte)index); break;
                case 2: w.Write((ushort)index); break;
                case 4: w.Write(index); break;
            }
        }
        
        return true;
    }

    private void WriteVisualMip(AdjustedBinaryWriter w, CPlugTreeVisualMip? mip, string gamePath)
    {
        if (mip is null)
        {
            w.Write7BitEncodedInt(0);
            return;
        }

        // this is slightly wrong approach, you wanna preferable ignore visual mips at all on specific lods
        var pickedLevel = lod.HasValue ? (mip.Levels.ElementAtOrDefault(lod.Value) ?? mip.Levels.LastOrDefault()) : null;

        var pickedLevels = pickedLevel is null ? mip.Levels : [pickedLevel];

        w.Write7BitEncodedInt(pickedLevels.Count);

        foreach (var (distance, tree) in pickedLevels)
        {
            w.Write(distance);
            WriteTree(w, tree, gamePath);
        }
    }

    private static void WriteMaterial(AdjustedBinaryWriter w, CPlugMaterial? material, GbxRefTableFile? materialFile, string gamePath)
    {
        w.Write(materialFile is null ? string.Empty : Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(materialFile.GetFullPath(), null)));

        var shaderName = material?.ShaderFile is null ? string.Empty : Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(material.ShaderFile.GetFullPath(), null));

        if (shaderName == Path.Combine("Techno", "Media", "Material", "TDiff PX2 Trans NormY PC3only"))
        {
            w.Write(true); // additionalMaterialProperties
            w.Write(false); // cast shadows
        }
        else
        {
            w.Write(false); // additionalMaterialProperties
        }
    }

    private void WriteSurface(AdjustedBinaryWriter w, CPlugSurface? surface)
    {
        if (surface is null || !collision)
        {
            w.Write(false);
            return;
        }

        w.Write(true);
        var surf = surface.Geom?.Surf ?? surface.Surf;
        var materials = surface.Materials
            .Select(x => (int)(x.SurfaceId ?? x.Material?.SurfaceId ?? CPlugSurface.MaterialId.Concrete))
            .ToList();

        w.Write7BitEncodedInt(surf switch
        {
            CPlugSurface.Sphere => 0,
            CPlugSurface.Ellipsoid => 1,
            CPlugSurface.Mesh => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown surface type.")
        });

        switch (surf)
        {
            case CPlugSurface.Sphere sphere:
                w.Write(sphere.Size);
                break;
            case CPlugSurface.Ellipsoid ellipsoid:
                w.Write(ellipsoid.Size.X);
                w.Write(ellipsoid.Size.Y);
                w.Write(ellipsoid.Size.Z);
                break;
            case CPlugSurface.Mesh mesh:
                w.Write7BitEncodedInt(mesh.Vertices.Length);
                foreach (var v in mesh.Vertices)
                {
                    w.Write(v.X);
                    w.Write(v.Y);
                    w.Write(v.Z);
                }
                
                w.Write7BitEncodedInt(mesh.CookedTriangles?.Length ?? 0);

                var maxIndex = 0;
                foreach (var tri in mesh.CookedTriangles ?? [])
                {
                    if (tri.U02.X > maxIndex) maxIndex = tri.U02.X;
                    if (tri.U02.Y > maxIndex) maxIndex = tri.U02.Y;
                    if (tri.U02.Z > maxIndex) maxIndex = tri.U02.Z;
                }
                
                byte intSize = maxIndex switch
                {
                    < 256 => 1,
                    < 65536 => 2,
                    _ => 4
                };
                w.Write(intSize);

                foreach (var tri in mesh.CookedTriangles ?? [])
                {
                    w.Write((byte)materials[tri.U03]);
                    switch (intSize)
                    {
                        case 1:
                            w.Write((byte)tri.U02.X);
                            w.Write((byte)tri.U02.Y);
                            w.Write((byte)tri.U02.Z);
                            break;
                        case 2:
                            w.Write((ushort)tri.U02.X);
                            w.Write((ushort)tri.U02.Y);
                            w.Write((ushort)tri.U02.Z);
                            break;
                        case 4:
                            w.Write(tri.U02.X);
                            w.Write(tri.U02.Y);
                            w.Write(tri.U02.Z);
                            break;
                    }
                }
                break;
        }
    }
}
