using GBX.NET.Engines.Plug;

namespace GbxTools3D.Extensions;

internal static class CPlugSolid2ModelExtensions
{
    public static void PopulateUsedMaterials(this CPlugSolid2Model solid2, Dictionary<string, CPlugMaterial?> materials, string relativeTo)
    {
        foreach (var extMat in solid2.Materials ?? [])
        {
            if (extMat.File is null)
            {
                continue;
            }

            var materialRelPath = Path.GetRelativePath(relativeTo, extMat.File.GetFullPath());
            materials.TryAdd(materialRelPath, extMat.Node);
        }
    }
}