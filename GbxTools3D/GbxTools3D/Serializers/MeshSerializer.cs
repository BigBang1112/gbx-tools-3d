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

    private MeshSerializer(CPlugSolid solid, byte? lod, bool collision)
    {
        this.solid = solid;
        this.lod = lod;
        this.collision = collision;
    }

    public static void Serialize(Stream stream, CPlugSolid solid, byte? lod = null, bool collision = false)
    {
        var serializer = new MeshSerializer(solid, lod, collision);
        serializer.Serialize(stream);
    }

    public static byte[] Serialize(CPlugSolid solid, byte? lod = null, bool collision = false)
    {
        using var ms = new MemoryStream();
        Serialize(ms, solid, lod, collision);
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

        using var deflate = new DeflateStream(stream, CompressionLevel.SmallestSize);
        using var w = new AdjustedBinaryWriter(deflate);

        wd.Write(lod ?? 255);

        var fileWriteTime = solid?.FileWriteTime ?? solid2?.FileWriteTime;

        w.Write(fileWriteTime.HasValue);
        if (fileWriteTime.HasValue)
        {
            w.Write(fileWriteTime.Value.ToFileTimeUtc());
        }

        if (solid is not null && solid.Tree is CPlugTree tree)
        {
            WriteTree(w, tree);
        }

        /*if (solid2 is not null)
        {
            tree = (CPlugTree)Activator.CreateInstance(typeof(CPlugTree), true)!;
            tree.Visual = solid2.Visuals[0];
            WriteTree(w, tree);
        }*/
    }

    private void WriteTree(AdjustedBinaryWriter w, CPlugTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        w.Write7BitEncodedInt(tree.Children.Count);

        WriteTranslation(w, tree.Location);
        WriteVisual(w, tree.Visual);
        WriteVisualMip(w, tree as CPlugTreeVisualMip);
        WriteSurface(w, tree.Surface as CPlugSurface);
        
        try
        {
            WriteShader(w, tree.ShaderFile);
        }
        catch
        {
            WriteShader(w, tree.ShaderFile);
        }

        w.Write(tree.Name ?? "");

        foreach (var node in tree.Children)
        {
            WriteTree(w, node);
        }
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
        if (collision || visual is null)
        {
            w.Write(false);
            return;
        }

        if (visual is CPlugVisualSprite)
        {
            w.Write(false); // Sprites are not supported
            return;
        }

        w.Write(true);

        if (visual is not CPlugVisual3D visual3d)
        {
            throw new Exception("Visual is not 3D");
        }

        if (visual3d.VertexStreams.Count == 0)
        {
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

            /*foreach (var v in visual3d.Vertices)
            {
                w.Write(v.Normal.X);
                w.Write(v.Normal.Y);
                w.Write(v.Normal.Z);
            }*/
        }
        else
        {
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

        w.Write7BitEncodedInt(visualIndexed.IndexBuffer?.Indices.Length ?? 0);

        foreach (var index in visualIndexed.IndexBuffer?.Indices ?? [])
        {
            w.Write7BitEncodedInt(index);
        }
    }

    private void WriteVisualMip(AdjustedBinaryWriter w, CPlugTreeVisualMip? mip)
    {
        if (mip is null)
        {
            w.Write(false);
            return;
        }

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
