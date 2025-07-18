using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;

namespace GbxTools3D.Extensions;

internal static class CSceneMobilExtensions
{
    public static CPlugSolid? GetSolid(this CSceneMobil mobil, string relativeTo, out string? relativePath)
    {
        if (mobil.Item?.Solid?.Tree is not CPlugSolid solid)
        {
            relativePath = null;
            return null;
        }

        relativePath = mobil.Item.Solid.TreeFile is null ? null
            : Path.GetRelativePath(relativeTo, mobil.Item.Solid.TreeFile.GetFullPath());
        return solid;
    }
}