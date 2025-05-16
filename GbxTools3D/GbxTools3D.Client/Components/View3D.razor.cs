using GBX.NET;
using GBX.NET.Engines.Game;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Client.Models;
using GbxTools3D.Client.Modules;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Components;

[SupportedOSPlatform("browser")]
public partial class View3D : ComponentBase
{
    private readonly HttpClient http;

    private JSObject? rendererModule;
    private JSObject? sceneModule;
    private JSObject? cameraModule;
    private JSObject? solidModule;
    private JSObject? materialModule;
    private JSObject? animationModule;

    private JSObject? renderer;
    private Scene? scene;
    private Camera? mapCamera;

    private Solid? focusedSolid;

    private bool mapLoadAttempted;

    [Parameter, EditorRequired]
    public GameVersion GameVersion { get; set; }

    [Parameter]
    public string? CollectionName { get; set; }

    [Parameter]
    public CGameCtnChallenge? Map { get; set; }

    [Parameter]
    public string? BlockName { get; set; }

    [Parameter]
    public string? VehicleName { get; set; }

    [Parameter]
    public EventCallback BeforeMapLoad { get; set; }

    [Parameter]
    public EventCallback<RenderDetails?> OnRenderDetails { get; set; }

    public BlockInfoDto? CurrentBlockInfo { get; private set; }

    public int RenderDetailsRefreshInterval { get; set; } = 500;

    private Dictionary<string, BlockInfoDto> blockInfos = [];
    private Dictionary<Int3, DecorationSizeDto> decorations = [];
    private Dictionary<string, MaterialDto> materials = [];
    private Dictionary<string, VehicleDto> vehicles = [];

    private readonly CancellationTokenSource cts = new();
    private Timer? timer;

    public View3D(HttpClient http)
    {
        this.http = http;
    }

