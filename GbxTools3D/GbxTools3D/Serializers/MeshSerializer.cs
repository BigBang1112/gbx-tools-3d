using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Graphic;
using GBX.NET.Engines.Plug;
using System.IO.Compression;
using System.Numerics;

namespace GbxTools3D.Serializers;

public class MeshSerializer
{
    private readonly CPlugSolid? solid;
    private readonly CPlugSolid2Model? solid2;
    private readonly CPlugPrefab? prefab;
    private readonly byte? lod;
    private readonly bool collision;
    private readonly CPlugVehicleVisModelShared? vehicle;
    private readonly bool isDeco;
    private readonly IDictionary<string, string>? materialSpecialMapping;

    private MeshSerializer(CPlugSolid solid, byte? lod, bool collision, CPlugVehicleVisModelShared? vehicle, bool isDeco, IDictionary<string, string>? materialSpecialMapping)
    {
        this.solid = solid;
        this.collision = collision;
        this.vehicle = vehicle;
        this.isDeco = isDeco;
        this.materialSpecialMapping = materialSpecialMapping;
        this.lod = vehicle is not null && lod >= vehicle.VisualVehicles.Length
            ? (byte)(vehicle.VisualVehicles.Length - 1) : lod;
    }

    private MeshSerializer(CPlugSolid2Model solid2, byte? lod, CPlugVehicleVisModelShared? vehicle)
    {
        this.solid2 = solid2;
        this.lod = lod;
        this.vehicle = vehicle;
    }

    private MeshSerializer(CPlugPrefab prefab, byte? lod)
    {
        this.prefab = prefab;
        this.lod = lod;
    }

    public static void Serialize(
        Stream stream, 
        CPlugSolid solid,
        string? filePath,
        string gamePath, 
        byte? lod = null, 
        bool collision = false, 
        CPlugVehicleVisModelShared? vehicle = null, 
        bool isDeco = false,
        IDictionary<string, string>? materialSpecialMapping = null)
    {
        var serializer = new MeshSerializer(solid, lod, collision, vehicle, isDeco, materialSpecialMapping);
        serializer.Serialize(stream, filePath, gamePath);
    }

    public static byte[] Serialize(
        CPlugSolid solid,
        string? filePath,
        string gamePath, 
        byte? lod = null, 
        bool collision = false, 
        CPlugVehicleVisModelShared? vehicle = null, 
        bool isDeco = false,
        IDictionary<string, string>? materialSpecialMapping = null)
    {
        using var ms = new MemoryStream();
        Serialize(ms, solid, filePath, gamePath, lod, collision, vehicle, isDeco, materialSpecialMapping);
        return ms.ToArray();
    }

    public static void Serialize(Stream stream, CPlugSolid2Model solid, string? filePath, string gamePath, byte? lod = null, CPlugVehicleVisModelShared? vehicle = null)
    {
        var serializer = new MeshSerializer(solid, lod, vehicle);
        serializer.Serialize(stream, filePath, gamePath);
    }

    public static byte[] Serialize(CPlugSolid2Model solid, string? filePath, string gamePath, byte? lod = null, CPlugVehicleVisModelShared? vehicle = null)
    {
        using var ms = new MemoryStream();
        Serialize(ms, solid, filePath, gamePath, lod, vehicle);
        return ms.ToArray();
    }

    public static void Serialize(Stream stream, CPlugPrefab prefab, string? filePath, string gamePath, byte? lod = null)
    {
        var serializer = new MeshSerializer(prefab, lod);
        serializer.Serialize(stream, filePath, gamePath);
    }

    public static byte[] Serialize(CPlugPrefab prefab, string? filePath, string gamePath, byte? lod = null)
    {
        using var ms = new MemoryStream();
        Serialize(ms, prefab, filePath, gamePath, lod);
        return ms.ToArray();
    }

