namespace GbxTools3D.Client.Extensions;

public static class PathExtensions
{
    public static string NormalizePath(this string path)
    {
        return path.Replace('\\', Path.DirectorySeparatorChar);
    }
}
