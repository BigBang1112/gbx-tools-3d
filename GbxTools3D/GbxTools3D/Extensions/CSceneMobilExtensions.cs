using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;

namespace GbxTools3D.Extensions;

internal static class CSceneMobilExtensions
{
    public static CPlugSolid? GetSolid(this CSceneMobil mobil, string relativeTo, out string? relativePath)
    {
        if (mobil.Item?.Solid?.Model is not CPlugSolid solid)
        {
            relativePath = null;
            return null;
        }

        var modelFile = mobil.Item.Solid.References.Model.File;
        relativePath = modelFile is null ? null
            : Path.GetRelativePath(relativeTo, modelFile.GetFullPath());
        return solid;
    }
}