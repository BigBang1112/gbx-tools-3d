using GBX.NET;
using GBX.NET.Engines.Game;
using System.Collections.Immutable;

namespace GbxTools3D.Client;

public static class GameVersionSupport
{
    public static ImmutableArray<GameVersion> Versions => [GameVersion.TMF, GameVersion.MP4, GameVersion.TMT, GameVersion.TMSX, GameVersion.TMNESWC];

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
}
