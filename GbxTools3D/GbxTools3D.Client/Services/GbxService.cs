using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;

namespace GbxTools3D.Client.Services;

public sealed class GbxService
{
    public List<Gbx> List { get; } = [];

    public Gbx<CGameCtnChallenge>? SelectedMap { get; private set; }
    public Gbx<CGameCtnReplayRecord>? SelectedReplay { get; private set; }
    public Gbx? SelectedMesh { get; private set; }
    public Gbx<CGameItemModel>? SelectedItem { get; private set; }

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
        SelectedMesh = null;
        SelectedItem = null;
    }

    private void SelectEnsured(Gbx gbx)
    {
        SelectedMap = null;
        SelectedReplay = null;
        SelectedMesh = null;
        SelectedItem = null;

        switch (gbx)
        {
            case Gbx<CGameCtnChallenge> map:
                SelectedMap = map;
                break;
            case Gbx<CGameCtnReplayRecord> replay:
                SelectedReplay = replay;
                break;
            case Gbx<CPlugSolid>:
            case Gbx<CPlugSolid2Model>:
            case Gbx<CPlugPrefab>:
                SelectedMesh = gbx;
                break;
            case Gbx<CGameItemModel> item:
                SelectedItem = item;
                break;
        }
    }
}
