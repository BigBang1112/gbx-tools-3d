using GBX.NET.Engines.Plug;

namespace GbxTools3D.Extensions;

internal static class CPlugSolidExtensions
{
    public static void PopulateUsedMaterials(this CPlugSolid solid, Dictionary<string, CPlugMaterial?> materials, string relativeTo)
    {
        var tree = (CPlugTree?)solid.Tree;

        if (tree is null)
        {
            return;
        }

        // root tree material
        if (tree.ShaderFile is not null)
        {
            var materialRelPath = Path.GetRelativePath(relativeTo, tree.ShaderFile.GetFullPath());
            materials.TryAdd(materialRelPath, tree.Shader as CPlugMaterial);
        }

        var materialFiles = tree
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