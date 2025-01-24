using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Plug;
using System.IO.Compression;
using System.Xml.Linq;

namespace GbxTools3D.Serializers;

public class MeshSerializer
{
    private readonly CPlugSolid? solid;
    private readonly CPlugSolid2Model? solid2;
    private readonly byte? lod;
    private readonly bool collision;
    private readonly CPlugVehicleVisModelShared? vehicle;

    private MeshSerializer(CPlugSolid solid, byte? lod, bool collision, CPlugVehicleVisModelShared? vehicle)
    {
        this.solid = solid;
        this.collision = collision;
        this.vehicle = vehicle;

        this.lod = vehicle is not null && lod >= vehicle.VisualVehicles.Length
            ? (byte)(vehicle.VisualVehicles.Length - 1) : lod;
    }

    public static void Serialize(Stream stream, CPlugSolid solid, byte? lod = null, bool collision = false, CPlugVehicleVisModelShared? vehicle = null)
    {
        var serializer = new MeshSerializer(solid, lod, collision, vehicle);
        serializer.Serialize(stream);
    }

    public static byte[] Serialize(CPlugSolid solid, byte? lod = null, bool collision = false, CPlugVehicleVisModelShared? vehicle = null)
    {
        using var ms = new MemoryStream();
        Serialize(ms, solid, lod, collision, vehicle);
        return ms.ToArray();
    }

    private void Serialize(Stream stream)
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
            WriteTree(w, tree, isRoot: true);
        }

        /*if (solid2 is not null)
        {
            tree = (CPlugTree)Activator.CreateInstance(typeof(CPlugTree), true)!;
            tree.Visual = solid2.Visuals[0];
            WriteTree(w, tree);
        }*/
    }

    private void WriteTree(AdjustedBinaryWriter w, CPlugTree tree, bool isRoot = false)
    {
        ArgumentNullException.ThrowIfNull(tree);

        w.Write7BitEncodedInt(tree.Children.Count(x => ShouldIncludeTree(x, isRoot)));

        WriteTranslation(w, tree.Location);
        WriteVisual(w, tree.Visual);

        if (vehicle is not null && isRoot && lod is null)
        {
            var visualMip = new CPlugTreeVisualMip();

            foreach (var level in tree.Children)
            {
                // may not work for boats and custom skins properly
                if (int.TryParse(level.Name, out var treeIndex))
                {
                    var lod = treeIndex * 16;
                    visualMip.Levels.Add(new(lod, level));
                }
            }

            visualMip.Levels.Sort((x, y) => x.FarZ.CompareTo(y.FarZ));

            WriteVisualMip(w, visualMip);
        }
        else
        {
            WriteVisualMip(w, tree as CPlugTreeVisualMip);
        }

        WriteSurface(w, tree.Surface as CPlugSurface);
        WriteShader(w, tree.ShaderFile);

        w.Write(tree.Name ?? "");

        foreach (var node in tree.Children.Where(x => ShouldIncludeTree(x, isRoot)))
        {
            WriteTree(w, node);
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

        if (trans.Value.XX == 1 && trans.Value.XY == 0 && trans.Value.XZ == 0
         && trans.Value.YX == 0 && trans.Value.YY == 1 && trans.Value.YZ == 0
         && trans.Value.ZX == 0 && trans.Value.ZY == 0 && trans.Value.ZZ == 1)
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

        if (trans.Value.TX == 0 && trans.Value.TY == 0 && trans.Value.TZ == 0)
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

    private void WriteVisual(AdjustedBinaryWriter w, CPlugVisual? visual)
    {
        if (collision || visual is null || visual is CPlugVisualSprite)
        {
            w.Write(false);
            return;
        }

        w.Write(true);

        if (visual is not CPlugVisual3D visual3d)
        {
            throw new Exception("Visual is not 3D");
        }

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
            w.Write(false); // has normals (it has, just not exposed)

            var vertStream = visual3d.VertexStreams[0];

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
        }

        if (visual is not CPlugVisualIndexed visualIndexed)
        {
            throw new Exception("Visual is not indexed");
        }

        var indices = visualIndexed.IndexBuffer?.Indices ?? [];

        w.Write7BitEncodedInt(indices.Length);

        // write int size
        var largestIndex = indices.Max();
        var intSize = largestIndex switch
        {
            < 256 => 1,
            < 65536 => 2,
            _ => 4
        };
        w.Write((byte)intSize);

        foreach (var index in indices)
        {
            switch (intSize)
            {
                case 1:
                    w.Write((byte)index);
                    break;
                case 2:
                    w.Write((ushort)index);
                    break;
                case 4:
                    w.Write(index);
                    break;
            }
        }
    }

    private void WriteVisualMip(AdjustedBinaryWriter w, CPlugTreeVisualMip? mip)
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
            WriteTree(w, tree);
        }
    }

    private static void WriteShader(AdjustedBinaryWriter w, GbxRefTableFile? shaderFile)
    {
        if (shaderFile is null)
        {
            w.Write(string.Empty);
            return;
        }

        w.Write(GbxPath.GetFileNameWithoutExtension(shaderFile.FilePath));
    }

    private void WriteSurface(AdjustedBinaryWriter w, CPlugSurface? surface)
    {
        if (surface is null || !collision)
        {
            w.Write(false);
            return;
        }

        w.Write(false); // TODO
    }
}