    protected override void OnInitialized()
    {
        if (RendererInfo.IsInteractive)
        {
            timer = new Timer(TimerCallback, null, 0, RenderDetailsRefreshInterval);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (firstRender)
        {
            await LoadSceneAsync(cts.Token);
        }

        await TryLoadMapAsync(cts.Token);
        await TryLoadBlockAsync(cts.Token);
        await TryLoadVehicleAsync(cts.Token);
    }

    private async Task LoadSceneAsync(CancellationToken cancellationToken)
    {
        rendererModule = await JSHost.ImportAsync(nameof(Renderer), "../js/renderer.js", cancellationToken);
        sceneModule = await JSHost.ImportAsync(nameof(Scene), "../js/scene.js", cancellationToken);
        cameraModule = await JSHost.ImportAsync(nameof(Camera), "../js/camera.js", cancellationToken);
        solidModule = await JSHost.ImportAsync(nameof(Solid), "../js/solid.js", cancellationToken);
        materialModule = await JSHost.ImportAsync(nameof(Material), "../js/material.js", cancellationToken);
        animationModule = await JSHost.ImportAsync(nameof(Animation), "../js/animation.js", cancellationToken);

        renderer = Renderer.Create();
        scene = new Scene();
        mapCamera = new Camera();

        Renderer.Camera = mapCamera;
        Renderer.Scene = scene;
    }

    private async Task<bool> TryFetchDataAsync(
        bool loadBlockInfos = false,
        bool loadDecorations = false,
        bool loadMaterials = false,
        bool loadVehicles = false,
        CancellationToken cancellationToken = default)
    {
        var collection = CollectionName ?? Map?.Collection;

        var tasks = new List<Task<HttpResponseMessage>>();

        var blockInfosTask = default(Task<HttpResponseMessage>);
        var decorationTask = default(Task<HttpResponseMessage>);

        if (collection is not null)
        {
            blockInfosTask = loadBlockInfos && blockInfos.Count == 0 ? http.GetAsync($"/api/blocks/{GameVersion}/{collection}", cts.Token) : null;
            decorationTask = loadDecorations && decorations.Count == 0 ? http.GetAsync($"/api/decorations/{GameVersion}/{collection}", cts.Token) : null;
        }

        var materialTask = loadMaterials && materials.Count == 0 ? http.GetAsync($"/api/materials/{GameVersion}", cts.Token) : null;
        var vehicleTask = loadVehicles && vehicles.Count == 0 ? http.GetAsync($"/api/vehicles/{GameVersion}", cts.Token) : null;

        if (blockInfosTask is not null) tasks.Add(blockInfosTask);
        if (decorationTask is not null) tasks.Add(decorationTask);
        if (materialTask is not null) tasks.Add(materialTask);
        if (vehicleTask is not null) tasks.Add(vehicleTask);

        if (tasks.Count == 0)
        {
            return false;
        }

        await foreach (var task in Task.WhenEach(tasks).WithCancellation(cts.Token))
        {
            task.Result.EnsureSuccessStatusCode(); // show note message that user has to wait, if the block list isnt available yet

            if (task == blockInfosTask)
            {
                blockInfos = (await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListBlockInfoDto, cts.Token))?
                    .ToDictionary(x => x.Name) ?? [];
            }
            else if (task == decorationTask)
            {
                decorations = (await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListDecorationSizeDto, cts.Token))?
                    .ToDictionary(x => x.Size) ?? [];
            }
            else if (task == materialTask)
            {
                materials = await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.DictionaryStringMaterialDto, cts.Token) ?? [];
            }
            else if (task == vehicleTask)
            {
                vehicles = (await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListVehicleDto, cts.Token))?
                    .ToDictionary(x => x.Name) ?? [];
            }
        }

        return true;
    }

    private async Task<bool> TryLoadBlockAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null || BlockName is null)
        {
            return false;
        }

        if (focusedSolid is not null)
        {
            scene?.Remove(focusedSolid);
            //focusedSolid.Dispose();
            focusedSolid = null;
        }

        // initial camera position
        var center = new Vec3(16, 4, 16);
        var position = center * (4, 6, 1);

        mapCamera.Position = position;
        mapCamera.CreateMapControls(renderer, center);
        //

        try
        {
            await TryFetchDataAsync(loadBlockInfos: true, loadMaterials: true, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return false;
        }

        if (!blockInfos.TryGetValue(BlockName, out var blockInfo))
        {
            return false;
        }

        var isGround = blockInfo.AirVariants.Count == 0;
        var variant = 0;
        var subVariant = 0;

        var units = isGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

        var blockSize = CollectionName is null ? (32, 8, 32) : new Id(CollectionName).GetBlockSize();
        var size = new Int3(units.Select(x => x.Offset.X).Max() + 1, units.Select(x => x.Offset.Y).Max() + 1, units.Select(x => x.Offset.Z).Max() + 1);
        var realSize = size * blockSize;

        // camera position after knowing block details
        center = new Vec3(realSize.X / 2f, realSize.Y / 2f, realSize.Z / 2f);
        position = center * (4, 6, 1);

        mapCamera.Position = position;
        mapCamera.CreateMapControls(renderer, center);
        //

        var hash = $"GbxTools3D|Solid|{GameVersion}|{BlockName}|{isGround}MyGuy|{variant}|{subVariant}|PleaseDontAbuseThisThankYou:*".Hash();

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        focusedSolid = await Solid.ParseAsync(stream, materials, expectedMeshCount: null, optimized: false);
        scene?.Add(focusedSolid);

        CurrentBlockInfo = blockInfo;

        return true;
    }

    private async Task<bool> TryLoadVehicleAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null || VehicleName is null)
        {
            return false;
        }

        if (focusedSolid is not null)
        {
            scene?.Remove(focusedSolid);
            //focusedSolid.Dispose();
            focusedSolid = null;
        }

        mapCamera.Position = new Vec3(0, 5, 10);
        mapCamera.CreateMapControls(renderer, default);

        try
        {
            await TryFetchDataAsync(loadMaterials: true, loadVehicles: true, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return false;
        }

        if (!vehicles.TryGetValue(VehicleName, out var vehicleInfo))
        {
            return false;
        }

        var hash = $"GbxTools3D|Vehicle|{GameVersion}|{VehicleName}|WhyDidYouNotHelpMe?".Hash();

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        focusedSolid = await Solid.ParseAsync(stream, materials, expectedMeshCount: null, optimized: false);
        scene?.Add(focusedSolid);

        return true;
    }

    private async Task<bool> TryLoadMapAsync(CancellationToken cancellationToken = default)
    {
        if (Map is null || mapCamera is null || renderer is null || mapLoadAttempted)
        {
            return false;
        }

        mapLoadAttempted = true; // because map load doesnt have good cleanup process, this hack will prevent multiple map loads

        await BeforeMapLoad.InvokeAsync();

        var blockSize = Map.Collection.GetValueOrDefault().GetBlockSize();
        var center = new Vec3(Map.Size.X * blockSize.X / 2f, /*baseHeight * blockSize.Y*/0, Map.Size.Z * blockSize.Z / 2f - Map.Size.Z * blockSize.Z * 0.15f);

        // setup camera
        mapCamera.Position = new Vec3(center.X, Map.Size.Z * 0.5f * blockSize.Z, 0);
        mapCamera.CreateMapControls(renderer, center);

        await TryFetchDataAsync(loadBlockInfos: true, loadDecorations: true, loadMaterials: true, cancellationToken: cancellationToken);

        var baseHeight = await PlaceDecorationAsync(Map, cancellationToken);

        await PlaceBlocksAsync(Map, baseHeight, cancellationToken);

        return true;
    }

    private async Task<int> PlaceDecorationAsync(CGameCtnChallenge map, CancellationToken cancellationToken)
    {
        var baseHeight = 5;

        if (!decorations.TryGetValue(map.Size, out var decoSize))
        {
            return baseHeight;
        }

        baseHeight = decoSize.BaseHeight;

        var deco = decoSize.Decorations
            .FirstOrDefault(x => x.Name == map.Decoration.Id);

        var size = $"{map.Size.X}x{map.Size.Y}x{map.Size.Z}";

        var tasks = new Dictionary<Task<HttpResponseMessage>, Iso4>();

        foreach (var sceneObject in decoSize.Scene.Where(x => x.Solid is not null))
        {
            if (Path.GetFileNameWithoutExtension(sceneObject.Solid)?.Contains("FarClip") == true)
            {
                continue;
            }

            var hash = $"GbxTools3D|Decoration|{GameVersion}|{map.Collection}|{size}|{sceneObject.Solid}|Je te hais".Hash();

            tasks.Add(http.GetAsync($"/api/mesh/{hash}", cancellationToken), sceneObject.Location);
        }

        await foreach (var meshResponseTask in Task.WhenEach(tasks.Keys).WithCancellation(cancellationToken))
        {
            using var meshResponse = await meshResponseTask;

            if (!meshResponse.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);
            var solid = await Solid.ParseAsync(stream, materials, expectedMeshCount: null, receiveShadow: false, castShadow: false);
            solid.Location = tasks[meshResponseTask];
            scene?.Add(solid);
        }

        return baseHeight;
    }

    private async Task PlaceBlocksAsync(CGameCtnChallenge map, int baseHeight, CancellationToken cancellationToken)
    {
        var coveredZoneBlocks = GetCoveredZoneBlocks().ToHashSet();

        var baseZoneBlock = blockInfos.Values.FirstOrDefault(x => x.IsDefaultZone);
        var baseZoneBlocks = CreateBaseZoneBlocks(baseZoneBlock, baseHeight);
        var clipBlocks = CreateClipBlocks();

        var uniqueBlockVariants = baseZoneBlocks
            .Concat(map.GetBlocks())
            .Where(x => !x.IsClip && !coveredZoneBlocks.Contains(x))
            .Concat(clipBlocks)
            .ToLookup(x => new UniqueVariant(x.Name, x.IsGround, x.Variant, x.SubVariant));

        var responseTasks = new Dictionary<UniqueVariant, Task<HttpResponseMessage>>();

        var counter = 0;
        foreach (var uniqueGroup in uniqueBlockVariants)
        {
            var (name, isGround, variant, subVariant) = uniqueGroup.Key;

            var hash = $"GbxTools3D|Solid|{GameVersion}|{name}|{isGround}MyGuy|{variant}|{subVariant}|PleaseDontAbuseThisThankYou:*".Hash();

            responseTasks.Add(uniqueGroup.Key, http.GetAsync($"/api/mesh/{hash}", cancellationToken));

            if (counter > 20)
            {
                await Task.Delay(20, cancellationToken);
                counter = 0;
            }

            await ProcessBlockResponsesAsync(responseTasks, maxRequestsToProcess: 10, uniqueBlockVariants, map, cancellationToken);

            counter++;
        }

        while (responseTasks.Count > 0)
        {
            await Task.Delay(20, cancellationToken);
            await ProcessBlockResponsesAsync(responseTasks, maxRequestsToProcess: null, uniqueBlockVariants, map, cancellationToken);
        }
    }

    internal async Task<Solid?> LoadGhostAsync(CGameCtnGhost ghost, CancellationToken cancellationToken = default)
    {
        await TryFetchDataAsync(loadMaterials: true, loadVehicles: true, cancellationToken: cancellationToken);

        var vehicleName = ghost.PlayerModel?.Id;

        if (vehicleName is null || !vehicles.TryGetValue(vehicleName, out var vehicleInfo))
        {
            return null;
        }

        var hash = $"GbxTools3D|Vehicle|{GameVersion}|{vehicleName}|WhyDidYouNotHelpMe?".Hash();

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        var vehicle = await Solid.ParseAsync(stream, materials, expectedMeshCount: null, optimized: false, castShadow: false);
        scene?.Add(vehicle);

        var camera = new Camera(vehicleInfo.CameraFov);
        Camera.Follow(vehicle.Object, vehicleInfo.CameraFar, vehicleInfo.CameraUp, vehicleInfo.CameraLookAtFactor);
        Renderer.Camera = camera;

        return vehicle;
    }

    public void Unfollow()
    {
        Camera.Unfollow();
        //mapCamera?.CreateMapControls(renderer, default);
    }

    private async Task ProcessBlockResponsesAsync(
        Dictionary<UniqueVariant, Task<HttpResponseMessage>> responseTasks,
        int? maxRequestsToProcess,
        ILookup<UniqueVariant, CGameCtnBlock> uniqueBlockVariantLookup,
        CGameCtnChallenge map,
        CancellationToken cancellationToken)
    {
        var tasksToRemove = new List<UniqueVariant>();

        foreach (var (variant, task) in responseTasks.Where(task => task.Value.IsCompleted))
        {
            using var response = await task;

            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                var expectedCount = uniqueBlockVariantLookup[variant].Count();
                var solid = await Solid.ParseAsync(stream, materials, expectedCount);

                PlaceBlocks(solid, variant, uniqueBlockVariantLookup[variant], map.Collection.GetValueOrDefault().GetBlockSize(), map);
            }

            tasksToRemove.Add(variant);

            if (maxRequestsToProcess.HasValue && tasksToRemove.Count >= maxRequestsToProcess)
            {
                break;
            }
        }

        foreach (var variant in tasksToRemove)
        {
            responseTasks[variant].Dispose();
            responseTasks.Remove(variant);
        }
    }

    private void PlaceBlocks(Solid solid, UniqueVariant variant, IEnumerable<CGameCtnBlock> blocks, Int3 blockSize, CGameCtnChallenge map)
    {
        if (scene is null)
        {
            return;
        }

        var blockCoordSize = new Int3(1, 1, 1);
        var height = 0;

        if (blockInfos?.TryGetValue(variant.Name, out var blockInfo) == true)
        {
            var airUnits = blockInfo.AirUnits;
            var groundUnits = blockInfo.GroundUnits;
            blockCoordSize = variant.IsGround
                ? (groundUnits.Length > 1 ? new Int3(
                    groundUnits.Max(unit => unit.Offset.X) + 1,
                    groundUnits.Max(unit => unit.Offset.Y) + 1,
                    groundUnits.Max(unit => unit.Offset.Z) + 1) : blockCoordSize)
                : (airUnits.Length > 1 ? new Int3(
                    airUnits.Max(unit => unit.Offset.X) + 1,
                    airUnits.Max(unit => unit.Offset.Y) + 1,
                    airUnits.Max(unit => unit.Offset.Z) + 1) : blockCoordSize);
            height = blockInfo.Height ?? 0;
        }

        var instanceInfos = new List<JSObject>();

        foreach (var block in blocks)
        {
            var actualCoord = block.Coord + block.Direction switch
            {
                Direction.North => (0, 0, 0),
                Direction.East => (blockCoordSize.Z, 0, 0),
                Direction.South => (blockCoordSize.X, 0, blockCoordSize.Z),
                Direction.West => (0, 0, blockCoordSize.X),
                _ => throw new ArgumentException("Invalid block direction")
            };

            var instanceInfo = Solid.GetInstanceInfoFromBlock((actualCoord + (0, -height - map.DecoBaseHeightOffset, 0)) * blockSize, block.Direction);

            instanceInfos.Add(instanceInfo);
        }

        solid.Instantiate(instanceInfos.ToArray());
        scene.Add(solid);
    }

    private IEnumerable<CGameCtnBlock> GetCoveredZoneBlocks()
    {
        if (Map is null || blockInfos is null)
        {
            yield break;
        }

        const bool isManiaPlanet = false;
        var groundPositions = new List<Int3>();

        foreach (var block in Map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height.HasValue)
            {
                continue;
            }

            PopulateGroundPositionsFromBlock(groundPositions, block, blockInfo);
        }

        foreach (var block in Map.GetBlocks())
        {
            if (!block.IsGround || !blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height is null)
            {
                continue;
            }

            if (groundPositions.Contains(block.Coord + (0, isManiaPlanet ? 0 : 1, 0)))
            {
                yield return block;
            }
        }

        static void PopulateGroundPositionsFromBlock(List<Int3> groundPositions, CGameCtnBlock block, BlockInfoDto blockInfo)
        {
            var units = block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

            if (units.Length == 1)
            {
                groundPositions.Add(block.Coord);
                return;
            }

            Span<Int3> rotatedUnits = stackalloc Int3[units.Length];

            RotateUnits(units, block.Direction, rotatedUnits, out var minX, out var minZ);

            // Adjust positions so the minimum X and Z become 0.
            foreach (var rotated in rotatedUnits)
            {
                groundPositions.Add(block.Coord + new Int3(rotated.X - minX, rotated.Y, rotated.Z - minZ));
            }
        }
    }

    private IEnumerable<CGameCtnBlock> CreateBaseZoneBlocks(BlockInfoDto? baseZoneInfo, int baseHeight)
    {
        if (Map is null || blockInfos is null || baseZoneInfo is null)
        {
            yield break;
        }

        var groundHeight = baseHeight + 1;
        var occupied = new HashSet<(int X, int Z)>();

        foreach (var block in Map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height.HasValue)
            {
                occupied.Add((block.Coord.X, block.Coord.Z));
                continue;
            }

            PopulateOccupiedZonesFromBlock(occupied, block, blockInfo, groundHeight);
        }

        // Iterate through the entire zone; if a coordinate isn't occupied, yield a new block.
        for (int x = 0; x < Map.Size.X; x++)
        {
            for (int z = 0; z < Map.Size.Z; z++)
            {
                if (!occupied.Contains((x, z)))
                {
                    yield return new CGameCtnBlock
                    {
                        Name = baseZoneInfo.Name,
                        Coord = new Int3(x, groundHeight - 1, z),
                        Direction = Direction.North,
                        IsGround = true
                    };
                }
            }
        }

        static void PopulateOccupiedZonesFromBlock(HashSet<(int X, int Z)> occupied, CGameCtnBlock block, BlockInfoDto blockInfo, int groundHeight)
        {
            var units = block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

            Span<Int3> rotatedUnits = stackalloc Int3[units.Length];

            RotateUnits(units, block.Direction, rotatedUnits, out var minX, out var minZ);

            // Adjust positions so the minimum X and Z become 0.
            foreach (var rotated in rotatedUnits)
            {
                var unit = block.Coord + new Int3(rotated.X - minX, rotated.Y, rotated.Z - minZ);
                if (unit.Y == groundHeight)
                {
                    occupied.Add((unit.X, unit.Z));
                }
            }
        }
    }

    private IEnumerable<CGameCtnBlock> CreateClipBlocks()
    {
        if (Map is null || blockInfos is null)
        {
            yield break;
        }

        var clipBlockDict = Map.GetBlocks()
            .Where(x => x.IsClip)
            .ToDictionary(x => x.Coord, x => x);

        var alreadyPlacedClips = new HashSet<(Int3, Direction)>();

        foreach (var block in Map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height.HasValue || blockInfo.IsRoad)
            {
                continue;
            }
            
            foreach (var clipBlock in CreateClipBlocks(block, blockInfo, clipBlockDict, alreadyPlacedClips))
            {
                yield return clipBlock;
            }
        }

        foreach (var (coord, block) in clipBlockDict)
        {
            if (!block.IsGround)
            {
                continue;
            }

            for (int i = 0; i < 4; i++)
            {
                var dir = (Direction)i;

                if (alreadyPlacedClips.Contains((coord, dir)))
                {
                    continue;
                }

                // TODO condition Fabric in Stadium here

                yield return new CGameCtnBlock
                {
                    Name = block.Name,
                    Coord = coord,
                    Direction = dir,
                    IsGround = block.IsGround
                };
            }
        }
    }

    private static IEnumerable<CGameCtnBlock> CreateClipBlocks(
        CGameCtnBlock block, 
        BlockInfoDto blockInfo, 
        Dictionary<Int3, CGameCtnBlock> clipBlockDict, 
        HashSet<(Int3, Direction)> alreadyPlacedClips)
    {
        var units = block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits;
        
        if (units.All(x => x.Clips is null or { Length: 0 }))
        {
            yield break;
        }

        var dirs = new HashSet<Direction>();

        var rotatedUnits = new Int3[units.Length];

        RotateUnits(units, block.Direction, rotatedUnits, out var minX, out var minZ);

        for (var i = 0; i < units.Length; i++)
        {
            var unit = units[i];
            var rotated = rotatedUnits[i];
            var unitCoord = block.Coord + new Int3(rotated.X - minX, rotated.Y, rotated.Z - minZ);

            foreach (var clip in unit.Clips ?? [])
            {
                // be careful with (int)clip.Dir, it also has Top and Bottom on 4 and 5
                var clipPopDir = (Direction)(((int)block.Direction + (int)clip.Dir) % 4);
                Int3 clipPop = clipPopDir switch
                {
                    Direction.North => (0, 0, 1),
                    Direction.East => (-1, 0, 0),
                    Direction.South => (0, 0, -1),
                    Direction.West => (1, 0, 0),
                    _ => throw new ArgumentException("Invalid clip direction")
                };

                var finalDir = (Direction)(((int)clipPopDir + 2) % 4);
                var finalCoord = unitCoord + clipPop;

                if (clipBlockDict.TryGetValue(finalCoord, out var clipBlock))
                {
                    yield return new CGameCtnBlock
                    {
                        Name = clip.Id,
                        Coord = finalCoord,
                        Direction = finalDir,
                        IsGround = clipBlock.IsGround
                    };

                    alreadyPlacedClips.Add((finalCoord, finalDir));
                }
            }
        }
    }

    private static void RotateUnits(Span<BlockUnit> units, Direction dir, Span<Int3> rotatedUnits, out int minX, out int minZ)
    {
        minX = int.MaxValue;
        minZ = int.MaxValue;

        for (int i = 0; i < units.Length; i++)
        {
            var rotated = RotateUnit(units[i].Offset, dir);
            rotatedUnits[i] = rotated;
            if (rotated.X < minX) minX = rotated.X;
            if (rotated.Z < minZ) minZ = rotated.Z;
        }
    }

    private static Int3 RotateUnit(Int3 unit, Direction direction) => direction switch
    {
        Direction.East => new Int3(-unit.Z, unit.Y, unit.X),
        Direction.South => new Int3(-unit.X, unit.Y, -unit.Z),
        Direction.West => new Int3(unit.Z, unit.Y, -unit.X),
        _ => unit,
    };

    private int? framesBefore;

    protected async void TimerCallback(object? state)
    {
        var info = renderer?.GetPropertyAsJSObject("info");

        if (info is null)
        {
            await OnRenderDetails.InvokeAsync(null);
            return;
        }

        var fps = default(double?);
        var calls = default(int?);
        var triangles = default(int?);
        var geometries = default(int?);
        var textures = default(int?);

        var infoRender = info.GetPropertyAsJSObject("render");

        if (infoRender is not null) 
        {
            var framesNow = infoRender.GetPropertyAsInt32("frame");

            fps = (framesNow - framesBefore.GetValueOrDefault(framesNow)) * 1000 / (double)RenderDetailsRefreshInterval;

            framesBefore = framesNow;

            calls = infoRender.GetPropertyAsInt32("calls");
            triangles = infoRender.GetPropertyAsInt32("triangles");
        }

        var infoMemory = info.GetPropertyAsJSObject("memory");

        if (infoMemory is not null)
        {
            geometries = infoMemory.GetPropertyAsInt32("geometries");
            textures = infoMemory.GetPropertyAsInt32("textures");
        }

        await OnRenderDetails.InvokeAsync(new RenderDetails(fps, calls, triangles, geometries, textures));
    }

    public async ValueTask DisposeAsync()
    {
        cts.Cancel();
        cts.Dispose();

        if (RendererInfo.IsInteractive)
        {
            if (timer is not null)
            {
                await timer.DisposeAsync();
            }
            Unfollow();
            Renderer.Dispose();
        }

        scene = null;
        mapCamera = null;

        rendererModule?.Dispose();
        sceneModule?.Dispose();
        cameraModule?.Dispose();
        solidModule?.Dispose();
        materialModule?.Dispose();
        animationModule?.Dispose();
    }
}
