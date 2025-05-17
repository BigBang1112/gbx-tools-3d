using GBX.NET;
using System.Collections.Immutable;

namespace GbxTools3D;

public static class GameVersionSupport
{
    public static ImmutableArray<GameVersion> Versions => [GameVersion.TMSX, GameVersion.TMNESWC, GameVersion.TMF];
}
