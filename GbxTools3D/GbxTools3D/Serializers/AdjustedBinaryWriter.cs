using System.Text;

namespace GbxTools3D.Serializers;

internal sealed class AdjustedBinaryWriter : BinaryWriter
{
    private readonly Dictionary<string, int> strings = [];

    public AdjustedBinaryWriter(Stream output) : base(output)
    {
    }

    public AdjustedBinaryWriter(Stream output, Encoding encoding) : base(output, encoding)
    {
    }

    public AdjustedBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
    {
    }

    public void WriteRepeatingString(string s)
    {
        if (strings.TryGetValue(s, out var index))
        {
            Write7BitEncodedInt(index);
            return;
        }

        Write7BitEncodedInt(0);
        strings[s] = strings.Count + 1;
        Write(s);
    }
}
