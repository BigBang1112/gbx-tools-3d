using GBX.NET;
using GBX.NET.Engines.Game;

namespace GbxTools3D.Client.Services;

public sealed class GbxService
{
    public List<Gbx> List { get; } = [];

    public Gbx<CGameCtnChallenge>? SelectedMap { get; private set; }
    public Gbx<CGameCtnReplayRecord>? SelectedReplay { get; private set; }

    public void Select(Gbx gbx)
    {
        if (!List.Contains(gbx))
        {
            throw new ArgumentException("Gbx not in list");
        }

        SelectEnsured(gbx);
    }

    public void Add(Gbx gbx, bool select = true)
    {
        List.Add(gbx);

        if (select)
        {
            SelectEnsured(gbx);
        }
    }

    public void Deselect()
    {
        SelectedMap = null;
        SelectedReplay = null;
    }

    private void SelectEnsured(Gbx gbx)
    {
        SelectedMap = null;
        SelectedReplay = null;

        switch (gbx)
        {
            case Gbx<CGameCtnChallenge> map:
                SelectedMap = map;
                break;
            case Gbx<CGameCtnReplayRecord> replay:
                SelectedReplay = replay;
                break;
        }
    }
}
