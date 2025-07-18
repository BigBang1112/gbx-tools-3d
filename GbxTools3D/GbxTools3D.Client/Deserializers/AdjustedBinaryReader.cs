using System.Text;

namespace GbxTools3D.Client.Deserializers;

public sealed class AdjustedBinaryReader : BinaryReader
{
    private readonly Dictionary<int, string> strings = [];

    public AdjustedBinaryReader(Stream input) : base(input)
    {
    }

    public AdjustedBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
    {
    }

    public AdjustedBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
    {
    }

    public string ReadRepeatingString()
    {
        var index = Read7BitEncodedInt();

        if (index == 0)
        {
            var str = ReadString();
            strings.Add(strings.Count, str);
            return str;
        }

        return strings[index - 1];
    }
}