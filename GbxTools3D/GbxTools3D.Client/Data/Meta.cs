using GBX.NET;
using GbxTools3D.Client.Extensions;
using System.IO.Compression;

namespace GbxTools3D.Client.Data;

public sealed class Meta
{
    private static readonly byte[] MAGIC = [0x24, 0xC4, 0xF4, 0x34, 0xB4, 0x02];

    private const int VERSION = 0;

    public required string PageName { get; init; }
    public required Int3 AirBlockSize { get; init; }
    public required Int3 GroundBlockSize { get; init; }

    public static async Task<Meta> ParseAsync(Stream stream)
    {
        await using var deflate = new DeflateStream(stream, CompressionMode.Decompress);
        using var reader = new BinaryReader(deflate);

        Dictionary<int, string> strings = new();

        var magic = reader.ReadBytes(6);

        if (!magic.SequenceEqual(MAGIC))
        {
            throw new InvalidDataException("Invalid file format");
        }

        var version = reader.Read7BitEncodedInt();

        var pageName = reader.ReadString();

        var airBlockSize = new Int3();

        // read air unit count
        var airUnitCount = reader.Read7BitEncodedInt();
        for (int i = 0; i < airUnitCount; i++)
        {
            var airUnitOffsetX = reader.Read7BitEncodedInt();
            var airUnitOffsetY = reader.Read7BitEncodedInt();
            var airUnitOffsetZ = reader.Read7BitEncodedInt();

            if (airUnitOffsetX > airBlockSize.X) airBlockSize = airBlockSize with { X = airUnitOffsetX };
            if (airUnitOffsetY > airBlockSize.Y) airBlockSize = airBlockSize with { Y = airUnitOffsetY };
            if (airUnitOffsetZ > airBlockSize.Z) airBlockSize = airBlockSize with { Z = airUnitOffsetZ };

            // read clip count for air unit
            var airUnitClipCount = reader.Read7BitEncodedInt();
            for (int j = 0; j < airUnitClipCount; j++)
            {
                var clipDir = reader.Read7BitEncodedInt();
                var clipName = reader.ReadRepeatingString(strings);
            }
        }

        var groundBlockSize = new Int3();

        // read ground unit count
        var groundUnitCount = reader.Read7BitEncodedInt();
        for (int i = 0; i < groundUnitCount; i++)
        {
            var groundUnitOffsetX = reader.Read7BitEncodedInt();
            var groundUnitOffsetY = reader.Read7BitEncodedInt();
            var groundUnitOffsetZ = reader.Read7BitEncodedInt();

            if (groundUnitOffsetX > groundBlockSize.X) groundBlockSize = groundBlockSize with { X = groundUnitOffsetX };
            if (groundUnitOffsetY > groundBlockSize.Y) groundBlockSize = groundBlockSize with { Y = groundUnitOffsetY };
            if (groundUnitOffsetZ > groundBlockSize.Z) groundBlockSize = groundBlockSize with { Z = groundUnitOffsetZ };

            // read clip count for ground unit
            var groundUnitClipCount = reader.Read7BitEncodedInt();
            for (int j = 0; j < groundUnitClipCount; j++)
            {
                var clipDir = reader.Read7BitEncodedInt();
                var clipName = reader.ReadRepeatingString(strings);
            }
        }

        return new()
        {
            PageName = pageName,
            AirBlockSize = airBlockSize + (1, 1, 1),
            GroundBlockSize = groundBlockSize + (1, 1, 1)
        };
    }
}
