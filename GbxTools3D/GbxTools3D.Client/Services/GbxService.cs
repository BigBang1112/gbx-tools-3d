using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using System.IO.Compression;

namespace GbxTools3D.Client.Services;

// this is done horribly and should be redone
public sealed class GbxService
{
    public List<Gbx> List { get; } = [];
    private List<ZipArchive> Zips { get; } = []; // should be moved to CPlugFileZip once it works

    public Gbx<CGameCtnChallenge>? SelectedMap { get; private set; }
    public Gbx<CGameCtnReplayRecord>? SelectedReplay { get; private set; }
    public Gbx<CGameCtnGhost>? SelectedGhost { get; private set; }
    public Gbx? SelectedMesh { get; private set; }
    public Gbx<CGameItemModel>? SelectedItem { get; private set; }
    public ZipArchive? SelectedSkinZip { get; private set; }

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

    public void Add(ZipArchive zip, bool select = true)
    {
        Zips.Add(zip);

        if (select)
        {
            SelectEnsured(zip);
        }
    }

    public void Deselect()
    {
        SelectedMap = null;
        SelectedReplay = null;
        SelectedGhost = null;
        SelectedMesh = null;
        SelectedItem = null;
        SelectedSkinZip = null;
    }

    private void SelectEnsured(Gbx gbx)
    {
        SelectedMap = null;
        SelectedReplay = null;
        SelectedGhost = null;
        SelectedMesh = null;
        SelectedItem = null;
        SelectedSkinZip = null;

        switch (gbx)
        {
            case Gbx<CGameCtnChallenge> map:
                SelectedMap = map;
                break;
            case Gbx<CGameCtnReplayRecord> replay:
                SelectedReplay = replay;
                break;
            case Gbx<CGameCtnGhost> ghost:
                SelectedGhost = ghost;
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

    private void SelectEnsured(ZipArchive zip)
    {
        SelectedMap = null;
        SelectedReplay = null;
        SelectedGhost = null;
        SelectedMesh = null;
        SelectedItem = null;
        SelectedSkinZip = zip;
    }
}