    private void Serialize(Stream stream, string? filePath, string gamePath)
    {
        if (solid is null && solid2 is null && prefab is null)
        {
            throw new Exception("Either Solid or Solid2 or Prefab must be provided for serialization.");
        }

        using var wd = new AdjustedBinaryWriter(stream);

        wd.Write([0xD4, 0x54, 0x35, 0x84, 0x03, 0xCD]);
        wd.Write7BitEncodedInt(2); // version

        wd.Write(lod ?? 255);

        using var deflate = new DeflateStream(stream, CompressionLevel.SmallestSize);
        using var w = new AdjustedBinaryWriter(deflate);

        var fileWriteTime = solid?.FileWriteTime ?? solid2?.FileWriteTime;

        w.Write(fileWriteTime.HasValue);
        if (fileWriteTime.HasValue)
        {
            w.Write(fileWriteTime.Value.ToFileTimeUtc());
        }

        w.Write(filePath ?? "");

        if (solid?.Tree is CPlugTree tree)
        {
            WriteTree(w, tree, gamePath, isRoot: true);
        }
        else if (solid2 is not null)
        {
            WriteTree(w, CreateSolid2Tree(solid2), gamePath, isRoot: true);
        }
        else if (prefab is not null)
        {
            WriteTree(w, CreatePrefabTree(prefab), gamePath, isRoot: true);
        }
        else
        {
            throw new Exception("Solid has no tree or Solid2/Prefab was not provided.");
        }

        /*if (solid2 is not null)
        {
            tree = (CPlugTree)Activator.CreateInstance(typeof(CPlugTree), true)!;
            tree.Visual = solid2.Visuals[0];
            WriteTree(w, tree);
        }*/
    }

    private static CPlugTree CreatePrefabTree(CPlugPrefab prefab)
    {
        var tree2 = new CPlugTree();

        for (int i = 0; i < prefab.Ents.Length; i++)
        {
            var ent = prefab.Ents[i];

            CPlugTree subTree;
            if (ent.Model is CPlugStaticObjectModel { Mesh: not null } staticObject)
            {
                subTree = CreateSolid2Tree(staticObject.Mesh);
            }
            else if (ent.Model is CPlugPrefab subPrefab) // this could get download expensive, may consider mesh reference
            {
                subTree = CreatePrefabTree(subPrefab);
            }
            else
            {
                continue;
            }

            subTree.Name = $"#{i}";

            var mat = (Mat3)Matrix4x4.CreateFromQuaternion(
                new Quaternion(ent.Rotation.X, ent.Rotation.Y, ent.Rotation.Z, ent.Rotation.W)
            );

            subTree.Location = new Iso4(mat.XX, mat.XY, mat.XZ, mat.YX, mat.YY, mat.YZ, mat.ZX, mat.ZY, mat.ZZ, ent.Position.X, ent.Position.Y, ent.Position.Z);

            tree2.Children.Add(subTree);

        }

        return tree2;
    }

    private static CPlugTree CreateSolid2Tree(CPlugSolid2Model solid2)
    {
        var tree2 = new CPlugTree();

        var shadedGeomsByLod = solid2.ShadedGeoms?.GroupBy(x => x.Lod) ?? [];

        var mips = shadedGeomsByLod.Count() >= 2 ? new CPlugTreeVisualMip { Levels = [] } : null;

        foreach (var shadedGeomLodGroup in shadedGeomsByLod)
        {
            var levelTree = mips is null ? null : new CPlugTree(); // only used if mips is not null

            foreach (var (i, shadedGeom) in shadedGeomLodGroup.Index())
            {
                var subTree = new CPlugTree
                {
                    Name = $"#{shadedGeom.VisualIndex}",
                    Visual = solid2.Visuals?[shadedGeom.VisualIndex],
                };

                if (solid2.MaterialIds?.Length > 0)
                {
                    var materialName = solid2.MaterialIds[shadedGeom.MaterialIndex];
                    subTree.ShaderFile = materialName is null ? null : new GbxRefTableFile(new(), 0, false, materialName);
                }
                else if (solid2.Materials?.Length > 0)
                {
                    subTree.Shader = solid2.Materials[shadedGeom.MaterialIndex].Node;
                    subTree.ShaderFile = solid2.Materials[shadedGeom.MaterialIndex].File;
                }
                else
                {

                }


                if (levelTree is null)
                {
                    tree2.Children.Add(subTree);
                }
                else
                {
                    levelTree.Children.Add(subTree);
                }
            }

            if (mips is not null && levelTree is not null)
            {
                mips.Levels.Add(new CPlugTreeVisualMip.Level(shadedGeomLodGroup.Key * 64, levelTree));
            }
        }

        if (mips is not null)
        {
            tree2.Children.Add(mips);
        }

        return tree2;
    }

    private void WriteTree(AdjustedBinaryWriter w, CPlugTree tree, string gamePath, bool isRoot = false)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var isSpecialDeco = isDeco && lod is null && tree.Children.Count == 2 && tree.Children[0].Name == "High" && tree.Children[1].Name == "Low";
        var isSpecialDeco2 = isDeco && lod is null && tree.Children.Count == 3 && tree.Children[0].Name == "Low" && tree.Children[1].Name == "High";

