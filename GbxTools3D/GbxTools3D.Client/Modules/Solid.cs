using GBX.NET;
using GBX.NET.Engines.Plug;
using GbxTools3D.Client.Deserializers;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Enums;
using GbxTools3D.Client.Extensions;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal sealed partial class Solid(JSObject obj, string? filePath)
{
    private static int indexCounter;

    private static readonly byte[] MAGIC = [0xD4, 0x54, 0x35, 0x84, 0x03, 0xCD];
    private const int VERSION = 2;

    private JSObject[] vertexNormalHelpers = [];
    public bool VertexNormalHelperEnabled => vertexNormalHelpers.Length > 0;

    private Dictionary<JSObject, JSObject> wireframeMemorizedMaterials = [];
    public bool WireframeEnabled => wireframeMemorizedMaterials.Count > 0;

    private Solid? collision;
    public bool CollisionsEnabled => collision is not null;

    private Solid[] objectLinks = [];
    public bool ObjectLinksEnabled => objectLinks.Length > 0;

    public JSObject Object { get; } = obj;
    public string? FilePath { get; } = filePath;

    public Vec3 Position
    {
        set
        {
            SetPosition(Object, value.X, value.Y, value.Z);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    public Mat3 RotationMatrix
    {
        set
        {
            SetRotationMatrix(Object, value.XX, value.XY, value.XZ, value.YX, value.YY, value.YZ, value.ZX, value.ZY, value.ZZ);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    public Quat RotationQuaternion
    {
        set
        {
            SetRotationQuaternion(Object, value.X, value.Y, value.Z, value.W);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    public Iso4 Location
    {
        set
        {
            SetRotationMatrix(Object, value.XX, value.XY, value.XZ, value.YX, value.YY, value.YZ, value.ZX, value.ZY, value.ZZ);
            SetPosition(Object, value.TX, value.TY, value.TZ);
            UpdateMatrix(Object);
            UpdateMatrixWorld(Object);
        }
    }

    public bool Visible
    {
        get => Object.GetPropertyAsBoolean("visible");
        set => Object.SetProperty("visible", value);
    }

    public JSObject? GetObjectByName(string name)
    {
        return GetObjectByName(Object, name);
    }

    [JSImport("create", nameof(Solid))]
    private static partial JSObject Create(bool matrixAutoUpdate);

    [JSImport("setName", nameof(Solid))]
    private static partial JSObject SetName(JSObject tree, string name);

    [JSImport("setUserData", nameof(Solid))]
    private static partial JSObject SetUserData(JSObject tree, string filePath);

    [JSImport("getObjectByName", nameof(Solid))]
    private static partial JSObject? GetObjectByName(JSObject tree, string name);

    [JSImport("reorderEuler", nameof(Solid))]
    public static partial void ReorderEuler(JSObject tree);

    [JSImport("add", nameof(Solid))]
    private static partial void Add(JSObject tree, JSObject child);

    [JSImport("setPosition", nameof(Solid))]
    private static partial void SetPosition(JSObject tree, double x, double y, double z);

    [JSImport("setRotationMatrix", nameof(Solid))]
    private static partial void SetRotationMatrix(JSObject tree, double xx, double xy, double xz, double yx, double yy, double yz, double zx, double zy, double zz);

    [JSImport("setRotationQuaternion", nameof(Solid))]
    private static partial void SetRotationQuaternion(JSObject tree, double x, double y, double z, double w);

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
        [JSMarshalAs<JSType.MemoryView>] Span<byte> uvData,
        bool computeNormals = false);
    
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
    
    [JSImport("getInstanceInfo", nameof(Solid))]
    private static partial JSObject GetInstanceInfo(int x, int y, int z, int dir);

    public static JSObject GetInstanceInfo(Int3 coord, Direction dir)
        => GetInstanceInfo(coord.X, coord.Y, coord.Z, (int)dir);

    [JSImport("instantiate", nameof(Solid))]
    private static partial JSObject Instantiate(JSObject tree, JSObject[] instanceInfos);

    [JSImport("createPointLight", nameof(Solid))]
    private static partial JSObject CreatePointLight(float r, float g, float b, float intensity, float distance, bool nightOnly);

    private static JSObject CreatePointLight(Vector3 color, float intensity, float distance, bool nightOnly)
        => CreatePointLight(color.X, color.Y, color.Z, intensity, distance, nightOnly);

    [JSImport("createSpotLight", nameof(Solid))]
    private static partial JSObject CreateSpotLight(JSObject tree, float r, float g, float b, float intensity, float distance, float angleInner, float angleOuter, bool nightOnly);
    
    private static JSObject CreateSpotLight(JSObject tree, Vector3 color, float intensity, float distance, float angleInner, float angleOuter, bool nightOnly)
        => CreateSpotLight(tree, color.X, color.Y, color.Z, intensity, distance, angleInner, angleOuter, nightOnly);

    [JSImport("createSpotLightHelper", nameof(Solid))]
    public static partial JSObject CreateSpotLightHelper(JSObject spotLight);

    [JSImport("createPointLightHelper", nameof(Solid))]
    public static partial JSObject CreatePointLightHelper(JSObject pointLight);

    [JSImport("getChildren", nameof(Solid))]
    public static partial JSObject[] GetChildren(JSObject tree);

    [JSImport("getAllChildren", nameof(Solid))]
    private static partial JSObject[] GetAllChildren(JSObject tree);

    [JSImport("createVertexNormalHelper", nameof(Solid))]
    private static partial JSObject CreateVertexNormalHelper(JSObject mesh);

    [JSImport("createSphere", nameof(Solid))]
    private static partial JSObject CreateSphere(double radius);

    [JSImport("createEllipsoid", nameof(Solid))]
    private static partial JSObject CreateEllipsoid(double sizeX, double sizeY, double sizeZ);

    [JSImport("createCollisionMesh", nameof(Solid))]
    private static partial JSObject CreateCollisionMesh(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> vertexData,
        [JSMarshalAs<JSType.MemoryView>] Span<int> indices,
        bool isTrigger);

    [JSImport("triangulate", nameof(Solid))]
    private static partial int[] Triangulate(double[] positions3d);

    [JSImport("log", nameof(Solid))]
    private static partial void Log(JSObject tree);

    public static async Task<Solid> ParseAsync(
        Stream stream,
        GameVersion gameVersion,
        Dictionary<string, MaterialDto>? availableMaterials,
        string? terrainModifier = null,
        int? expectedMeshCount = null, 
        bool optimized = true, 
        bool receiveShadow = true,
        bool castShadow = true,
        bool noLights = false,
        bool isTrigger = false)
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

        var filePath = version >= 1 ? r.ReadString() : null;

        JSObject tree;
        if (optimized)
        {
            var geometries = new List<JSObject>();
            var materials = new List<JSObject>();
            await ReadTreeAsSingleGeometryAsync(r, gameVersion, version, rot: Mat3.Identity, pos: Vector3.Zero, geometries, materials, availableMaterials, terrainModifier, noLights, isTrigger);
            
            switch (geometries.Count)
            {
                case 0:
                    tree = Create(matrixAutoUpdate: false);
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
            tree = await ReadTreeAsNestedObjectsAsync(r, gameVersion, version, expectedMeshCount, receiveShadow, castShadow, availableMaterials, terrainModifier, noLights, isTrigger);
            Log(tree); // TODO: temporary
        }

        if (!string.IsNullOrEmpty(filePath))
        {
            SetUserData(tree, filePath.NormalizePath());
        }

        return new Solid(tree, filePath);
    }

    public void Instantiate(JSObject[] instanceInfos)
    {
        Instantiate(Object, instanceInfos);
    }

    private static async Task ReadTreeAsSingleGeometryAsync(
        AdjustedBinaryReader r, 
        GameVersion gameVersion,
        int version,
        Mat3 rot, 
        Vector3 pos,
        List<JSObject> geometries,
        List<JSObject> materials,
        Dictionary<string, MaterialDto>? availableMaterials,
        string? terrainModifier,
        bool noLights,
        bool isTrigger)
    {
        var childrenCount = r.Read7BitEncodedInt();

        var rotForTrans = rot;

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
            var delta = ReadVector3(r);
            pos += new Vector3(
                delta.X * rotForTrans.XX + delta.Y * rotForTrans.XY + delta.Z * rotForTrans.XZ,
                delta.X * rotForTrans.YX + delta.Y * rotForTrans.YY + delta.Z * rotForTrans.YZ,
                delta.X * rotForTrans.ZX + delta.Y * rotForTrans.ZY + delta.Z * rotForTrans.ZZ
            );
        }

        var geometry = ReadVisualAsGeometry(r, rot, pos);
        
        if (geometry is not null)
        {
            geometries.Add(geometry);

            var materialName = r.ReadString();
            var additionalMaterialProperties = r.ReadBoolean();

            if (additionalMaterialProperties)
            {
                r.ReadBoolean(); // castShadow, cannot work here rip
            }

            materials.Add(Material.GetOrCreateMaterial(materialName, gameVersion, availableMaterials, terrainModifier));
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

                await ReadTreeAsSingleGeometryAsync(r, gameVersion, version, rot, pos, i == 0 ? geometries : [], i == 0 ? materials : [], availableMaterials, terrainModifier, noLights, isTrigger);

                //AddLod(lod, lodTree, distance);
            }

            //Add(tree, lod);
        }

        var surface = ReadSurface(r, isTrigger);

        if (version >= 2)
        {
            var light = ReadLight(r, null, noLights);
        }

        var name = r.ReadString();

        for (var i = 0; i < childrenCount; i++)
        {
            await ReadTreeAsSingleGeometryAsync(r, gameVersion, version, rot, pos, geometries, materials, availableMaterials, terrainModifier, noLights, isTrigger);
        }
    }

    private static async Task<JSObject> ReadTreeAsNestedObjectsAsync(
        AdjustedBinaryReader r,
        GameVersion gameVersion,
        int version,
        int? expectedMeshCount, 
        bool receiveShadow, 
        bool castShadow,
        Dictionary<string, MaterialDto>? availableMaterials,
        string? terrainModifier,
        bool noLights,
        bool isTrigger)
    {
        var tree = Create(matrixAutoUpdate: true);

        var childrenCount = r.Read7BitEncodedInt();

        if (r.ReadBoolean())
        {
            var mat3 = ReadMatrix3(r);
            SetRotationMatrix(tree, mat3.XX, mat3.XY, mat3.XZ, mat3.YX, mat3.YY, mat3.YZ, mat3.ZX, mat3.ZY, mat3.ZZ);
        }

        if (r.ReadBoolean())
        {
            var pos = ReadVector3(r);
            SetPosition(tree, pos.X, pos.Y, pos.Z);
        }

        var geometry = ReadVisualAsGeometry(r, rot: Mat3.Identity, pos: Vector3.Zero);

        if (geometry is not null)
        {
            var materialName = r.ReadString();
            var additionalMaterialProperties = r.ReadBoolean();

            if (additionalMaterialProperties)
            {
                castShadow = r.ReadBoolean();
            }
            
            var visual = expectedMeshCount.HasValue
                ? CreateInstancedMeshSingleMaterial(geometry, Material.GetOrCreateMaterial(materialName, gameVersion, availableMaterials, terrainModifier), expectedMeshCount.Value, receiveShadow, castShadow)
                : CreateMeshSingleMaterial(geometry, Material.GetOrCreateMaterial(materialName, gameVersion, availableMaterials, terrainModifier), receiveShadow, castShadow);
            Add(tree, visual);
        }

        await RestAsync(indexCounter);

        var mipLevelCount = r.Read7BitEncodedInt();

        if (mipLevelCount > 0)
        {
            var lod = CreateLod();

            for (var i = 0; i < mipLevelCount; i++)
            {
                var distance = r.ReadSingle();

                var lodTree = await ReadTreeAsNestedObjectsAsync(r, gameVersion, version, expectedMeshCount, receiveShadow, castShadow, availableMaterials, terrainModifier, noLights, isTrigger);

                AddLod(lod, lodTree, distance);
            }

            Add(tree, lod);
        }

        var surface = ReadSurface(r, isTrigger);

        if (surface is not null)
        {
            Add(tree, surface);
        }

        if (version >= 2)
        {
            var light = ReadLight(r, tree, noLights);

            if (light is not null)
            {
                Add(tree, light);
            }
        }

        var name = r.ReadString();

        SetName(tree, name);

        for (var i = 0; i < childrenCount; i++)
        {
            Add(tree, await ReadTreeAsNestedObjectsAsync(r, gameVersion, version, expectedMeshCount, receiveShadow, castShadow, availableMaterials, terrainModifier, noLights, isTrigger));
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
                    vertex.X * rot.YX + vertex.Y * rot.YY + vertex.Z * rot.YZ + pos.Y,
                    vertex.X * rot.ZX + vertex.Y * rot.ZY + vertex.Z * rot.ZZ + pos.Z
                );
            }
        }

        Span<byte> normalData = [];

        if (hasNormals)
        {
            // Parse normals
            normalData = r.ReadBytes(vertexCount * 3 * sizeof(float));

            if (rot != Mat3.Identity || pos != Vector3.Zero)
            {
                var normals = MemoryMarshal.Cast<byte, Vec3>(normalData);

                for (var i = 0; i < normals.Length; i++)
                {
                    var normal = normals[i];
                    normals[i] = new Vec3(
                        normal.X * rot.XX + normal.Y * rot.XY + normal.Z * rot.XZ,
                        normal.X * rot.YX + normal.Y * rot.YY + normal.Z * rot.YZ,
                        normal.X * rot.ZX + normal.Y * rot.ZY + normal.Z * rot.ZZ
                    ).GetNormalized();
                }
            }
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

        return CreateGeometry(vertexData, normalData, indices, uvData);
    }

    private static JSObject? ReadSurface(AdjustedBinaryReader r, bool isTrigger)
    {
        if (!r.ReadBoolean())
        {
            return null;
        }

        switch ((SurfaceType)r.Read7BitEncodedInt())
        {
            case SurfaceType.Sphere:
                var size = r.ReadSingle();
                return CreateSphere(size);
            case SurfaceType.Ellipsoid:
                var sizeX = r.ReadSingle();
                var sizeY = r.ReadSingle();
                var sizeZ = r.ReadSingle();
                return CreateEllipsoid(sizeX, sizeY, sizeZ);
            case SurfaceType.Mesh:
                var vertexCount = r.Read7BitEncodedInt();
                Span<byte> vertices = r.ReadBytes(vertexCount * 3 * sizeof(float));
                
                var triCount = r.Read7BitEncodedInt();
                var intSize = r.ReadByte();

                // 1 mat index byte, 3 indices
                Span<byte> triBuffer = r.ReadBytes(triCount * (1 + intSize * 3));

                // material index is not yet processed

                Span<int> indices = stackalloc int[triCount * 3];

                switch (intSize)
                {
                    case 1:
                        for (var i = 0; i < triCount; i++)
                        {
                            indices[i * 3] = triBuffer[i * 4 + 1];
                            indices[i * 3 + 1] = triBuffer[i * 4 + 2];
                            indices[i * 3 + 2] = triBuffer[i * 4 + 3];
                        }
                        break;
                    case 2:
                        for (var i = 0; i < triCount; i++)
                        {
                            indices[i * 3] = BitConverter.ToUInt16(triBuffer.Slice(i * 7 + 1, 2));
                            indices[i * 3 + 1] = BitConverter.ToUInt16(triBuffer.Slice(i * 7 + 3, 2));
                            indices[i * 3 + 2] = BitConverter.ToUInt16(triBuffer.Slice(i * 7 + 5, 2));
                        }
                        break;
                    case 4:
                        for (var i = 0; i < triCount; i++)
                        {
                            indices[i * 3] = BitConverter.ToInt32(triBuffer.Slice(i * 13 + 1, 4));
                            indices[i * 3 + 1] = BitConverter.ToInt32(triBuffer.Slice(i * 13 + 5, 4));
                            indices[i * 3 + 2] = BitConverter.ToInt32(triBuffer.Slice(i * 13 + 9, 4));
                        }
                        break;
                }

                return CreateCollisionMesh(vertices, indices, isTrigger);
            default:
                throw new InvalidDataException("Unknown surface type");
        }
    }

    private static JSObject? ReadLight(AdjustedBinaryReader r, JSObject? tree, bool noLights)
    {
        if (!r.ReadBoolean())
        {
            return null;
        }

        var nightOnly = r.ReadBoolean();
        var color = ReadVector3(r);
        var intensity = r.ReadSingle();

        switch (r.Read7BitEncodedInt())
        {
            case 0:
                return CreatePointLight(color, intensity, distance: 64, nightOnly);
            case 1: // light ball (point)
                var radius = r.ReadSingle();
                return tree is null || noLights ? null : CreatePointLight(color, intensity, radius, nightOnly);
            case 2: // light spot
                radius = r.ReadSingle();
                var angleInner = r.ReadSingle();
                var angleOuter = r.ReadSingle();
                return tree is null || noLights ? null : CreateSpotLight(tree, color, intensity, radius, angleInner, angleOuter, nightOnly);
            default:
                throw new InvalidDataException("Unknown light type");
        }
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

    public static Task<Solid> CreateFromSolidAsync(CPlugSolid solid, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        if (solid.Tree is not CPlugTree tree)
        {
            throw new InvalidDataException("Solid tree is null");
        }

        return Task.FromResult(new Solid(CreateObjectFromTree(tree, gameVersion, availableMaterials), filePath: null));
    }

    private static JSObject CreateObjectFromTree(CPlugTree plugTree, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        var tree = Create(matrixAutoUpdate: true);

        SetName(tree, plugTree.Name);

        var iso4 = plugTree.Location ?? Iso4.Identity;
        SetRotationMatrix(tree, iso4.XX, iso4.XY, iso4.XZ, iso4.YX, iso4.YY, iso4.YZ, iso4.ZX, iso4.ZY, iso4.ZZ);
        SetPosition(tree, iso4.TX, iso4.TY, iso4.TZ);

        if (plugTree.Visual is CPlugVisualIndexedTriangles visual)
        {
            var materialName = GbxPath.ChangeExtension(plugTree.ShaderFile?.FilePath, null);

            if (materialName is null)
            {
                if (plugTree.Name.StartsWith('d'))
                {
                    materialName = "d";
                }
                else if (plugTree.Name.StartsWith('s'))
                {
                    materialName = "s";
                }
                else if (plugTree.Name.StartsWith('p'))
                {
                    materialName = "p";
                }
                else if (plugTree.Name.StartsWith('g'))
                {
                    materialName = ":Glass";
                }
                else
                {
                    materialName = "";
                }
            }

            var mesh = CreateMeshFromVisual(visual, materialName, gameVersion, availableMaterials);
            Add(tree, mesh);
        }

        if (plugTree is CPlugTreeVisualMip lodTree)
        {
            var lod = CreateLod();

            foreach (var level in lodTree.Levels)
            {
                var levelTree = CreateObjectFromTree(level.Tree, gameVersion, availableMaterials);
                AddLod(lod, levelTree, level.FarZ);
            }

            Add(tree, lod);
        }

        foreach (var child in plugTree.Children)
        {
            Add(tree, CreateObjectFromTree(child, gameVersion, availableMaterials));
        }

        return tree;
    }

    private static JSObject CreateMeshFromVisual(CPlugVisualIndexedTriangles visual, string materialName, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        bool hasNormals;
        Span<Vec3> positions;
        Span<Vec3> normals;

        if (visual.Vertices.Length > 0)
        {
            hasNormals = visual.Vertices.Any(x => x.Normal.HasValue);

            positions = new Vec3[visual.Vertices.Length];
            normals = new Vec3[hasNormals ? visual.Vertices.Length : 0];

            for (var i = 0; i < visual.Vertices.Length; i++)
            {
                var vertex = visual.Vertices[i];
                positions[i] = visual.Vertices[i].Position;

                if (hasNormals)
                {
                    normals[i] = visual.Vertices[i].Normal.GetValueOrDefault();
                }
            }
        }
        else
        {
            hasNormals = visual.VertexStreams.Any(x => x.Normals?.Length > 0);
            positions = visual.VertexStreams.SelectMany(x => x.Positions ?? []).ToArray();
            normals = hasNormals ? visual.VertexStreams.SelectMany(x => x.Normals ?? []).ToArray() : [];
        }

        Span<byte> vertexData = MemoryMarshal.AsBytes<Vec3>(positions);
        Span<byte> normalData = hasNormals ? MemoryMarshal.AsBytes<Vec3>(normals) : [];
        Span<int> indices = visual.IndexBuffer?.Indices ?? [];

        var uvs = visual.TexCoords.SelectMany(x => x.TexCoords).Select(x => x.UV).ToArray().AsSpan();
        Span<byte> uvData = MemoryMarshal.AsBytes<Vec2>(uvs);

        var geometry = CreateGeometry(vertexData, normalData, indices, uvData);

        return CreateMeshSingleMaterial(geometry, Material.GetOrCreateMaterial(materialName, gameVersion, availableMaterials, terrainModifier: null, endsWith: true), receiveShadow: true, castShadow: true);
    }

    public static async Task<Solid> CreateFromSolid2Async(CPlugSolid2Model solid2, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        return new Solid(await CreateObjectFromSolid2Async(solid2, gameVersion, availableMaterials), filePath: null);
    }

    public static Task<JSObject> CreateObjectFromSolid2Async(CPlugSolid2Model solid2, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        var tree = Create(matrixAutoUpdate: true);

        foreach (var geom in solid2.ShadedGeoms ?? [])
        {
            if (solid2.Visuals?[geom.VisualIndex] is not CPlugVisualIndexedTriangles visual)
            {
                continue;
            }

            var materialName = GetMaterialName(solid2, geom.MaterialIndex);

            var mesh = CreateMeshFromVisual(visual, materialName, gameVersion, availableMaterials);
            Add(tree, mesh);
        }

        return Task.FromResult(tree);
    }

    private static string GetMaterialName(CPlugSolid2Model solid, int materialIndex)
    {
        if (solid.CustomMaterials is { Length: > 0 } customMaterials)
        {
            return customMaterials[materialIndex].MaterialUserInst?.Link ?? "";
        }

        if (solid.Materials is { Length: > 0 } materialsArray)
        {
            return GbxPath.GetFileNameWithoutExtension(materialsArray[materialIndex].File?.FilePath) ?? "";
        }

        if (solid.MaterialInsts is { Length: > 0 } materialInsts)
        {
            return materialInsts[materialIndex].Link ?? "";
        }

        if (solid.MaterialIds is { Length: > 0 } materialIds)
        {
            return materialIds[materialIndex];
        }

        return "";
    }

    public static async Task<Solid> CreateFromPrefabAsync(CPlugPrefab prefab, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        var tree = Create(matrixAutoUpdate: true);

        var i = 0;
        foreach (var ent in prefab.Ents)
        {
            if (ent.Model is CPlugStaticObjectModel { Mesh: not null } staticObject)
            {
                var subTree = await CreateObjectFromSolid2Async(staticObject.Mesh, gameVersion, availableMaterials);
                Add(tree, subTree);

                SetName(subTree, $"#{i}");
            }
            else if (ent.Model is CPlugDynaObjectModel { Mesh: not null } dynaObject)
            {
                var subTree = await CreateObjectFromSolid2Async(dynaObject.Mesh, gameVersion, availableMaterials);
                Add(tree, subTree);

                SetName(subTree, $"#{i}");
            }

            i++;
        }

        return new Solid(tree, filePath: null);
    }

    public static Task<Solid> CreateFromCrystalAsync(CPlugCrystal crystal, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        var tree = Create(matrixAutoUpdate: true);

        foreach (var layer in crystal.Layers)
        {
            if (layer is CPlugCrystal.GeometryLayer { Crystal: not null } geometryLayer)
            {
                var positions = geometryLayer.Crystal.Positions;
                var indices = new List<int>();
                Span<Vec2> uvs = new Vec2[positions.Length];

                foreach (var face in geometryLayer.Crystal.Faces)
                {
                    if (face.Vertices.Length > 3)
                    {
                        var triangulatedInds = Triangulate(face.Vertices.SelectMany(x =>
                        {
                            var pos = positions[x.Index];
                            return new double[] { pos.X, pos.Y, pos.Z };
                        }).ToArray());

                        var projectedInds = triangulatedInds.Select(x => face.Vertices[x].Index);
                        indices.AddRange(projectedInds);

                        foreach (var index in triangulatedInds)
                        {
                            var vert = face.Vertices[index];
                            uvs[vert.Index] = vert.TexCoord;
                        }
                    }
                    else
                    {
                        indices.AddRange(face.Vertices.Select(x => x.Index));

                        foreach (var vert in face.Vertices)
                        {
                            uvs[vert.Index] = vert.TexCoord;
                        }
                    }
                }

                Span<byte> vertexData = MemoryMarshal.AsBytes<Vec3>(positions.AsSpan());
                Span<byte> uvData = MemoryMarshal.AsBytes<Vec2>(uvs);

                var geometry = CreateGeometry(vertexData, [], CollectionsMarshal.AsSpan(indices), uvData, computeNormals: true);

                var mesh = CreateMeshSingleMaterial(geometry, Material.GetOrCreateMaterial("", gameVersion, availableMaterials, terrainModifier: null, endsWith: true), receiveShadow: true, castShadow: true);
                Add(tree, mesh);
            }
        }

        return Task.FromResult(new Solid(tree, filePath: null));
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

    public JSObject[] GetAllChildren()
    {
        return GetAllChildren(Object);
    }

    public void ToggleVertexNormalHelper(Scene scene)
    {
        if (vertexNormalHelpers.Length > 0)
        {
            foreach (var vertexNormalHelper in vertexNormalHelpers)
            {
                scene.Remove(vertexNormalHelper);
            }
            vertexNormalHelpers = [];
            return;
        }

        vertexNormalHelpers = GetAllChildren()
            .Where(x => x.GetPropertyAsBoolean("isMesh"))
            .Select(CreateVertexNormalHelper)
            .ToArray();

        foreach (var vertexNormalHelper in vertexNormalHelpers)
        {
            scene.Add(vertexNormalHelper);
        }
    }

    public void ToggleCollision(Scene scene, Solid? collision)
    {
        if (collision is null)
        {
            if (this.collision is not null)
            {
                scene.Remove(this.collision);
                this.collision = null;
            }
            return;
        }

        this.collision = collision;
        scene.Add(collision.Object);
    }

    public void ToggleObjectLinks(Scene scene, Solid[] objectLinks)
    {
        if (objectLinks.Length == 0)
        {
            if (this.objectLinks.Length > 0)
            {
                foreach (var link in this.objectLinks)
                {
                    scene.Remove(link.Object);
                }
                this.objectLinks = [];
            }
            return;
        }

        this.objectLinks = objectLinks;
        foreach (var link in objectLinks)
        {
            scene.Add(link.Object);
        }
    }

    public void ToggleWireframe()
    {
        if (wireframeMemorizedMaterials.Count > 0)
        {
            foreach (var child in GetAllChildren())
            {
                if (!child.GetPropertyAsBoolean("isMesh"))
                {
                    continue;
                }

                var material = child.GetPropertyAsJSObject("material");

                if (material is null)
                {
                    continue;
                }

                child.SetProperty("material", wireframeMemorizedMaterials[child]);
            }
            wireframeMemorizedMaterials = [];
            return;
        }

        var memorizedMaterials = new Dictionary<JSObject, JSObject>();

        foreach (var child in GetAllChildren())
        {
            if (!child.GetPropertyAsBoolean("isMesh"))
            {
                continue;
            }

            var material = child.GetPropertyAsJSObject("material");

            if (material is null)
            {
                continue;
            }

            memorizedMaterials.Add(child, material);

            var wireframeMaterial = Material.GetWireframeMaterial();

            child.SetProperty("material", wireframeMaterial);
        }

        wireframeMemorizedMaterials = memorizedMaterials;
    }

    public void Remove(Scene? scene)
    {
        if (scene is null)
        {
            return;
        }

        foreach (var vertexNormalHelper in vertexNormalHelpers)
        {
            scene.Remove(vertexNormalHelper);
        }

        vertexNormalHelpers = [];

        if (collision is not null)
        {
            scene.Remove(collision);
            collision = null;
        }

        foreach (var link in objectLinks)
        {
            scene.Remove(link.Object);
        }

        scene.Remove(Object);
    }

    public Dictionary<string, string> GetAllMaterials()
    {
        var materials = new Dictionary<string, string>();

        foreach (var child in GetAllChildren())
        {
            if (!child.GetPropertyAsBoolean("isMesh"))
            {
                continue;
            }

            var material = child.GetPropertyAsJSObject("material");

            if (material is null)
            {
                continue;
            }

            var materialName = material.GetPropertyAsString("name");

            if (string.IsNullOrEmpty(materialName))
            {
                continue;
            }

            if (materials.ContainsKey(materialName))
            {
                continue;
            }

            var userData = material.GetPropertyAsJSObject("userData");

            if (userData is null)
            {
                continue;
            }

            var shaderName = userData.GetPropertyAsString("shaderName");

            materials.Add(materialName, shaderName ?? "");
        }

        return materials;
    }
}
