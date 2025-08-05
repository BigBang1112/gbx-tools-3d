using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Client.Models;
using GbxTools3D.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using GbxTools3D.Client.Enums;
using GbxTools3D.Client.Services;

namespace GbxTools3D.Client.Components;

[SupportedOSPlatform("browser")]
public partial class View3D : ComponentBase
{
    private readonly HttpClient http;
    private readonly IJSRuntime js;
    private readonly StateService stateService;

    private JSObject? rendererModule;
    private JSObject? sceneModule;
    private JSObject? cameraModule;
    private JSObject? solidModule;
    private JSObject? materialModule;
    private JSObject? animationModule;
    private JSObject? renderer;
    private Camera? mapCamera;
    private Camera? vehicleCamera;

    private DotNetObjectReference<View3D>? objRef;
    private IJSObjectReference? rendererModuleInterop;

    internal Scene? Scene { get; private set; }
    internal List<Solid> FocusedSolids { get; private set; } = [];

    private bool mapLoadAttempted;

    [Parameter]
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
    public string? DecorationName { get; set; }

    [Parameter]
    public string? SceneName { get; set; }

    [Parameter]
    public EventCallback AfterSceneLoad { get; set; }

    [Parameter]
    public EventCallback BeforeMapLoad { get; set; }

    [Parameter]
    public Action<RenderDetails?>? OnRenderDetails { get; set; }

    [Parameter]
    public EventCallback OnFocusedSolidsChange { get; set; }

    [Parameter]
    public bool IsCatalog { get; set; }

    [Parameter]
    public CPlugSolid? Solid1 { get; set; }

    [Parameter]
    public CPlugSolid2Model? Solid2 { get; set; }

    [Parameter]
    public CPlugPrefab? Prefab { get; set; }

    [Parameter]
    public CGameItemModel? Item { get; set; }

    [Parameter]
    public ZipArchive? SkinZip { get; set; }

    public event Action<IntersectionInfo>? OnIntersect;

    public BlockInfoDto? CurrentBlockInfo { get; private set; }

    public int RenderDetailsRefreshInterval { get; set; } = 500;

    private Dictionary<string, CollectionDto> collectionInfos = [];
    private Dictionary<string, BlockInfoDto> blockInfos = [];
    private ILookup<Int3, DecorationSizeDto> decorations = new Dictionary<Int3, DecorationSizeDto>()
        .ToLookup(x => x.Key, x => x.Value);
    public Dictionary<string, MaterialDto> Materials { get; private set; } = [];
    private Dictionary<string, VehicleDto> vehicles = [];

    private readonly CancellationTokenSource cts = new();
    private Timer? timer;

    private const int PillarOffset = 12;

    public View3D(HttpClient http, IJSRuntime js, StateService stateService)
    {
        this.http = http;
        this.js = js;
        this.stateService = stateService;
    }

    protected override void OnInitialized()
    {
        if (RendererInfo.IsInteractive)
        {
            timer = new Timer(TimerCallback, null, 0, RenderDetailsRefreshInterval);
        }

        objRef = DotNetObjectReference.Create(this);
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

            await AfterSceneLoad.InvokeAsync();
        }

        if (renderer is not null)
        {
            Renderer.HideTransformControls();
        }

