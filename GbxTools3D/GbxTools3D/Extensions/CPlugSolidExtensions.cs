using GBX.NET.Engines.Plug;

namespace GbxTools3D.Extensions;

internal static class CPlugSolidExtensions
{
    public static void PopulateUsedMaterials(this CPlugSolid solid, Dictionary<string, CPlugMaterial?> materials, string relativeTo)
    {
        var materialFiles = ((CPlugTree?)solid.Tree)?
            .GetAllChildren(includeVisualMipLevels: true)
            .Where(x => x.ShaderFile is not null)
            .Select(x => (x.ShaderFile!, x.Shader as CPlugMaterial)) ?? [];

        foreach (var (materialFile, material) in materialFiles)
        {
            var materialRelPath = Path.GetRelativePath(relativeTo, materialFile.GetFullPath());
            materials.TryAdd(materialRelPath, material);
        }
    }
}