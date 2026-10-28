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
        if (tree.References.Shader.File is not null)
        {
            var materialRelPath = Path.GetRelativePath(relativeTo, tree.References.Shader.File.GetFullPath());
            materials.TryAdd(materialRelPath, tree.Shader as CPlugMaterial);
        }

        var materialFiles = tree
            .GetAllChildren(includeVisualMipLevels: true)
            .Where(x => x.References.Shader.File is not null)
            .Select(x => (x.References.Shader.File!, x.Shader as CPlugMaterial)) ?? [];

        foreach (var (materialFile, material) in materialFiles)
        {
            var materialRelPath = Path.GetRelativePath(relativeTo, materialFile.GetFullPath());
            materials.TryAdd(materialRelPath, material);
        }
    }
}