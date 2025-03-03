using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using GBX.NET;
using GbxTools3D.Client.Deserializers;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Enums;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal sealed partial class Solid(JSObject obj)
{
    private static int indexCounter;

    private static readonly byte[] MAGIC = [0xD4, 0x54, 0x35, 0x84, 0x03, 0xCD];
    private const int VERSION = 0;

    public JSObject Object { get; } = obj;

    public Vec3 Position
    {
        set
        {
            SetPosition(Object, value.X, value.Y, value.Z);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    public Mat3 Rotation
    {
        set
        {
            SetRotation(Object, value.XX, value.XY, value.XZ, value.YX, value.YY, value.YZ, value.ZX, value.ZY, value.ZZ);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    public Iso4 Location
    {
        set
        {
            SetRotation(Object, value.XX, value.XY, value.XZ, value.YX, value.YY, value.YZ, value.ZX, value.ZY, value.ZZ);
            SetPosition(Object, value.TX, value.TY, value.TZ);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    [JSImport("create", nameof(Solid))]
    private static partial JSObject Create();

    [JSImport("add", nameof(Solid))]
    private static partial void Add(JSObject tree, JSObject child);

    [JSImport("setPosition", nameof(Solid))]
    private static partial void SetPosition(JSObject tree, double x, double y, double z);

    [JSImport("setRotation", nameof(Solid))]
    private static partial void SetRotation(JSObject tree, double xx, double xy, double xz, double yx, double yy, double yz, double zx, double zy, double zz);

    [JSImport("updateMatrix", nameof(Solid))]
    private static partial void UpdateMatrix(JSObject tree);
    
    [JSImport("updateMatrixWorld", nameof(Solid))]
    private static partial void UpdateMatrixWorld(JSObject tree);
    
    [JSImport("createLod", nameof(Solid))]
    private static partial JSObject CreateLod();

    [JSImport("addLod", nameof(Solid))]
    private static partial void AddLod(JSObject lodTree, JSObject levelTree, double distance);

    [JSImport("createGeometry", nameof(Solid))]
    private static partial JSObject CreateGeometry(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> vertexData,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> normalData,
        [JSMarshalAs<JSType.MemoryView>] Span<int> indices, 
        [JSMarshalAs<JSType.MemoryView>] Span<byte> uvData);
    
    [JSImport("mergeGeometries", nameof(Solid))]
    private static partial JSObject MergeGeometries(JSObject[] geometries);

    [JSImport("createInstancedMesh", nameof(Solid))]
    private static partial JSObject CreateInstancedMeshMultipleMaterials(JSObject geometry, JSObject[] materials, int expectedMeshCount, bool receiveShadow, bool castShadow);

    [JSImport("createInstancedMesh", nameof(Solid))]
    private static partial JSObject CreateInstancedMeshSingleMaterial(JSObject geometry, JSObject material, int expectedMeshCount, bool receiveShadow, bool castShadow);

    [JSImport("createMesh", nameof(Solid))]
    private static partial JSObject CreateMeshMultipleMaterials(JSObject geometry, JSObject[] materials, bool receiveShadow, bool castShadow);

    [JSImport("createMesh", nameof(Solid))]
    private static partial JSObject CreateMeshSingleMaterial(JSObject geometry, JSObject material, bool receiveShadow, bool castShadow);
    
    [JSImport("getInstanceInfoFromBlock", nameof(Solid))]
    private static partial JSObject GetInstanceInfoFromBlock(int x, int y, int z, int dir);
    
    public static JSObject GetInstanceInfoFromBlock(Int3 coord, Direction dir)
        => GetInstanceInfoFromBlock(coord.X, coord.Y, coord.Z, (int)dir);

    [JSImport("instantiate", nameof(Solid))]
    private static partial JSObject Instantiate(JSObject tree, JSObject[] instanceInfos);

    [JSImport("log", nameof(Solid))]
    private static partial void Log(JSObject tree);

    public static async Task<Solid> ParseAsync(
        Stream stream, 
        Dictionary<string, MaterialDto>? availableMaterials,
        int? expectedMeshCount = null, 
        bool optimized = true, 
        bool receiveShadow = true,
        bool castShadow = true)
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

        await using var deflate = new DeflateStream(stream, CompressionMode.Decompress);
        using var r = new AdjustedBinaryReader(deflate);

        var fileWriteTime = r.ReadBoolean() ? DateTime.FromFileTime(r.ReadInt64()) : default(DateTime?);

        JSObject tree;
        if (optimized)
        {
            var geometries = new List<JSObject>();
            var materials = new List<JSObject>();
            await ReadTreeAsSingleGeometryAsync(r, rot: Mat3.Identity, pos: Vector3.Zero, geometries, materials, availableMaterials);
            
            switch (geometries.Count)
            {
                case 0:
                    tree = Create();
                    break;
                case 1:
                    tree = expectedMeshCount.HasValue
                        ? CreateInstancedMeshSingleMaterial(geometries[0], materials[0], expectedMeshCount.Value, receiveShadow, castShadow)
                        : CreateMeshSingleMaterial(geometries[0], materials[0], receiveShadow, castShadow);
                    break;
                default:
                {
                    var geometry = MergeGeometries(geometries.ToArray());
                    tree = expectedMeshCount.HasValue
                        ? CreateInstancedMeshMultipleMaterials(geometry, materials.ToArray(), expectedMeshCount.Value, receiveShadow, castShadow)
                        : CreateMeshMultipleMaterials(geometry, materials.ToArray(), receiveShadow, castShadow);
                    break;
                }
            }
        }
        else
        {
            tree = await ReadTreeAsNestedObjectsAsync(r, expectedMeshCount, receiveShadow, castShadow, availableMaterials);
            UpdateMatrixWorld(tree);
            Log(tree); // temporary
        }

        return new Solid(tree);
    }

    public void Instantiate(JSObject[] instanceInfos)
    {
        Instantiate(Object, instanceInfos);
    }

    private static async Task ReadTreeAsSingleGeometryAsync(
        AdjustedBinaryReader r, 
        Mat3 rot, 
        Vector3 pos,
        List<JSObject> geometries,
        List<JSObject> materials,
        Dictionary<string, MaterialDto>? availableMaterials)
    {
        var childrenCount = r.Read7BitEncodedInt();

        if (r.ReadBoolean())
        {
            var a = rot;
            var b = ReadMatrix3(r);
            rot = new Mat3(
                a.XX * b.XX + a.XY * b.YX + a.XZ * b.ZX,
                a.XX * b.XY + a.XY * b.YY + a.XZ * b.ZY,
                a.XX * b.XZ + a.XY * b.YZ + a.XZ * b.ZZ,

                a.YX * b.XX + a.YY * b.YX + a.YZ * b.ZX,
                a.YX * b.XY + a.YY * b.YY + a.YZ * b.ZY,
                a.YX * b.XZ + a.YY * b.YZ + a.YZ * b.ZZ,

                a.ZX * b.XX + a.ZY * b.YX + a.ZZ * b.ZX,
                a.ZX * b.XY + a.ZY * b.YY + a.ZZ * b.ZY,
                a.ZX * b.XZ + a.ZY * b.YZ + a.ZZ * b.ZZ
            );
        }

        if (r.ReadBoolean())
        {
            pos += ReadVector3(r);
        }

        var geometry = ReadVisualAsGeometry(r, rot, pos);
        
        if (geometry is not null)
        {
            geometries.Add(geometry);

            var materialName = r.ReadString();
            var additionalMaterialProperties = r.ReadBoolean();
            
            materials.Add(Material.GetOrCreateMaterial(materialName, availableMaterials));
        }

        await RestAsync(indexCounter);

        var mipLevelCount = r.Read7BitEncodedInt();

        if (mipLevelCount > 0)
        {
            //var lod = CreateLod();

            var storedDistance = 0f;

            for (var i = 0; i < mipLevelCount; i++)
            {
                var distance = storedDistance;
                storedDistance = r.ReadSingle();

                await ReadTreeAsSingleGeometryAsync(r, rot, pos, i == 0 ? geometries : [], i == 0 ? materials : [], availableMaterials);

                //AddLod(lod, lodTree, distance);
            }

            //Add(tree, lod);
        }

        var surface = ReadSurface(r);

        var name = r.ReadString();

        for (var i = 0; i < childrenCount; i++)
        {
            await ReadTreeAsSingleGeometryAsync(r, rot, pos, geometries, materials, availableMaterials);
        }
    }

    private static async Task<JSObject> ReadTreeAsNestedObjectsAsync(
        AdjustedBinaryReader r, 
        int? expectedMeshCount, 
        bool receiveShadow, 
        bool castShadow,
        Dictionary<string, MaterialDto>? availableMaterials = null)
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

        UpdateMatrix(tree);

        var geometry = ReadVisualAsGeometry(r, rot: Mat3.Identity, pos: Vector3.Zero);

        if (geometry is not null)
        {
            var materialName = r.ReadString();
            var additionalMaterialProperties = r.ReadBoolean();
            
            var visual = expectedMeshCount.HasValue
                ? CreateInstancedMeshSingleMaterial(geometry, Material.GetOrCreateMaterial(materialName, availableMaterials), expectedMeshCount.Value, receiveShadow, castShadow)
                : CreateMeshSingleMaterial(geometry, Material.GetOrCreateMaterial(materialName, availableMaterials), receiveShadow, castShadow);
            Add(tree, visual);
        }

        await RestAsync(indexCounter);

        var mipLevelCount = r.Read7BitEncodedInt();

        if (mipLevelCount > 0)
        {
            var lod = CreateLod();

            var storedDistance = 0f;

            for (var i = 0; i < mipLevelCount; i++)
            {
                var distance = storedDistance;
                storedDistance = r.ReadSingle();

                var lodTree = await ReadTreeAsNestedObjectsAsync(r, expectedMeshCount, receiveShadow, castShadow, availableMaterials);

                AddLod(lod, lodTree, distance);
            }

            Add(tree, lod);
        }

        var surface = ReadSurface(r);

        var name = r.ReadString();

        for (var i = 0; i < childrenCount; i++)
        {
            Add(tree, await ReadTreeAsNestedObjectsAsync(r, expectedMeshCount, receiveShadow, castShadow, availableMaterials));
        }

        return tree;
    }

    private static JSObject? ReadVisualAsGeometry(AdjustedBinaryReader r, Mat3 rot, Vector3 pos)
    {
        if (!r.ReadBoolean())
        {
            return null;
        }

        var hasNormals = r.ReadBoolean();

        var vertexCount = r.Read7BitEncodedInt();
        var texSetCount = r.Read7BitEncodedInt();

        // Parse texture coordinates        
        Span<byte> uvData = r.ReadBytes(texSetCount * vertexCount * 2 * sizeof(float));

        // Parse vertices
        Span<byte> vertexData = r.ReadBytes(vertexCount * 3 * sizeof(float));
        
        if (rot != Mat3.Identity || pos != Vector3.Zero)
        {
            var vertices = MemoryMarshal.Cast<byte, Vec3>(vertexData);

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                vertices[i] = new Vec3(
                    vertex.X * rot.XX + vertex.Y * rot.XY + vertex.Z * rot.XZ + pos.X,
                    vertex.X * rot.YZ + vertex.Y * rot.YY + vertex.Z * rot.YZ + pos.Y,
                    vertex.X * rot.ZX + vertex.Y * rot.ZY + vertex.Z * rot.ZZ + pos.Z
                );
            }
        }

        Span<byte> normals = [];

        if (hasNormals)
        {
            // Parse normals
            normals = r.ReadBytes(vertexCount * 3 * sizeof(float));
        }

        // Parse indices
        var indexCount = r.Read7BitEncodedInt();
        var intSize = r.ReadByte();

        Span<byte> indexBuffer = r.ReadBytes(indexCount * intSize);

        Span<int> indices = stackalloc int[indexCount];

        switch (intSize)
        {
            case 1:
                for (var i = 0; i < indexCount; i++)
                {
                    indices[i] = indexBuffer[i];
                }
                break;
            case 2:
                var ushortInds = MemoryMarshal.Cast<byte, ushort>(indexBuffer);
                for (var i = 0; i < indexCount; i++)
                {
                    indices[i] = ushortInds[i];
                }
                break;
            case 4:
                indices = MemoryMarshal.Cast<byte, int>(indexBuffer);
                break;
        }
        
        indexCounter += indexCount;

        return CreateGeometry(vertexData, normals, indices, uvData);
    }

    private static JSObject? ReadSurface(AdjustedBinaryReader r)
    {
        if (!r.ReadBoolean())
        {
            return null;
        }

        switch ((SurfaceType)r.Read7BitEncodedInt())
        {
            case SurfaceType.Sphere:
                var size = r.ReadSingle();
                break;
            case SurfaceType.Ellipsoid:
                var sizeX = r.ReadSingle();
                var sizeY = r.ReadSingle();
                var sizeZ = r.ReadSingle();
                break;
            case SurfaceType.Mesh:
                var vertexCount = r.Read7BitEncodedInt();
                Span<byte> vertices = r.ReadBytes(vertexCount * 3 * sizeof(float));
                
                var triCount = r.Read7BitEncodedInt();
                var intSize = r.ReadByte();

                // 1 mat index byte, 3 floats
                Span<byte> triBuffer = r.ReadBytes(triCount * (1 + intSize * 3));

                Span<int> triBufferInts = stackalloc int[triCount * 4];

                switch (intSize)
                {
                    case 1:
                        for (var i = 0; i < triBufferInts.Length; i++)
                        {
                            triBufferInts[i] = triBuffer[i];
                        }
                        break;
                    case 2:
                        for (var i = 0; i < triCount; i++)
                        {
                            triBufferInts[i * 4] = triBuffer[i * 7];
                            triBufferInts[i * 4 + 1] = BitConverter.ToUInt16(triBuffer.Slice(i * 7 + 1, 2));
                            triBufferInts[i * 4 + 2] = BitConverter.ToUInt16(triBuffer.Slice(i * 7 + 3, 2));
                            triBufferInts[i * 4 + 3] = BitConverter.ToUInt16(triBuffer.Slice(i * 7 + 5, 2));
                        }
                        break;
                    case 4:
                        for (var i = 0; i < triCount; i++)
                        {
                            triBufferInts[i * 4] = triBuffer[i * 13];
                            triBufferInts[i * 4 + 1] = BitConverter.ToInt32(triBuffer.Slice(i * 13 + 1, 4));
                            triBufferInts[i * 4 + 2] = BitConverter.ToInt32(triBuffer.Slice(i * 13 + 5, 4));
                            triBufferInts[i * 4 + 3] = BitConverter.ToInt32(triBuffer.Slice(i * 13 + 9, 4));
                        }
                        break;
                }
                
                break;
            default:
                throw new InvalidDataException("Unknown surface type");
        }

        return null;
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
