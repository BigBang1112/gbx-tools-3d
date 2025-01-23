using GBX.NET;
using GbxTools3D.Client.Deserializers;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace GbxTools3D.Client.Data;

internal sealed partial class Solid
{
    private static int indexCounter;

    private static readonly byte[] MAGIC = [0xD4, 0x54, 0x35, 0x84, 0x03, 0xCD];

    private const int VERSION = 0;

    [JSImport("create", nameof(Solid))]
    private static partial JSObject Create();

    [JSImport("add", nameof(Solid))]
    private static partial void Add(JSObject tree, JSObject child);

    [JSImport("setPosition", nameof(Solid))]
    private static partial void SetPosition(JSObject tree, double x, double y, double z);

    [JSImport("setRotation", nameof(Solid))]
    private static partial void SetRotation(JSObject tree, double xx, double xy, double xz, double yx, double yy, double yz, double zx, double zy, double zz);

    [JSImport("createLod", nameof(Solid))]
    private static partial JSObject CreateLod();

    [JSImport("addLod", nameof(Solid))]
    private static partial void AddLod(JSObject lodTree, JSObject levelTree, double distance);

    [JSImport("createVisual", nameof(Solid))]
    private static partial JSObject CreateVisual(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> vertices,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> normals,
        [JSMarshalAs<JSType.MemoryView>] Span<int> indices, 
        [JSMarshalAs<JSType.MemoryView>] Span<byte> uvs, 
        int expectedMeshCount);

    public static async Task<JSObject> ParseAsync(Stream stream, int expectedMeshCount)
    {
        using var rd = new AdjustedBinaryReader(stream);

        var magic = rd.ReadBytes(6);

        if (!magic.SequenceEqual(MAGIC))
        {
            throw new InvalidDataException("Invalid file format");
        }

        var version = rd.Read7BitEncodedInt();

        if (version > VERSION)
        {
            throw new InvalidDataException("Unsupported version");
        }

        var lod = new byte?(rd.ReadByte());
        if (lod == 255) lod = null;

        using var deflate = new DeflateStream(stream, CompressionMode.Decompress);
        using var r = new AdjustedBinaryReader(deflate);

        var fileWriteTime = r.ReadBoolean() ? DateTime.FromFileTime(r.ReadInt64()) : default(DateTime?);

        return await ReadTreeAsync(r, expectedMeshCount);
    }

    private static async Task<JSObject> ReadTreeAsync(AdjustedBinaryReader r, int expectedMeshCount)
    {
        var tree = Create();

        var childrenCount = r.Read7BitEncodedInt();

        if (r.ReadBoolean())
        {
            var mat3 = ReadMatrix3(r);
            SetRotation(tree, mat3.XX, mat3.XY, mat3.XZ, mat3.YX, mat3.YY, mat3.YZ, mat3.ZX, mat3.ZY, mat3.ZZ);
        }

        if (r.ReadBoolean())
        {
            var pos = ReadVector3(r);
            SetPosition(tree, pos.X, pos.Y, pos.Z);
        }

        var visual = ReadVisual(r, expectedMeshCount, out var indexCount);

        await RestAsync(indexCount);

        if (visual is not null)
        {
            Add(tree, visual);
        }

        var mipLevelCount = r.Read7BitEncodedInt();

        if (mipLevelCount > 0)
        {
            var lod = CreateLod();

            var storedDistance = 0f;

            for (int i = 0; i < mipLevelCount; i++)
            {
                var distance = storedDistance;
                storedDistance = r.ReadSingle();

                var lodTree = await ReadTreeAsync(r, expectedMeshCount);

                AddLod(lod, lodTree, distance);
            }

            Add(tree, lod);
        }

        var hasSurface = r.ReadBoolean();

        var shaderName = r.ReadString();

        var name = r.ReadString();

        for (int i = 0; i < childrenCount; i++)
        {
            Add(tree, await ReadTreeAsync(r, expectedMeshCount));
        }

        return tree;
    }

    private static JSObject? ReadVisual(AdjustedBinaryReader r, int expectedMeshCount, out int indexCount)
    {
        var hasVisual = r.ReadBoolean();

        if (!hasVisual)
        {
            indexCount = 0;
            return null;
        }

        var hasNormals = r.ReadBoolean();

        var vertexCount = r.Read7BitEncodedInt();
        var texSetCount = r.Read7BitEncodedInt();

        // Parse texture coordinates        
        Span<byte> uvs = r.ReadBytes(texSetCount * vertexCount * 2 * sizeof(float));

        // Parse vertices
        Span<byte> vertices = r.ReadBytes(vertexCount * 3 * sizeof(float));

        Span<byte> normals = [];

        if (hasNormals)
        {
            // Parse normals
            normals = r.ReadBytes(vertexCount * 3 * sizeof(float));
        }

        var stopwatch = Stopwatch.StartNew();

        // Parse indices
        indexCount = r.Read7BitEncodedInt();
        var intSize = r.ReadByte();

        Span<byte> indexBuffer = r.ReadBytes(indexCount * intSize);

        Span<int> indices = stackalloc int[indexCount];

        switch (intSize)
        {
            case 1:
                for (int i = 0; i < indexCount; i++)
                {
                    indices[i] = indexBuffer[i];
                }
                break;
            case 2:
                var ushortInds = MemoryMarshal.Cast<byte, ushort>(indexBuffer);
                for (int i = 0; i < indexCount; i++)
                {
                    indices[i] = ushortInds[i];
                }
                break;
            case 4:
                indices = MemoryMarshal.Cast<byte, int>(indexBuffer);
                break;
        }

        return CreateVisual(vertices, normals, indices, uvs, expectedMeshCount);
    }

    private static Mat3 ReadMatrix3(AdjustedBinaryReader reader)
    {
        var xx = reader.ReadSingle();
        var xy = reader.ReadSingle();
        var xz = reader.ReadSingle();
        var yx = reader.ReadSingle();
        var yy = reader.ReadSingle();
        var yz = reader.ReadSingle();
        var zx = reader.ReadSingle();
        var zy = reader.ReadSingle();
        var zz = reader.ReadSingle();

        return new Mat3(xx, xy, xz, yx, yy, yz, zx, zy, zz);
    }

    private static Vector3 ReadVector3(AdjustedBinaryReader reader)
    {
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        var z = reader.ReadSingle();

        return new Vector3(x, y, z);
    }

    private static async Task RestAsync(int indexCount)
    {
        indexCounter += indexCount;

        if (indexCounter > 10000)
        {
            await Task.Delay(1);
            indexCounter = 0;
        }
    }
}
