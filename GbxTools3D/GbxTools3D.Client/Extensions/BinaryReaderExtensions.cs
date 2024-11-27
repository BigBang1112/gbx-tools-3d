namespace GbxTools3D.Client.Extensions;

public static class BinaryReaderExtensions
{
    public static string ReadRepeatingString(this BinaryReader reader, Dictionary<int, string> strings)
    {
        var index = reader.Read7BitEncodedInt();

        if (index == 0)
        {
            var str = reader.ReadString();
            strings.Add(strings.Count, str);
            return str;
        }

        return strings[index - 1];
    }
}