        w.Write7BitEncodedInt(tree.Children.Count(x => ShouldIncludeTree(x, isRoot) && !isSpecialDeco));

        WriteTranslation(w, tree.Location);
        var hasVisual = WriteVisual(w, tree.Visual);

        if (hasVisual)
        {
            if (materialSpecialMapping?.TryGetValue(tree.Name, out var specialMaterial) == true)
            {
                WriteMaterial(w, specialMaterial);
            }
            else
            {
                WriteMaterial(w, tree.Shader as CPlugMaterial, tree.ShaderFile, tree.Shader as CPlugShaderApply, gamePath);
            }
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
        else if (isSpecialDeco2)
        {
            var low = tree.Children[0];
            var high = tree.Children[1];

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

        WriteLight(w, tree as CPlugTreeLight);

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

    private static void WriteMaterial(AdjustedBinaryWriter w, CPlugMaterial? material, GbxRefTableFile? materialFile, CPlugShaderApply? shader, string gamePath)
    {
        var materialName = materialFile is null ? string.Empty : Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(materialFile.GetFullPath(), null));

        if (shader is not null)
        {
            materialName = $"Shader_{Guid.NewGuid()}"; // should be probably removed
        }

        w.Write(materialName);

        var shaderName = material?.ShaderFile is null ? string.Empty : Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(material.ShaderFile.GetFullPath(), null));

        if (shaderName == Path.Combine("Techno", "Media", "Material", "TDiff PX2 Trans NormY PC3only")
            || shaderName == Path.Combine("Techno", "Media", "Material", "TAdd Night"))
        {
            w.Write(true); // additionalMaterialProperties
            w.Write(false); // cast shadows
        }
        else
        {
            w.Write(false); // additionalMaterialProperties
        }
    }

    private static void WriteMaterial(AdjustedBinaryWriter w, string materialName)
    {
        w.Write(materialName);
        w.Write(false); // additionalMaterialProperties
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

                if (mesh.CookedTriangles?.Length > 0)
                {
                    w.Write7BitEncodedInt(mesh.CookedTriangles.Length);

                    var maxIndex = 0;
                    foreach (var tri in mesh.CookedTriangles)
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

                    foreach (var tri in mesh.CookedTriangles)
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
                }
                else if (mesh.Triangles?.Length > 0)
                {
                    w.Write7BitEncodedInt(mesh.Triangles.Length);

                    var maxIndex = 0;
                    foreach (var tri in mesh.Triangles)
                    {
                        if (tri.U01.X > maxIndex) maxIndex = tri.U01.X;
                        if (tri.U01.Y > maxIndex) maxIndex = tri.U01.Y;
                        if (tri.U01.Z > maxIndex) maxIndex = tri.U01.Z;
                    }

                    byte intSize = maxIndex switch
                    {
                        < 256 => 1,
                        < 65536 => 2,
                        _ => 4
                    };
                    w.Write(intSize);

                    foreach (var tri in mesh.Triangles)
                    {
                        w.Write((byte)materials[tri.U04]);
                        switch (intSize)
                        {
                            case 1:
                                w.Write((byte)tri.U01.X);
                                w.Write((byte)tri.U01.Y);
                                w.Write((byte)tri.U01.Z);
                                break;
                            case 2:
                                w.Write((ushort)tri.U01.X);
                                w.Write((ushort)tri.U01.Y);
                                w.Write((ushort)tri.U01.Z);
                                break;
                            case 4:
                                w.Write(tri.U01.X);
                                w.Write(tri.U01.Y);
                                w.Write(tri.U01.Z);
                                break;
                        }
                    }
                }
                break;
        }
    }

    private static void WriteLight(AdjustedBinaryWriter w, CPlugTreeLight? treeLight)
    {
        if (treeLight?.PlugLight?.Light is not GxLight light)
        {
            w.Write(false);
            return;
        }

        var plugLight = treeLight.PlugLight;

        w.Write(true);
        w.Write(plugLight.NightOnly);
        w.Write(light.Color.X);
        w.Write(light.Color.Y);
        w.Write(light.Color.Z);
        w.Write(light.Intensity);

        if (light is GxLightSpot spot)
        {
            w.Write7BitEncodedInt(2);
            w.Write(spot.Radius);
            w.Write(spot.AngleInner);
            w.Write(spot.AngleOuter);
        }
        else if (light is GxLightBall lightBall)
        {
            w.Write7BitEncodedInt(1);
            w.Write(lightBall.Radius);
        }
        else
        {
            w.Write7BitEncodedInt(0);
        }
    }
}
