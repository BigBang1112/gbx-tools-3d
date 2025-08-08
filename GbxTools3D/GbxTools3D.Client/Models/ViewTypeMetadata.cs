using GbxTools3D.Client.Enums;

namespace GbxTools3D.Client.Models;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class ViewTypeMetadata(string name, string[] points, string link, string[] extensions, LoadingStageKind[] loadingStages) : Attribute
{
    public string Name { get; } = name;
    public string[] Points { get; } = points;
    public string Link { get; } = link;
    public string[] Extensions { get; } = extensions;
    public LoadingStageKind[] LoadingStages { get; } = loadingStages;
}
