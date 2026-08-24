using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Rewards;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Rewards;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetAvailableDungeons;

public record GetAvailableDungeonsQuery(Guid CharacterId) : IQuery<DungeonHubDto>;

public sealed class GetAvailableDungeonsQueryHandler(DungeonHubFactory hub)
    : IRequestHandler<GetAvailableDungeonsQuery, DungeonHubDto>
{
    public Task<DungeonHubDto> Handle(
        GetAvailableDungeonsQuery request,
        CancellationToken cancellationToken) =>
        hub.CreateAsync(request.CharacterId, cancellationToken);
}

public sealed class DungeonHubFactory
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonAccessPolicy _dungeonAccess;
    private readonly IDungeonPreviewRewardService _previewRewards;
    private readonly ICharacterRepository _characters;
    private readonly IDungeonRunService _dungeonRuns;
    private readonly IDungeonMasteryService _mastery;
    private readonly IItemBaseRepository _itemBases;
    private readonly IDungeonSigilAssemblySettingsProvider _sigilAssemblySettings;
    private readonly IRewardTableDefinitionProvider _rewardTables;
    private readonly IMapper _mapper;

    public DungeonHubFactory(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonAccessPolicy dungeonAccess,
        IDungeonPreviewRewardService previewRewards,
        ICharacterRepository characters,
        IDungeonRunService dungeonRuns,
        IDungeonMasteryService mastery,
        IItemBaseRepository itemBases,
        IDungeonSigilAssemblySettingsProvider sigilAssemblySettings,
        IRewardTableDefinitionProvider rewardTables,
        IMapper mapper)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonAccess = dungeonAccess;
        _previewRewards = previewRewards;
        _characters = characters;
        _dungeonRuns = dungeonRuns;
        _mastery = mastery;
        _itemBases = itemBases;
        _sigilAssemblySettings = sigilAssemblySettings;
        _rewardTables = rewardTables;
        _mapper = mapper;
    }

    public async Task<DungeonHubDto> CreateAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await CreateAsync(
            characterId,
            new Dictionary<string, int>(),
            cancellationToken);

    public async Task<DungeonHubDto> CreateAsync(
        Guid characterId,
        IReadOnlyDictionary<string, int> inventoryQuantityOverrides,
        CancellationToken cancellationToken)
    {
        var previews = new List<DungeonPreviewDto>();
        var sigilFragments = await _characters.GetSigilFragmentsAsync(
            characterId,
            cancellationToken);

        var dungeons = _dungeonDefinitions.GetAll()
            .OrderBy(x => DungeonDefinitionIdentity.GetFamilyId(x.Id))
            .ThenBy(x => x.Grade)
            .ToList();
        var completionRecords = await _dungeonRuns.GetCompletionRecordsAsync(
            characterId,
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var records = completionRecords.ToDictionary(
            x => x.DungeonDefinitionId,
            StringComparer.OrdinalIgnoreCase);
        var masteryByDungeon = await _mastery.GetMasteryByDungeonAsync(
            characterId,
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var accessByDungeon = await _dungeonAccess.EvaluateForPreviewAsync(
            characterId,
            dungeons,
            inventoryQuantityOverrides,
            cancellationToken);
        var rewardsByDungeon = await _previewRewards.GetPossibleCompletionRewardsAsync(
            dungeons,
            cancellationToken);
        var gatheringItemIds = dungeons
            .SelectMany(dungeon => dungeon.GatheringNodes)
            .SelectMany(GetGatheringDrops)
            .Select(drop => drop.ItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var gatheringItemBases = await _itemBases.GetItemBasesByIdsAsync(
            gatheringItemIds,
            cancellationToken);
        var sigilSettings = _sigilAssemblySettings.GetSettings();

        foreach (var dungeon in dungeons)
        {
            records.TryGetValue(dungeon.Id, out var record);
            masteryByDungeon.TryGetValue(dungeon.Id, out var mastery);
            var accessSnapshot = accessByDungeon[dungeon.Id];
            var access = accessSnapshot.Entry;
            var sigilAssemblyAccess = accessSnapshot.SigilAssembly;
            var sigilRequirement = access.EntryRequirements.FirstOrDefault(x =>
                x.ItemId.Equals(dungeon.SigilItemId, StringComparison.OrdinalIgnoreCase));

            previews.Add(new DungeonPreviewDto
            {
                Id = dungeon.Id,
                Region = dungeon.Region,
                FamilyId = DungeonDefinitionIdentity.GetFamilyId(dungeon.Id),
                FamilyTitle = DungeonDefinitionIdentity.GetFamilyTitle(dungeon.Name),
                Title = dungeon.Name,
                Difficulty = FormatDifficulty(dungeon.Grade),
                Tier = dungeon.Tier,
                Grade = FormatGrade(dungeon.Grade),
                CanEnter = access.CanEnter,
                MissingRequirements = [.. access.MissingRequirements],
                EntryRequirements = _mapper.Map<List<DungeonEntryRequirementDto>>(access.EntryRequirements),
                SigilItemId = string.IsNullOrWhiteSpace(dungeon.SigilItemId) ? null : dungeon.SigilItemId,
                SigilName = sigilRequirement?.Name,
                CanAssembleSigil = sigilSettings.Enabled && sigilAssemblyAccess?.CanEnter == true,
                SigilAssemblyMissingRequirements = sigilAssemblyAccess?.MissingRequirements.ToList() ?? [],
                RequiredTowerFloor = dungeon.RequiredTowerFloor,
                RequiredPreviousDungeonId = dungeon.RequiredPreviousDungeonId,
                MinRooms = dungeon.MinRooms,
                MaxRooms = dungeon.MaxRooms,
                Record = MapRecord(record),
                Mastery = MapMastery(mastery),
                Rewards = _mapper.Map<List<DungeonPreviewRewardDto>>(rewardsByDungeon[dungeon.Id]),
                GatheringNodes = MapGatheringNodes(dungeon, gatheringItemBases)
            });
        }

        return new DungeonHubDto
        {
            SigilFragments = sigilFragments ?? 0,
            SigilAssemblyEnabled = sigilSettings.Enabled,
            SigilAssemblyCost = sigilSettings.FragmentCost,
            Dungeons = previews
        };
    }

    private List<DungeonGatheringNodePreviewDto> MapGatheringNodes(
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        IReadOnlyDictionary<string, ItemBase> itemBases)
    {
        if (dungeon.GatheringNodes.Count == 0)
        {
            return [];
        }

        return dungeon.GatheringNodes
            .Select(node => MapGatheringNode(node, itemBases))
            .Where(node => node.Loot.Count > 0)
            .ToList();
    }

    private DungeonGatheringNodePreviewDto MapGatheringNode(
        DungeonGatheringNodeDefinition node,
        IReadOnlyDictionary<string, ItemBase> itemBases)
    {
        var drops = GetGatheringDrops(node);

        return new DungeonGatheringNodePreviewDto
        {
            Id = node.Id,
            Name = node.Name,
            Type = node.Type.ToString(),
            LevelRequirement = node.LevelRequirement,
            ProcChance = node.ProcChance,
            Loot = drops
                .Where(drop => itemBases.ContainsKey(drop.ItemId))
                .Select(drop => new DungeonGatheringLootPreviewDto
                {
                    Id = drop.ItemId,
                    ItemId = drop.ItemId,
                    ItemBase = _mapper.Map<ItemBaseDto>(itemBases[drop.ItemId]),
                    MinQuantity = drop.MinQuantity,
                    MaxQuantity = drop.MaxQuantity,
                    DropChancePercent = drop.DropChancePercent,
                    IsRare = drop.IsRare
                })
                .ToList()
        };
    }

    private IReadOnlyList<GatheringDropPreview> GetGatheringDrops(
        DungeonGatheringNodeDefinition node)
    {
        var nodeChance = Math.Clamp(node.ProcChance, 0f, 1f);
        var totalInlineWeight = node.Loot.Sum(loot => Math.Max(0d, loot.Weight));
        var inlineTotalWithNoDrop = totalInlineWeight + Math.Max(0d, 100d - totalInlineWeight);
        var drops = node.Loot
            .Where(loot => !string.IsNullOrWhiteSpace(loot.ItemId))
            .Select(loot => new GatheringDropPreview(
                loot.ItemId,
                loot.MinQuantity,
                loot.MaxQuantity,
                ProbabilityToPercent(
                    nodeChance * (inlineTotalWithNoDrop <= 0d
                        ? 0d
                        : Math.Max(0d, loot.Weight) / inlineTotalWithNoDrop)),
                loot.IsRare))
            .ToList();

        foreach (var rewardTableId in node.BonusRewardTableIds)
        {
            drops.AddRange(AnalyzeGatheringRewardTable(
                _rewardTables.GetById(rewardTableId),
                nodeChance));
        }

        return drops;
    }

    private IEnumerable<GatheringDropPreview> AnalyzeGatheringRewardTable(
        RewardTableDefinition table,
        double parentProbability)
    {
        foreach (var roll in table.Rolls)
        {
            var rollProbability = parentProbability * Math.Clamp(roll.Chance, 0d, 1d);
            foreach (var entry in roll.Entries)
            {
                var probability = CalculateEntryProbability(roll, entry, rollProbability);
                if (entry.Type == RewardEntryType.Item && !string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    yield return new GatheringDropPreview(
                        entry.ItemId,
                        entry.Quantity.Min,
                        Math.Max(entry.Quantity.Min, entry.Quantity.Max) * Math.Max(1, roll.Rolls),
                        ProbabilityToPercent(probability),
                        entry.Tags.Contains("rare", StringComparer.OrdinalIgnoreCase));
                    continue;
                }

                if (entry.Type == RewardEntryType.RewardTableReference &&
                    !string.IsNullOrWhiteSpace(entry.RewardTableId))
                {
                    foreach (var nested in AnalyzeGatheringRewardTable(
                        _rewardTables.GetById(entry.RewardTableId),
                        probability))
                    {
                        yield return nested;
                    }
                }
            }
        }
    }

    private static double CalculateEntryProbability(
        RewardRollDefinition roll,
        RewardEntryDefinition entry,
        double rollProbability)
    {
        var entryChance = Math.Clamp(entry.Chance, 0d, 1d);
        var probability = roll.Type switch
        {
            RewardRollType.Weighted => GetWeightedProbability(roll, entry, false),
            RewardRollType.WeightedWithNoDrop => GetWeightedProbability(roll, entry, true),
            _ => 1d
        };
        var perRollProbability = probability * rollProbability * entryChance;
        return 1d - Math.Pow(1d - Math.Clamp(perRollProbability, 0d, 1d), Math.Max(1, roll.Rolls));
    }

    private static double GetWeightedProbability(
        RewardRollDefinition roll,
        RewardEntryDefinition entry,
        bool includeNoDrop)
    {
        var totalWeight = roll.Entries.Sum(candidate => Math.Max(0d, candidate.Weight)) +
            (includeNoDrop ? Math.Max(0d, roll.NoDropWeight) : 0d);
        return totalWeight <= 0d ? 0d : Math.Max(0d, entry.Weight) / totalWeight;
    }

    private static double ProbabilityToPercent(double probability) =>
        Math.Round(Math.Clamp(probability, 0d, 1d) * 100d, 4);

    private sealed record GatheringDropPreview(
        string ItemId,
        int MinQuantity,
        int MaxQuantity,
        double DropChancePercent,
        bool IsRare);

    private DungeonRecordDto MapRecord(DungeonCompletionRecord? record)
    {
        return record is null
            ? new DungeonRecordDto()
            : _mapper.Map<DungeonRecordDto>(record);
    }

    private DungeonMasteryDto MapMastery(DungeonMasterySnapshot? mastery)
    {
        return mastery is null
            ? new DungeonMasteryDto()
            : _mapper.Map<DungeonMasteryDto>(mastery);
    }

    private static string FormatGrade(Domain.Models.Dungeons.Definitions.DungeonGrade grade) =>
        grade switch
        {
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeII => "Grade II",
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeIII => "Grade III",
            _ => "Grade I"
        };

    private static string FormatDifficulty(Domain.Models.Dungeons.Definitions.DungeonGrade grade) =>
        grade switch
        {
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeII => "Veteran",
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeIII => "Champion",
            _ => "Novice"
        };

}
