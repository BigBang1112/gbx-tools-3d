using GBX.NET.Engines.Plug;

namespace GbxTools3D.Extensions;

internal static class CPlugPrefabExtensions
{
    public static void PopulateUsedMaterials(this CPlugPrefab prefab, Dictionary<string, CPlugMaterial?> materials, string relativeTo)
    {
        foreach (var ent in prefab.Ents)
        {
            if (ent.Model is CPlugStaticObjectModel { Mesh: not null } staticObject)
            {
                staticObject.Mesh.PopulateUsedMaterials(materials, relativeTo);
            }
        }
    }
}