using GBX.NET.Engines.Game;

namespace GbxTools3D.Client.Extensions;

public static class CGameCtnChallengeExtensions
{
    public static int GetNumberOfLaps(this CGameCtnChallenge map)
    {
        return map.IsLapRace == true ? map.NbLaps : 1;
    }
}
