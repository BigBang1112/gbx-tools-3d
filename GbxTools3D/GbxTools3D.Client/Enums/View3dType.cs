using GbxTools3D.Client.Models;

namespace GbxTools3D.Client.Enums;

public enum View3dType
{
    [ViewTypeMetadata(
    name: "Replay",
    points:
    [
        "See the replay almost instantly and anywhere",
        "Checkpoints and inputs that you cannot see in MediaTracker",
        "Drifts, speed, brakes, and other vehicle parameters"
    ],
    link: "replay",
    extensions: ["Replay.Gbx"],
    loadingStages: [
        LoadingStage.Vehicle, 
        LoadingStage.Blocks ,
        LoadingStage.Pylons, 
        LoadingStage.Decos
    ])]
    Replay,

    [ViewTypeMetadata(
    name: "Map",
    points:
    [
        "See how the map actually looks like",
        "No need to rely on thumbnails anymore",
        "Look into various map parameters in context"
    ],
    link: "map",
    extensions: ["Challenge.Gbx", "Map.Gbx"],
    loadingStages: [
        LoadingStage.Blocks ,
        LoadingStage.Pylons, 
        LoadingStage.Decos
    ])]
    Map,
    
    [ViewTypeMetadata(
    name: "Ghost",
    points:
    [
        "Only have a Ghost.Gbx? Put it here and import the map additionally",
        "Same experience as with Replay viewing"
    ],
    link: "ghost",
    extensions: ["Ghost.Gbx"],
    loadingStages: [])]
    Ghost,
    
    [ViewTypeMetadata(
    name: "Skin",
    points:
    [
        "Check out the skins before you equip them",
        "Open doors or bonnets, if you wish so (soon)"
    ],
    link: "skin",
    extensions: ["zip"],
    loadingStages: [])]
    Skin,
    
    [ViewTypeMetadata(
    name: "Item",
    points:
    [
        "See the item in a real shape without relying on icons or screenshots",
        "Find out their mesh parameters or the placement metadata"
    ],
    link: "item",
    extensions: ["Item.Gbx", "Block.Gbx"],
    loadingStages: [])]
    Item,
    
    [ViewTypeMetadata(
    name: "Mesh",
    points:
    [
        "If you only have the mesh piece in Gbx format",
        "Similar overview to Item viewing"
    ],
    link: "mesh",
    extensions: ["Mesh.Gbx", "Solid.Gbx", "Solid2.Gbx", "Prefab.Gbx"],
    loadingStages: [])]
    Mesh,
    
    
}

internal static class View3dTypeExtensions
{
    internal static ViewTypeMetadata GetMetadata(this View3dType value)
    {
        var member = typeof(View3dType).GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttributes(typeof(ViewTypeMetadata), false)
            .Cast<ViewTypeMetadata>()
            .FirstOrDefault()!;
    }
}