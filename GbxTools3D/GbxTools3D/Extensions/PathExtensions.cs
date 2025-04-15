namespace GbxTools3D.Extensions;

public static class PathExtensions
{
    public static string NormalizePath(this string path)
    {
        return path.Replace('\\', Path.DirectorySeparatorChar);
    }
}
