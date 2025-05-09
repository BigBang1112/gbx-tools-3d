using GBX.NET.Engines.Game;
using System.Xml;

namespace GbxTools3D.Client.Extensions;

public static class CGameCtnGhostExtensions
{
    public static int? GetNumberOfLaps(this CGameCtnGhost ghost, CGameCtnChallenge? fallback = null)
    {
        if (ghost.Validate_RaceSettings is null || ghost.Validate_RaceSettings == "1P-Time")
        {
            return fallback?.GetNumberOfLaps();
        }

        // TODO: use MiniXmlReader
        var readerSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

        using var strReader = new StringReader(ghost.Validate_RaceSettings);
        using var reader = XmlReader.Create(strReader, readerSettings);

        try
        {
            reader.ReadToDescendant("laps");
            return reader.ReadElementContentAsInt();
        }
        catch
        {
            return fallback?.GetNumberOfLaps();
        }
    }
}