        try
        {
            await TryLoadMapAsync(cts.Token);

            if (GameVersion == GameVersion.TM2020)
            {
                return;
            }

            await TryLoadBlockAsync(cts.Token);
            await TryLoadVehicleAsync(cts.Token);
            await TryLoadDecorationAsync(cts.Token);
            await TryLoadMeshAsync(cts.Token);
            await TryLoadItemAsync(cts.Token);
            await TryLoadSkinAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions
        }
        catch (ObjectDisposedException)
        {
            // Ignore disposed exceptions, which can happen if the component is disposed while waiting for async operations
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP request exceptions, e.g., show a message to the user
        }
    }

    private async Task LoadSceneAsync(CancellationToken cancellationToken)
    {
        rendererModule = await JSHost.ImportAsync(nameof(Renderer), "../js/renderer.js", cancellationToken);
        sceneModule = await JSHost.ImportAsync(nameof(Scene), "../js/scene.js", cancellationToken);
        cameraModule = await JSHost.ImportAsync(nameof(Camera), "../js/camera.js", cancellationToken);
        solidModule = await JSHost.ImportAsync(nameof(Solid), "../js/solid.js", cancellationToken);
        materialModule = await JSHost.ImportAsync(nameof(Material), "../js/material.js", cancellationToken);
        animationModule = await JSHost.ImportAsync(nameof(Animation), "../js/animation.js", cancellationToken);

        rendererModuleInterop = await js.InvokeAsync<IJSObjectReference>("import", $"./js/renderer.js");
        await rendererModuleInterop.InvokeVoidAsync("passDotNet", objRef);

        renderer = Renderer.Create();
        Scene = new Scene(IsCatalog);
        mapCamera = new Camera();

        Renderer.Scene = Scene;
        Renderer.Camera = mapCamera;
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

        var collectionsTask = default(Task<HttpResponseMessage>);
        var blockInfosTask = default(Task<HttpResponseMessage>);
        var decorationTask = default(Task<HttpResponseMessage>);

        if (collection is not null)
        {
            collectionsTask = collectionInfos.Count == 0 ? http.GetAsync($"/api/collections/{GameVersion}", cts.Token) : null;
            blockInfosTask = loadBlockInfos && blockInfos.Count == 0 ? http.GetAsync($"/api/blocks/{GameVersion}/{collection}", cts.Token) : null;
            decorationTask = loadDecorations && decorations.Count == 0 ? http.GetAsync($"/api/decorations/{GameVersion}/{Map?.Decoration.Collection ?? collection}", cts.Token) : null;
        }

        var materialTask = loadMaterials && Materials.Count == 0 ? http.GetAsync($"/api/materials/{GameVersion}", cts.Token) : null;
        var vehicleTask = loadVehicles && vehicles.Count == 0 ? http.GetAsync($"/api/vehicles/{GameVersion}", cts.Token) : null;

        if (collectionsTask is not null) tasks.Add(collectionsTask);
        if (blockInfosTask is not null) tasks.Add(blockInfosTask);
        if (decorationTask is not null) tasks.Add(decorationTask);
        if (materialTask is not null) tasks.Add(materialTask);
        if (vehicleTask is not null) tasks.Add(vehicleTask);

        if (tasks.Count == 0)
        {
            return false;
        }

        await foreach (var task in Task.WhenEach(tasks).WithCancellation(cancellationToken))
        {
            task.Result.EnsureSuccessStatusCode(); // show note message that user has to wait, if the block list isnt available yet

            if (task == collectionsTask)
            {
                collectionInfos = (await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListCollectionDto, cancellationToken))?
                    .ToDictionary(x => x.Name) ?? [];
            }
            else if (task == blockInfosTask)
            {
                blockInfos = (await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListBlockInfoDto, cancellationToken))?
                    .ToDictionary(x => x.Name) ?? [];
            }
            else if (task == decorationTask)
            {
                decorations = ((await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListDecorationSizeDto, cancellationToken)) ?? [])
                    .ToLookup(x => x.Size);
            }
            else if (task == materialTask)
            {
                Materials = await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.DictionaryStringMaterialDto, cancellationToken) ?? [];
            }
            else if (task == vehicleTask)
            {
                vehicles = (await task.Result.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListVehicleDto, cancellationToken))?
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

        var collectionInfo = CollectionName is null ? null : collectionInfos.GetValueOrDefault(CollectionName);

        var isGround = blockInfo.AirVariants.Count == 0;
        var units = isGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

        var blockSize = collectionInfo?.GetSquareSize() ?? (32, 8, 32);
        var size = units.Length == 0
            ? new Int3(1, 1, 1)
            : new Int3(units.Select(x => x.Offset.X).Max() + 1, units.Select(x => x.Offset.Y).Max() + 1, units.Select(x => x.Offset.Z).Max() + 1);
        var realSize = size * blockSize;

        // camera position after knowing block details
        center = new Vec3(realSize.X / 2f, realSize.Y / 2f, realSize.Z / 2f);
        position = center * (-4, 6, 1);

        mapCamera.Position = position;
        mapCamera.CreateOrbitControls(renderer, center);
        //

        var variant = isGround
            ? blockInfo.GroundVariants.FirstOrDefault()?.Variant ?? 0
            : blockInfo.AirVariants.FirstOrDefault()?.Variant ?? 0;

        var meshFound = await ChangeBlockVariantAsync(isGround, variant, subVariant: 0, cancellationToken);

        if (!meshFound)
        {
            return false;
        }

        CurrentBlockInfo = blockInfo;

        //Renderer.EnableRaycaster();

        return true;
    }

    public async Task<bool> ChangeBlockVariantAsync(bool isGround, int variant, int subVariant, CancellationToken cancellationToken = default)
    {
        if (BlockName is null)
        {
            throw new InvalidOperationException("Not in block view mode, cannot change variant.");
        }

        if (renderer is null)
        {
            throw new InvalidOperationException("Renderer is not initialized.");
        }

        if (mapCamera is null)
        {
            throw new InvalidOperationException("Map camera is not initialized.");
        }

        ClearFocusedSolids();
        await OnFocusedSolidsChange.InvokeAsync();

        var hash = $"GbxTools3D|Solid|{GameVersion}|{CollectionName}|{BlockName}|{isGround}MyGuy|{variant}|{subVariant}|PleaseDontAbuseThisThankYou:*".Hash();

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return false;
        }

        using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        var focusedSolid = await Solid.ParseAsync(stream, GameVersion, Materials, expectedMeshCount: null, optimized: false);
        Scene?.Add(focusedSolid);
        FocusedSolids = [focusedSolid];
        await OnFocusedSolidsChange.InvokeAsync();

        return true;
    }

    private async Task<bool> TryLoadVehicleAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null || VehicleName is null)
        {
            return false;
        }

        ClearFocusedSolids();
        await OnFocusedSolidsChange.InvokeAsync();

        mapCamera.Position = new Vec3(0, 5, 10);
        mapCamera.CreateOrbitControls(renderer, default);

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

        var materials = Materials;
        if (SkinZip is not null)
        {
            materials = new Dictionary<string, MaterialDto>(Materials);

            string[] prefixes = ["Bay", "Coast", "Rally", "Snow", "Speed", "Sport", "Stadium"];

            var skinMaterial = await CreateSkinMaterialAsync(SkinZip, "Diffuse.dds", ":DDSFlipY", cancellationToken);
            if (skinMaterial is not null)
            {
                foreach (var prefix in prefixes)
                {
                    materials[$"Vehicles\\Media\\Material\\{prefix}CarSkin"] = skinMaterial;
                }
            }
            var detailsMaterial = await CreateSkinMaterialAsync(SkinZip, "Details.dds", ":DDSFlipY", cancellationToken);
            if (detailsMaterial is not null)
            {
                foreach (var prefix in prefixes)
                {
                    materials[$"Vehicles\\Media\\Material\\{prefix}CarSkinDetails"] = detailsMaterial;
                }
            }
            var projShadMaterial = await CreateSkinMaterialAsync(SkinZip, "ProjShad.dds", ":FakeShad", cancellationToken);
            if (projShadMaterial is not null) materials[""] = projShadMaterial; // shouldnt be empty? or wtf?
        }

        var focusedSolid = await Solid.ParseAsync(stream, GameVersion, materials, expectedMeshCount: null, optimized: false);
        Scene?.Add(focusedSolid);
        FocusedSolids = [focusedSolid];
        await OnFocusedSolidsChange.InvokeAsync();

        //Renderer.EnableRaycaster();

        return true;
    }

    private async Task<bool> TryLoadDecorationAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null || DecorationName is null || CollectionName is null)
        {
            return false;
        }

        ClearFocusedSolids();
        await OnFocusedSolidsChange.InvokeAsync();

        mapCamera.Position = new Vec3(0, 5, 10);
        mapCamera.CreateMapControls(renderer, default);

        try
        {
            await TryFetchDataAsync(loadMaterials: true, loadDecorations: true, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return false;
        }

        var decoSizeArray = DecorationName.Split('x');

        if (decoSizeArray.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(decoSizeArray[0], out var decoSizeX)
            || !int.TryParse(decoSizeArray[1], out var decoSizeY)
            || !int.TryParse(decoSizeArray[2], out var decoSizeZ))
        {
            return false;
        }

        var decoSize = new Int3(decoSizeX, decoSizeY, decoSizeZ);

        if (!decorations.Contains(decoSize))
        {
            return false;
        }

        var decoInfos = decorations[decoSize];
        var decoInfo = decoInfos.Count() == 1
            ? decoInfos.First()
            : decoInfos.First(x => x.SceneName.Substring(x.SceneName.LastIndexOf('\\') + 1) == SceneName); // where SceneName matches

        var blockSize = collectionInfos[CollectionName].GetSquareSize();
        var center = new Vec3(decoSize.X * blockSize.X / 2f, /*baseHeight * blockSize.Y*/0, decoSize.Z * blockSize.Z / 2f - decoSize.Z * blockSize.Z * 0.15f);

        // setup camera
        mapCamera.Position = new Vec3(center.X, decoSize.Z * blockSize.Z, -decoSize.Z * blockSize.Z);
        mapCamera.CreateMapControls(renderer, center);

        await foreach (var solid in CreateDecorationAsync(CollectionName, decoInfo, optimized: false, cancellationToken))
        {
            FocusedSolids.Add(solid);
        }
        await OnFocusedSolidsChange.InvokeAsync();

        //Renderer.EnableRaycaster();

        return true;
    }

    private async Task<bool> TryLoadMeshAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null)
        {
            return false;
        }

        if (Solid1 is null && Solid2 is null && Prefab is null)
        {
            return false; // no mesh to load
        }

        ClearFocusedSolids();
        await OnFocusedSolidsChange.InvokeAsync();

        // initial camera position
        var center = new Vec3(16, 4, 16);
        var position = center * (4, 6, 1);

        mapCamera.Position = position;
        mapCamera.CreateMapControls(renderer, center);
        //

        if (GameVersion == (GameVersion.TMT | GameVersion.MP4))
        {
            GameVersion = GameVersion.MP4;
        }

        try
        {
            await TryFetchDataAsync(loadMaterials: true, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return false;
        }

        Solid focusedSolid;
        if (Solid1 is not null)
        {
            focusedSolid = await Solid.CreateFromSolidAsync(Solid1, GameVersion, Materials);
        }
        else if (Solid2 is not null)
        {
            focusedSolid = await Solid.CreateFromSolid2Async(Solid2, GameVersion, Materials);
        }
        else if (Prefab is not null)
        {
            focusedSolid = await Solid.CreateFromPrefabAsync(Prefab, GameVersion, Materials);
        }
        else
        {
            throw new InvalidOperationException("No solid or prefab provided for mesh view.");
        }
        Scene?.Add(focusedSolid);
        FocusedSolids = [focusedSolid];
        await OnFocusedSolidsChange.InvokeAsync();

        //Renderer.EnableRaycaster();

        return true;
    }

    private async Task<bool> TryLoadItemAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null || Item is null)
        {
            return false;
        }

        ClearFocusedSolids();
        await OnFocusedSolidsChange.InvokeAsync();

        // initial camera position
        var center = new Vec3(16, 4, 16);
        var position = center * (4, 6, 1);

        mapCamera.Position = position;
        mapCamera.CreateMapControls(renderer, center);
        //

        try
        {
            await TryFetchDataAsync(loadMaterials: true, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return false;
        }

        Solid focusedSolid;
        if (Item.EntityModelEdition is CGameCommonItemEntityModelEdition { MeshCrystal: not null } modelEdition)
        {
            focusedSolid = await Solid.CreateFromCrystalAsync(modelEdition.MeshCrystal, GameVersion, Materials);
        }
        else if (Item.EntityModel is CGameCommonItemEntityModel { StaticObject.Mesh: not null } model)
        {
            focusedSolid = await Solid.CreateFromSolid2Async(model.StaticObject.Mesh, GameVersion, Materials);
        }
        else if (Item.EntityModel is CPlugPrefab prefab)
        {
            focusedSolid = await Solid.CreateFromPrefabAsync(prefab, GameVersion, Materials);
        }
        else
        {
            throw new InvalidOperationException("Item does not have a valid mesh to display.");
        }
        Scene?.Add(focusedSolid);
        FocusedSolids = [focusedSolid];
        await OnFocusedSolidsChange.InvokeAsync();

        //Renderer.EnableRaycaster();

        return true;
    }

    private async Task<bool> TryLoadSkinAsync(CancellationToken cancellationToken = default)
    {
        if (mapCamera is null || renderer is null || SkinZip is null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(VehicleName))
        {
            return false;
        }

        ClearFocusedSolids();
        await OnFocusedSolidsChange.InvokeAsync();

        mapCamera.Position = new Vec3(0, 5, 10);
        mapCamera.CreateOrbitControls(renderer, default);

        try
        {
            await TryFetchDataAsync(loadMaterials: true, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return false;
        }

        var mainBody = SkinZip.Entries
                .FirstOrDefault(x => string.Equals(x.Name, "MainBodyHigh.Solid.Gbx", StringComparison.OrdinalIgnoreCase))
            ?? SkinZip.Entries
                .FirstOrDefault(x => string.Equals(x.Name, "MainBody.Solid.Gbx", StringComparison.OrdinalIgnoreCase));

        var materials = new Dictionary<string, MaterialDto>(Materials);

        var skinMaterial = await CreateSkinMaterialAsync(SkinZip, "Diffuse.dds", ":DDSFlipY", cancellationToken);
        if (skinMaterial is not null) materials["s"] = skinMaterial;
        var detailsMaterial = await CreateSkinMaterialAsync(SkinZip, "Details.dds", ":DDSFlipY", cancellationToken);
        if (detailsMaterial is not null) materials["d"] = detailsMaterial;
        var projShadMaterial = await CreateSkinMaterialAsync(SkinZip, "ProjShad.dds", ":FakeShad", cancellationToken);
        if (projShadMaterial is not null) materials[""] = projShadMaterial; // shouldnt be empty? or wtf?

        Solid focusedSolid;
        if (mainBody is null)
        {
            return false; // load official vehicle
        }
        else
        {
            await using var stream = mainBody.Open();
            await using var ms = new MemoryStream((int)mainBody.Length);
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            var solid = await Gbx.ParseNodeAsync<CPlugSolid>(ms, cancellationToken: cancellationToken);
            focusedSolid = await Solid.CreateFromSolidAsync(solid, GameVersion, materials);
        }

        Scene?.Add(focusedSolid);
        FocusedSolids = [focusedSolid];
        await OnFocusedSolidsChange.InvokeAsync();

        //Renderer.EnableRaycaster();

        return true;
    }

    private static async Task<MaterialDto?> CreateSkinMaterialAsync(ZipArchive zip, string diffuseFileName, string shaderName, CancellationToken cancellationToken)
    {
        var texture = zip.Entries
            .FirstOrDefault(x => string.Equals(x.Name, diffuseFileName, StringComparison.OrdinalIgnoreCase));

        if (texture is null)
        {
            return null;
        }

        await using var stream = texture.Open();
        await using var ms = new MemoryStream((int)texture.Length);
        await stream.CopyToAsync(ms, cancellationToken);

        return new MaterialDto
        {
            Textures = new Dictionary<string, string>
            {
                { "Diffuse", $"data:image/vnd.ms-dds;base64,{Convert.ToBase64String(ms.ToArray())}" }
            }.ToImmutableDictionary(),
            Shader = shaderName
        };
    }

    private void ClearFocusedSolids()
    {
        foreach (var solid in FocusedSolids)
        {
            solid.Remove(Scene);
            //solid.Dispose();
        }

        FocusedSolids.Clear();
    }

    public async Task<bool> TryLoadMapAsync(CancellationToken cancellationToken = default)
    {
        if (Map is null || mapCamera is null || renderer is null || mapLoadAttempted)
        {
            return false;
        }

        mapLoadAttempted = true; // because map load doesnt have good cleanup process, this hack will prevent multiple map loads

        GameVersion = GameVersionSupport.GetSupportedGameVersion(Map);

        if (GameVersion == GameVersion.TM2020)
        {
            return false;
        }

        await BeforeMapLoad.InvokeAsync();

        await TryFetchDataAsync(loadBlockInfos: true, loadDecorations: true, loadMaterials: true, cancellationToken: cancellationToken);

        var collectionInfo = Map.Collection is null ? null : collectionInfos.GetValueOrDefault(Map.Collection);

        var blockSize = collectionInfo?.GetSquareSize() ?? Map.Collection.GetValueOrDefault().GetBlockSize();
        var center = new Vec3(Map.Size.X * blockSize.X / 2f, /*baseHeight * blockSize.Y*/0, Map.Size.Z * blockSize.Z / 2f - Map.Size.Z * blockSize.Z * 0.15f);

        if (vehicleCamera is null)
        {
            // setup camera
            mapCamera.Position = new Vec3(center.X, Map.Size.Z * 0.5f * blockSize.Z, 0);
            mapCamera.CreateMapControls(renderer, center);
        }

        var baseHeight = 5;
        var decoSize = default(DecorationSizeDto);

        if (decorations.Contains(Map.Size))
        {
            decoSize = decorations[Map.Size].First(x => x.Decorations.Any(x => x.Name == Map.Decoration.Id));
            baseHeight = decoSize.BaseHeight + (decoSize.OffsetBlockY ? 1 : 0);
        }

        await PlaceBlocksAsync(Map, baseHeight, blockSize, cancellationToken);
        await PlacePylonsAsync(Map, baseHeight, blockSize, cancellationToken);

        if (decoSize is not null)
        {
            var deco = decoSize.Decorations.FirstOrDefault(x => x.Name == Map.Decoration.Id);
            // TODO with deco

            await foreach (var _ in CreateDecorationAsync(Map.Decoration.Collection, decoSize, optimized: true, cancellationToken)) {}
        }

        return true;
    }

    [JSInvokable]
    public void Intersects(int objectId, string materialName, JsonDocument? materialUserData)
    {
        var obj = Scene?.GetObjectById(objectId);

        if (obj is not null)
        {
            OnIntersect?.Invoke(new(obj, materialName, materialUserData));
        }
    }

    private async IAsyncEnumerable<Solid> CreateDecorationAsync(string collectionName, DecorationSizeDto decoSize, bool optimized, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = new Dictionary<Task<HttpResponseMessage>, Iso4>();

        var sceneObjects = decoSize.Scene.Where(x => x.Solid is not null).ToList();

        foreach (var sceneObject in sceneObjects)
        {
            if (Path.GetFileNameWithoutExtension(sceneObject.Solid)?.Contains("FarClip") == true)
            {
                continue;
            }

            if (optimized)
            {
                var normalizedPath = sceneObject.Solid?.Replace('\\', '/');

                // currently slow af to process
                if (normalizedPath == "Stadium/Media/Solid/Other/StadiumWarpFlags")
                {
                    continue;
                }

                // has weird texturing
                /*if (normalizedPath == "Island/Media/Solid/Other/IslandSkyDome")
                {

                }*/
            }

            var hash = $"GbxTools3D|Decoration|{GameVersion}|{collectionName}|{sceneObject.Solid}|Je te hais".Hash();

            tasks.Add(http.GetAsync($"/api/mesh/{hash}", cancellationToken), sceneObject.Location);
        }
        stateService.NotifyTasksDefined(new LoadingStageDto(LoadingStage.Decos, tasks.Count));

        await foreach (var meshResponseTask in Task.WhenEach(tasks.Keys).WithCancellation(cancellationToken))
        {
            using var meshResponse = await meshResponseTask;

            if (!meshResponse.IsSuccessStatusCode)
            {
                stateService.NotifyTasksChanged(new LoadingStageDto(LoadingStage.Decos, 1));
                continue;
            }

            await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);
            var solid = await Solid.ParseAsync(stream, GameVersion, Materials, expectedMeshCount: null, optimized: optimized, receiveShadow: false, castShadow: false);
            solid.Location = tasks[meshResponseTask];
            Scene?.Add(solid);
            stateService.NotifyTasksChanged(new LoadingStageDto(LoadingStage.Decos, 1));
            yield return solid;
        }
    }

    private async Task PlaceBlocksAsync(CGameCtnChallenge map, int baseHeight, Int3 blockSize, CancellationToken cancellationToken)
    {
        var collection = CollectionName ?? Map?.Collection;

        var yOffset = GameVersion >= GameVersion.TMT ? map.DecoBaseHeightOffset + baseHeight : 0;

        var coveredZoneBlocks = GetCoveredZoneBlocks().ToImmutableHashSet();
        var terrainModifiers = GetTerrainModifiers();

        var baseZoneBlock = blockInfos.Values.FirstOrDefault(x => x.IsDefaultZone);
        var baseZoneBlocks = GameVersion >= GameVersion.MP3 ? [] : CreateBaseZoneBlocks(baseZoneBlock, baseHeight);
        var clipBlocks = CreateClipBlocks();

        var uniqueBlockVariants = baseZoneBlocks
            .Concat(map.GetBlocks())
            .Concat(map.GetBakedBlocks())
            .Where(x => !x.IsClip && !coveredZoneBlocks.Contains(x))
            .Concat(clipBlocks)
            .ToLookup(x => new UniqueVariant(
            x.Name,
            x.IsGround,
            x.Variant,
            x.Name.EndsWith("Pillar") ? 0 : x.SubVariant, // because TMF sometimes has billion subvariants for pillars and it kills performance

            // terrain modifier with check that ensures the block is not modified by itself
            // this is not exact, it should be checked against real block units and not just 0x0x0!!
            terrainModifiers.GetValueOrDefault(x.Coord with { Y = 0 }) is TerrainModifierInfo info && info.ModifiedBy != x ? info.TerrainModifier : null));

        stateService.NotifyTasksDefined(new LoadingStageDto(LoadingStage.Blocks, uniqueBlockVariants.Count));

        var responseTasks = new Dictionary<UniqueVariant, Task<HttpResponseMessage>>();

        var counter = 0;
        foreach (var uniqueGroup in uniqueBlockVariants)
        {
            var (name, isGround, variant, subVariant, terrainModifier) = uniqueGroup.Key;

            if (!blockInfos.TryGetValue(name, out var blockInfo))
            {
                Console.WriteLine($"Block info for {name} not found.");
                stateService.NotifyTasksChanged(new LoadingStageDto(LoadingStage.Blocks, 1));
                continue;
            }

            var variants = isGround ? blockInfo.GroundVariants : blockInfo.AirVariants;

            if (!variants.Any(x => x.Variant == variant && x.SubVariant == subVariant))
            {
                Console.WriteLine($"Block variant {name} {(isGround ? "Ground" : "Air")}{variant}/{subVariant} not found in block info.");
                stateService.NotifyTasksChanged(new LoadingStageDto(LoadingStage.Blocks, 1));
                continue;
            }

            var hash = $"GbxTools3D|Solid|{GameVersion}|{collection}|{name}|{isGround}MyGuy|{variant}|{subVariant}|PleaseDontAbuseThisThankYou:*".Hash();

            responseTasks.Add(uniqueGroup.Key, http.GetAsync($"/api/mesh/{hash}", cancellationToken));

            if (counter > 20)
            {
                await Task.Delay(20, cancellationToken);
                counter = 0;
            }

            await ProcessBlockResponsesAsync(responseTasks, maxRequestsToProcess: 10, uniqueBlockVariants, yOffset, blockSize, cancellationToken);

            counter++;
        }

        while (responseTasks.Count > 0)
        {
            await Task.Delay(20, cancellationToken);
            await ProcessBlockResponsesAsync(responseTasks, maxRequestsToProcess: null, uniqueBlockVariants, yOffset, blockSize, cancellationToken);
        }
    }

    internal async Task<Solid?> LoadGhostAsync(CGameCtnGhost ghost, CancellationToken cancellationToken = default)
    {
        if (GameVersion == GameVersion.Unspecified)
        {
            GameVersion = GameVersionSupport.GetSupportedGameVersion(ghost);
        }

        await TryFetchDataAsync(loadMaterials: true, loadVehicles: true, cancellationToken: cancellationToken);

        var vehicleName = ghost.PlayerModel?.Id;

        if (vehicleName is null || !vehicles.TryGetValue(vehicleName, out var vehicleInfo))
        {
            return null;
        }

        var tempGameVersion = GameVersion;
        if (GameVersion == GameVersion.TMNESWC)
        {
            tempGameVersion = GameVersion.TMF; // temporary
        }

        var hash = $"GbxTools3D|Vehicle|{tempGameVersion}|{vehicleName}|WhyDidYouNotHelpMe?".Hash();

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        var vehicle = await Solid.ParseAsync(stream, GameVersion, Materials, expectedMeshCount: null, optimized: false, castShadow: false, noLights: true);
        Scene?.Add(vehicle);

        vehicleCamera = new Camera(vehicleInfo.CameraFov);
        Camera.Follow(vehicle.Object, vehicleInfo.CameraFar, vehicleInfo.CameraUp, vehicleInfo.CameraLookAtFactor);
        Renderer.Camera = vehicleCamera;

        return vehicle;
    }

    internal async Task<Solid?> CreateVehicleCollisionsAsync(string vehicleName, CancellationToken cancellationToken = default)
    {
        if (vehicleName is null || !vehicles.TryGetValue(vehicleName, out var vehicleInfo))
        {
            return null;
        }

        var hash = $"GbxTools3D|Vehicle|{GameVersion}|{vehicleName}|WhyDidYouNotHelpMe?".Hash();

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}?collision=true", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        var vehicle = await Solid.ParseAsync(stream, GameVersion, Materials, expectedMeshCount: null, optimized: false, castShadow: false, noLights: true);
        Scene?.Add(vehicle);

        return vehicle;
    }

    internal async Task ToggleCollisionsAsync(string hash, Solid solid, CancellationToken cancellationToken = default)
    {
        if (Scene is null)
        {
            return;
        }

        if (solid.CollisionsEnabled)
        {
            solid.ToggleCollision(Scene, collision: null);
            return;
        }

        using var meshResponse = await http.GetAsync($"/api/mesh/{hash}?collision=true", cancellationToken);

        if (!meshResponse.IsSuccessStatusCode)
        {
            return;
        }

        await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

        var collisionSolid = await Solid.ParseAsync(stream, GameVersion, Materials, optimized: false);
        solid.ToggleCollision(Scene, collisionSolid);
    }

    internal async Task ToggleBlockCollisionsAsync(bool isGround, int variant, int subVariant, Solid solid, CancellationToken cancellationToken = default)
    {
        await ToggleCollisionsAsync($"GbxTools3D|Solid|{GameVersion}|{CollectionName}|{BlockName}|{isGround}MyGuy|{variant}|{subVariant}|PleaseDontAbuseThisThankYou:*".Hash(), solid, cancellationToken);
    }

    internal async Task ToggleVehicleCollisionsAsync(Solid solid, CancellationToken cancellationToken = default)
    {
        await ToggleCollisionsAsync($"GbxTools3D|Vehicle|{GameVersion}|{VehicleName}|WhyDidYouNotHelpMe?".Hash(), solid, cancellationToken);
    }

    internal async Task ToggleDecorationCollisionsAsync(Solid solid, CancellationToken cancellationToken = default)
    {
        await ToggleCollisionsAsync($"GbxTools3D|Decoration|{GameVersion}|{CollectionName}|{GbxPath.GetFileNameWithoutExtension(solid.FilePath)}|Je te hais".Hash(), solid, cancellationToken);
    }

    internal async Task ToggleBlockObjectLinksAsync(
        bool isGround,
        int variant,
        int subVariant,
        int objectLinkCount,
        bool hasWaypoint,
        Solid solid,
        CancellationToken cancellationToken = default)
    {
        if (Scene is null)
        {
            return;
        }

        if (solid.ObjectLinksEnabled)
        {
            solid.ToggleObjectLinks(Scene, objectLinks: []);
            return;
        }

        Solid[] objectLinks;

        if (hasWaypoint)
        {
            var hash = $"GbxTools3D|Solid|{GameVersion}|{CollectionName}|{BlockName}|{isGround}|Way to go bois".Hash();

            using var meshTriggerResponse = await http.GetAsync($"/api/mesh/{hash}?collision=true", cancellationToken);

            if (!meshTriggerResponse.IsSuccessStatusCode)
            {
                return;
            }

            await using var stream = await meshTriggerResponse.Content.ReadAsStreamAsync(cancellationToken);

            var collisionSolid = await Solid.ParseAsync(stream, GameVersion, Materials, optimized: false, isTrigger: true);

            objectLinks = [collisionSolid];
        }
        else
        {
            objectLinks = new Solid[objectLinkCount];

            for (var i = 0; i < objectLinkCount; i++)
            {
                var hash = $"GbxTools3D|Solid|{GameVersion}|{CollectionName}|{BlockName}|Hella{isGround}|{variant}|{subVariant}|{i}|marosisPakPakGhidraGang".Hash();

                using var meshTriggerResponse = await http.GetAsync($"/api/mesh/{hash}?collision=true", cancellationToken);

                if (!meshTriggerResponse.IsSuccessStatusCode)
                {
                    return;
                }

                await using var stream = await meshTriggerResponse.Content.ReadAsStreamAsync(cancellationToken);

                var collisionSolid = await Solid.ParseAsync(stream, GameVersion, Materials, optimized: false, isTrigger: true);

                objectLinks[i] = collisionSolid;
            }
        }

        solid.ToggleObjectLinks(Scene, objectLinks);
    }

    private async Task ProcessBlockResponsesAsync(
        Dictionary<UniqueVariant, Task<HttpResponseMessage>> responseTasks,
        int? maxRequestsToProcess,
        ILookup<UniqueVariant, CGameCtnBlock> uniqueBlockVariantLookup,
        int yOffset,
        Int3 blockSize,
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
                var solid = await Solid.ParseAsync(stream, GameVersion, Materials, variant.TerrainModifier, expectedCount);

                PlaceBlocks(solid, variant, uniqueBlockVariantLookup[variant], blockSize, yOffset);

                stateService.NotifyTasksChanged(new LoadingStageDto(LoadingStage.Blocks, 1));
            }
            else
            {
                Console.WriteLine($"Failed to load block variant {variant.Name} (Ground: {variant.IsGround}, Variant: {variant.Variant}, SubVariant: {variant.SubVariant}). Status code: {response.StatusCode}");
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

    private void PlaceBlocks(Solid solid, UniqueVariant variant, IEnumerable<CGameCtnBlock> blocks, Int3 blockSize, int yOffset)
    {
        if (Scene is null)
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

        if (GameVersion >= GameVersion.MP3)
        {
            height = 0;
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
                _ => (0, 0, 0) // possible top/bottom in baked blocks?
            };

            var instanceInfo = Solid.GetInstanceInfo((actualCoord - (0, height + yOffset, 0)) * blockSize, block.Direction);

            instanceInfos.Add(instanceInfo);
        }

        solid.Instantiate(instanceInfos.ToArray());
        Scene.Add(solid);
    }

    private IEnumerable<CGameCtnBlock> GetCoveredZoneBlocks()
    {
        if (Map is null || blockInfos is null)
        {
            yield break;
        }

        var isManiaPlanet = GameVersion >= GameVersion.MP3;
        var groundPositions = new List<Int3>();

        foreach (var block in Map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height.HasValue)
            {
                continue;
            }

            PopulateGroundPositionsFromBlock(groundPositions, block, blockInfo);
        }

        foreach (var block in Map.GetBlocks().Concat(Map.GetBakedBlocks()))
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
            var units = (block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits).AsSpan();

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
            var units = (block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits).AsSpan();

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
            .DistinctBy(x => x.Coord)
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

        // ground fillers
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

        RotateUnits(units.AsSpan(), block.Direction, rotatedUnits, out var minX, out var minZ);

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

                if (!clipBlockDict.TryGetValue(finalCoord, out var clipBlock))
                {
                    continue;
                }

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

    private async Task PlacePylonsAsync(CGameCtnChallenge map, int baseHeight, Int3 blockSize, CancellationToken cancellationToken)
    {
        var collection = CollectionName ?? Map?.Collection;

        var pylonDict = CreatePylons(map, blockSize, baseHeight);

        var pylonInfos = pylonDict.Values.Distinct().ToList();

        var pylonMeshResponseTasks = new Dictionary<Task<HttpResponseMessage>, PylonInfo>();

        stateService.NotifyTasksDefined(new LoadingStageDto(LoadingStage.Pylons, pylonInfos.Count));

        foreach (var pylonInfo in pylonInfos)
        {
            var hash = $"GbxTools3D|Solid|{GameVersion}|{collection}|{pylonInfo.Name}|TrueMyGuy|{pylonInfo.Height - 1}|0|PleaseDontAbuseThisThankYou:*".Hash();
            pylonMeshResponseTasks[http.GetAsync($"/api/mesh/{hash}", cancellationToken)] = pylonInfo;
        }

        await foreach (var meshResponseTask in Task.WhenEach(pylonMeshResponseTasks.Keys).WithCancellation(cancellationToken))
        {
            using var meshResponse = await meshResponseTask;

            if (!meshResponse.IsSuccessStatusCode)
            {
                continue;
            }

            var pylonInfo = pylonMeshResponseTasks[meshResponseTask];
            var pylonKeys = pylonDict
                .Where(x => x.Value == pylonInfo)
                .Select(x => x.Key)
                .ToList();

            await using var stream = await meshResponse.Content.ReadAsStreamAsync(cancellationToken);

            var solid = await Solid.ParseAsync(stream, GameVersion, Materials, expectedMeshCount: pylonKeys.Count);

            var instanceInfos = new JSObject[pylonKeys.Count];

            for (var i = 0; i < pylonKeys.Count; i++)
            {
                var (pylonPos, dir) = pylonKeys[i];
                instanceInfos[i] = Solid.GetInstanceInfo(pylonPos, dir);
            }

            solid.Instantiate(instanceInfos);
            Scene?.Add(solid);
            stateService.NotifyTasksChanged(new LoadingStageDto(LoadingStage.Pylons, 1));
        }
    }

    private sealed record PylonInfo(int Height, string Name);

    private Dictionary<(Int3, Direction), PylonInfo> CreatePylons(CGameCtnChallenge map, Int3 blockSize, int baseHeight)
    {
        if (blockInfos is null)
        {
            return [];
        }

        // tells which pylon mesh to place and at which height it starts
        // if the zone isn't gonna be available in the dictionary, it is considered to be base zone with baseHeight
        var zonePylonDict = new Dictionary<Int3, PylonInfo?>();

        foreach (var block in map.GetBlocks())
        {
            if (blockInfos.TryGetValue(block.Name, out var blockInfo) && blockInfo.Height.HasValue)
            {
                zonePylonDict[block.Coord with { Y = 0 }] = blockInfo.PylonName is null ? null : new(block.Coord.Y, blockInfo.PylonName);
            }
        }

        var avoidPylonSet = new HashSet<Int3>();

        foreach (var block in map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height.HasValue)
            {
                continue;
            }

            var units = block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

            if (units.All(x => x.AcceptPylons is null or 255))
            {
                continue;
            }

            PopulateAvoidPylonSet(avoidPylonSet, block, units.AsSpan());
        }

        var baseZoneBlock = blockInfos.Values.FirstOrDefault(x => x.IsDefaultZone);
        var pylonDict = new Dictionary<(Int3, Direction), PylonInfo>();

        foreach (var block in map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo) || blockInfo.Height.HasValue)
            {
                continue;
            }

            var units = block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

            if (units.All(x => x.PlacePylons is null or 0))
            {
                continue;
            }

            if (blockInfo.IsRoad && units.Length > 0 && units[0].PlacePylons == 1)
            {
                byte pylons = block.Variant switch
                {
                    0 => 255,
                    1 => 3,
                    2 => 15,
                    3 => 51,
                    4 => 63,
                    5 => 255,
                    _ => throw new Exception("Invalid pylon variant")
                };

                units = [new BlockUnit { PlacePylons = pylons, AcceptPylons = 255 }];
            }

            PopulatePylonsFromBlock(blockSize, zonePylonDict, baseZoneBlock, pylonDict, block, units.AsSpan(), baseHeight, avoidPylonSet);
        }

        return pylonDict;

        static void PopulateAvoidPylonSet(HashSet<Int3> avoidPylonSet, CGameCtnBlock block, ReadOnlySpan<BlockUnit> units)
        {
            Span<Int3> rotatedUnits = stackalloc Int3[units.Length];

            RotateUnits(units, block.Direction, rotatedUnits, out var minX, out var minZ);

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                var acceptPylons = unit.AcceptPylons ?? 255;

                if (acceptPylons == 255)
                {
                    continue;
                }

                var rotatedUnit = rotatedUnits[i];
                var unitCoord = block.Coord + new Int3(rotatedUnit.X - minX, rotatedUnit.Y, rotatedUnit.Z - minZ);
                avoidPylonSet.Add(unitCoord with { Y = 0 });
            }
        }

        static void PopulatePylonsFromBlock(
            Int3 blockSize,
            Dictionary<Int3, PylonInfo?> zonePylonDict,
            BlockInfoDto? baseZoneBlock,
            Dictionary<(Int3, Direction), PylonInfo> pylonDict,
            CGameCtnBlock block,
            ReadOnlySpan<BlockUnit> units,
            int baseHeight,
            HashSet<Int3> avoidPylonSet)
        {
            Span<Int3> rotatedUnits = stackalloc Int3[units.Length];

            RotateUnits(units, block.Direction, rotatedUnits, out var minX, out var minZ);

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                var placePylons = unit.PlacePylons ?? 0;

                if (placePylons == 0)
                {
                    continue;
                }

                var rotatedUnit = rotatedUnits[i];
                var unitCoord = block.Coord + new Int3(rotatedUnit.X - minX, rotatedUnit.Y, rotatedUnit.Z - minZ);
                var unitCoordY0 = unitCoord with { Y = 0 };

                if (avoidPylonSet.Contains(unitCoordY0))
                {
                    continue;
                }

                if (!zonePylonDict.TryGetValue(unitCoordY0, out var zonePylonInfo) && baseZoneBlock?.PylonName is not null)
                {
                    zonePylonInfo = new PylonInfo(baseHeight, baseZoneBlock.PylonName);
                }

                if (zonePylonInfo is null)
                {
                    continue;
                }

                var shift = (int)block.Direction * 2;
                var rotatedPylons = ((placePylons << shift) & 255) | (placePylons >> (8 - shift));

                for (var pylonIndex = 0; pylonIndex < 8; pylonIndex++)
                {
                    if ((rotatedPylons >> pylonIndex & 1) == 0)
                    {
                        continue;
                    }

                    var dir = pylonIndex / 2;
                    var side = pylonIndex % 2;
                    var pylonOffset = PillarOffset - side * PillarOffset * 2;
                    var pos = (unitCoordY0 + (0, zonePylonInfo.Height + 1, 0)) * blockSize + (blockSize.X / 2, 0, blockSize.Z / 2);
                    // (zonePylonInfo.Height + 1) might cause duplicates around hills when changing heights?

                    pos += (Direction)dir switch
                    {
                        Direction.North => (pylonOffset, 0, blockSize.Z / 2),
                        Direction.East => (-blockSize.X / 2, 0, pylonOffset),
                        Direction.South => (-pylonOffset, 0, -blockSize.Z / 2),
                        Direction.West => (blockSize.X / 2, 0, -pylonOffset),
                        _ => throw new ArgumentException("Invalid block direction")
                    };

                    var pylonDir = pylonIndex switch
                    {
                        0 => Direction.South,
                        1 => Direction.North,
                        2 => Direction.West,
                        3 => Direction.East,
                        4 => Direction.North,
                        5 => Direction.South,
                        6 => Direction.East,
                        7 => Direction.West,
                        _ => throw new ArgumentException("Invalid pylon index")
                    };

                    var key = (pos, pylonDir);
                    var height = unitCoord.Y - zonePylonInfo.Height - 1;

                    if (height > 0 && (!pylonDict.TryGetValue(key, out var pylonInfo) || pylonInfo.Height < height))
                    {
                        pylonDict[key] = new PylonInfo(height, zonePylonInfo.Name);
                    }
                }
            }
        }
    }

    private sealed record TerrainModifierInfo(string TerrainModifier, CGameCtnBlock? ModifiedBy);

    private ImmutableDictionary<Int3, TerrainModifierInfo> GetTerrainModifiers()
    {
        if (Map is null || blockInfos is null)
        {
            return ImmutableDictionary<Int3, TerrainModifierInfo>.Empty;
        }

        var terrainModifiers = ImmutableDictionary.CreateBuilder<Int3, TerrainModifierInfo>();

        foreach (var block in Map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo))
            {
                continue;
            }

            var units = block.IsGround ? blockInfo.GroundUnits : blockInfo.AirUnits;

            if (!units.Any(x => x.TerrainModifier is not null))
            {
                continue;
            }

            var pivotX = units.Min(u => u.Offset.X);
            var pivotZ = units.Min(u => u.Offset.Z);

            var rotated = new (Int3 Position, string? Modifier)[units.Length];

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];

                var relX = unit.Offset.X - pivotX;
                var relZ = unit.Offset.Z - pivotZ;

                var newPos = block.Direction switch
                {
                    Direction.North => new Int3(relX, unit.Offset.Y, relZ),
                    Direction.East => new Int3(-relZ, unit.Offset.Y, relX),
                    Direction.South => new Int3(-relX, unit.Offset.Y, -relZ),
                    Direction.West => new Int3(relZ, unit.Offset.Y, -relX),
                    _ => throw new ArgumentException("Invalid direction")
                };

                // New absolute position
                var absPos = new Int3(pivotX + newPos.X, unit.Offset.Y, pivotZ + newPos.Z);
                rotated[i] = (Position: absPos, Modifier: unit.TerrainModifier);
            }

            var normMinX = rotated.Min(t => t.Position.X);
            var normMinY = rotated.Min(t => t.Position.Y);
            var normMinZ = rotated.Min(t => t.Position.Z);

            for (int i = 0; i < rotated.Length; i++)
            {
                var (pos, modifier) = rotated[i];
                if (modifier is null)
                {
                    continue; // Skip if no terrain modifier present
                }

                var normalizedPos = new Int3(pos.X - normMinX, pos.Y - normMinY, pos.Z - normMinZ);
                var unitCoord = (block.Coord + normalizedPos) with { Y = 0 };

                // also stores the block with this
                // this ensures the block is not modified by itself
                // this is not exact, it should be checked against real block units and not just 0x0x0!!
                terrainModifiers[unitCoord] = new TerrainModifierInfo(modifier, block);
            }
        }

        // ensures the dirt modifiers are applied after fabric ones
        foreach (var block in Map.GetBlocks())
        {
            if (!blockInfos.TryGetValue(block.Name, out var blockInfo))
            {
                continue;
            }

            if (blockInfo.TerrainModifier is not null)
            {
                terrainModifiers[block.Coord with { Y = 0 }] = new TerrainModifierInfo(blockInfo.TerrainModifier, null);
            }
        }

        return terrainModifiers.ToImmutable();
    }

    private static void RotateUnits(ReadOnlySpan<BlockUnit> units, Direction dir, Span<Int3> rotatedUnits, out int minX, out int minZ)
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
            OnRenderDetails?.Invoke(null);
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

        OnRenderDetails?.Invoke(new RenderDetails(fps, calls, triangles, geometries, textures));
    }

    public void ShowGrid() => Scene?.ShowGrid();
    public void HideGrid() => Scene?.HideGrid();

    public void Unfollow()
    {
        Camera.Unfollow();
        //mapCamera?.CreateMapControls(renderer, default);
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
            Camera.RemoveControls();
            Animation.DisposeMixers();
            Renderer.Dispose();
        }

        Scene = null;
        mapCamera = null;
        vehicleCamera = null;

        rendererModule?.Dispose();
        sceneModule?.Dispose();
        cameraModule?.Dispose();
        solidModule?.Dispose();
        materialModule?.Dispose();
        animationModule?.Dispose();

        if (rendererModuleInterop is not null)
        {
            try
            {
                await rendererModuleInterop.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        objRef?.Dispose();
    }

    private JSObject? lightHelper;

    internal void SetLightHelper(JSObject? child, Func<JSObject, JSObject> createHelper)
    {
        if (Scene is null)
        {
            lightHelper = null;
            return;
        }

        if (child is null)
        {
            ResetLightHelper();
            return;
        }

        lightHelper = createHelper(child);
        Scene.Add(lightHelper);
    }

    internal void ResetLightHelper()
    {
        if (lightHelper is not null)
        {
            Scene?.Remove(lightHelper);
            lightHelper = null;
        }
    }

    internal void SetOrbitCamera()
    {
        if (renderer is null || vehicleCamera is null)
        {
            return;
        }
        vehicleCamera.CreateOrbitControls(renderer, default);
    }

    internal void SetFreeCamera()
    {
        if (renderer is null || vehicleCamera is null)
        {
            return;
        }
        vehicleCamera.CreateFlyControls(renderer);
    }
}
