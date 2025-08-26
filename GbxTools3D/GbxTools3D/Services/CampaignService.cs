using GBX.NET;
using GBX.NET.Engines.Game;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

internal sealed class CampaignService
{
    private readonly AppDbContext db;
    private readonly ILogger<CampaignService> logger;

    public CampaignService(AppDbContext db, ILogger<CampaignService> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task CreateOrUpdateCampaignsAsync(string datasetPath, GameVersion gameVersion, CancellationToken cancellationToken)
    {
        var gameFolder = gameVersion.ToString();
        var gamePath = Path.Combine(datasetPath, gameFolder);

        if (!Directory.Exists(gamePath))
        {
            logger.LogWarning("Game folder {GameFolder} does not exist in dataset path {DatasetPath}. Skipping campaign import for this version.", gameFolder, datasetPath);
            return;
        }

        var mapUidGroupDict = new Dictionary<string, MapGroup>();
        var mapUidMapWithNoGroupDict = new Dictionary<string, Map>();

        foreach (var mapOrCampaignFilePath in Directory.EnumerateFiles(
                     Path.Combine(datasetPath, gameFolder, "Tracks", "Campaigns"), "*.Gbx", SearchOption.AllDirectories))
        {
            var node = Gbx.ParseHeaderNode(mapOrCampaignFilePath);

            switch (node)
            {
                case CGameCtnCampaign campaignNode:
                    campaignNode = Gbx.Parse<CGameCtnCampaign>(mapOrCampaignFilePath);

                    logger.LogInformation("Checking campaign {Campaign}...", campaignNode.Name);

                    var campaign =
                        await db.Campaigns
                            .FirstOrDefaultAsync(x => x.Name == campaignNode.CampaignId && x.GameVersion == gameVersion, cancellationToken);

                    if (campaign is null)
                    {
                        campaign = new Campaign
                        {
                            GameVersion = gameVersion,
                            Name = campaignNode.CampaignId ?? "",
                            DisplayName = campaignNode.Name,
                            CollectionId = campaignNode.CollectionId,
                        };

                        await db.Campaigns.AddAsync(campaign, cancellationToken);
                    }

                    campaign.DisplayName = campaignNode.Name;
                    campaign.CollectionId = campaignNode.CollectionId;

                    foreach (var (i, group) in campaignNode.ChallengeGroups?.Index() ?? [])
                    {
                        var mapGroup = await db.MapGroups
                            .FirstOrDefaultAsync(x => x.Campaign == campaign && x.Name == group.Name, cancellationToken);

                        if (mapGroup is null)
                        {
                            mapGroup = new MapGroup
                            {
                                Campaign = campaign,
                                Name = group.Name ?? "",
                                Order = i
                            };

                            await db.MapGroups.AddAsync(mapGroup, cancellationToken);
                        }

                        mapGroup.Name = group.Name ?? "";
                        mapGroup.Order = i;

                        foreach (var mapInfo in group.MapInfos ?? [])
                        {
                            if (mapInfo.Metadata is null) continue;

                            if (!mapUidGroupDict.ContainsKey(mapInfo.Metadata.Id))
                            {
                                mapUidGroupDict[mapInfo.Metadata.Id] = mapGroup;
                            }
                        }
                    }
                    break;
                case CGameCtnChallenge mapNode:
                    var map = await db.Maps
                        .Include(x => x.Group)
                        .FirstOrDefaultAsync(x => x.MapUid == mapNode.MapUid && x.GameVersion == gameVersion, cancellationToken);

                    if (map is null)
                    {
                        map = new Map
                        {
                            GameVersion = gameVersion,
                            MapUid = mapNode.MapUid,
                            Data = await File.ReadAllBytesAsync(mapOrCampaignFilePath, cancellationToken)
                        };
                        await db.Maps.AddAsync(map, cancellationToken);
                    }

                    map.Path = Path.GetRelativePath(gamePath, mapOrCampaignFilePath);
                    map.Data = await File.ReadAllBytesAsync(mapOrCampaignFilePath, cancellationToken);

                    if (mapUidGroupDict.TryGetValue(mapNode.MapUid, out var mapGroupFromDict))
                    {
                        map.Group = mapGroupFromDict;
                    }
                    else
                    {
                        mapUidMapWithNoGroupDict[mapNode.MapUid] = map;
                    }
                    break;
            }
        }

        foreach (var (mapUid, map) in mapUidMapWithNoGroupDict)
        {
            if (mapUidGroupDict.TryGetValue(mapUid, out var mapGroup))
            {
                map.Group = mapGroup;
            }
        }

        logger.LogInformation("Saving map changes to database...");

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Campaign import for {GameFolder} completed.", gameFolder);
    }
}
