using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
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
        _mapper = mapper;
    }

    public async Task<DungeonHubDto> CreateAsync(
        Guid characterId,
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
            cancellationToken);
        var rewardsByDungeon = await _previewRewards.GetPossibleCompletionRewardsAsync(
            dungeons,
            cancellationToken);
        var gatheringItemIds = dungeons
            .SelectMany(dungeon => dungeon.GatheringNodes)
            .SelectMany(node => node.Loot)
            .Select(loot => loot.ItemId)
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
        return new DungeonGatheringNodePreviewDto
        {
            Id = node.Id,
            Name = node.Name,
            Type = node.Type.ToString(),
            LevelRequirement = node.LevelRequirement,
            ProcChance = node.ProcChance,
            Loot = node.Loot
                .Where(loot => itemBases.ContainsKey(loot.ItemId))
                .Select(loot => new DungeonGatheringLootPreviewDto
                {
                    Id = loot.ItemId,
                    ItemId = loot.ItemId,
                    ItemBase = _mapper.Map<ItemBaseDto>(itemBases[loot.ItemId]),
                    MinQuantity = loot.MinQuantity,
                    MaxQuantity = loot.MaxQuantity,
                    IsRare = loot.IsRare
                })
                .ToList()
        };
    }

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
