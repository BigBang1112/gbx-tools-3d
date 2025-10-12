using GBX.NET;
using GBX.NET.Engines.Game;
using System.Collections.Immutable;

namespace GbxTools3D.Client;

public static class GameVersionSupport
{
    public static ImmutableArray<GameVersion> Versions => [GameVersion.TM2020, GameVersion.TMF, GameVersion.MP4, GameVersion.TMT, GameVersion.TMSX, GameVersion.TMNESWC];

    public static GameVersion GetSupportedGameVersion(CGameCtnChallenge map)
    {
        var gameVersion = map.GameVersion;

        if (gameVersion == (GameVersion.MP3 | GameVersion.TMT))
        {
            gameVersion = map.TitleId == "TMCE@nadeolabs" ? GameVersion.TMT : GameVersion.MP3;
        }

        if (gameVersion == GameVersion.TMU)
        {
            gameVersion = GameVersion.TMF;
        }

        if (gameVersion == GameVersion.MP3) // temporary
        {
            gameVersion = GameVersion.MP4;
        }

        if (gameVersion < GameVersion.TMSX) // temporary
        {
            gameVersion = GameVersion.TMF;
        }

        return gameVersion;
    }

    public static GameVersion GetSupportedGameVersion(CGameCtnGhost ghost)
    {
        var gameVersion = ghost.GameVersion;

        if (gameVersion == (GameVersion.TMT | GameVersion.MP4 | GameVersion.TM2020))
        {
            gameVersion = GameVersion.TMT;
        }

        if (gameVersion == (GameVersion.MP4 | GameVersion.TM2020))
        {
            gameVersion = GameVersion.MP4;
        }

        // TMU specifically is not supported yet
        if (gameVersion == GameVersion.TMU)
        {
            gameVersion = GameVersion.TMF;
        }

        // looks like some TMF ghosts have different chunks than ones in replays
        // FIXED: can be removed in next gbx.net update
        if (gameVersion == GameVersion.Unspecified)
        {
            gameVersion = GameVersion.TMF;
        }

        return gameVersion;
    }
}
