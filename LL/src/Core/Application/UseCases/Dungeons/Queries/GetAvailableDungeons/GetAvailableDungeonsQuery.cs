using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetAvailableDungeons;

public record GetAvailableDungeonsQuery(Guid CharacterId) : IQuery<DungeonHubDto>;

public sealed class GetAvailableDungeonsQueryHandler : IRequestHandler<GetAvailableDungeonsQuery, DungeonHubDto>
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonAccessPolicy _dungeonAccess;
    private readonly IDungeonPreviewRewardService _previewRewards;
    private readonly ICharacterService _characters;
    private readonly IDungeonRunService _dungeonRuns;
    private readonly IDungeonMasteryService _mastery;
    private readonly IItemBaseRepository _itemBases;
    private readonly IDungeonSigilAssemblySettingsProvider _sigilAssemblySettings;
    private readonly IMapper _mapper;
    private readonly IPowerRatingService _powerRatings;
    private readonly IDungeonPowerRecommendationStore _powerRecommendations;

    public GetAvailableDungeonsQueryHandler(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonAccessPolicy dungeonAccess,
        IDungeonPreviewRewardService previewRewards,
        ICharacterService characters,
        IDungeonRunService dungeonRuns,
        IDungeonMasteryService mastery,
        IItemBaseRepository itemBases,
        IDungeonSigilAssemblySettingsProvider sigilAssemblySettings,
        IPowerRatingService powerRatings,
        IDungeonPowerRecommendationStore powerRecommendations,
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
        _powerRatings = powerRatings;
        _powerRecommendations = powerRecommendations;
    }

    public async Task<DungeonHubDto> Handle(
        GetAvailableDungeonsQuery request,
        CancellationToken cancellationToken)
    {
        var previews = new List<DungeonPreviewDto>();
        var character = await _characters.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);
        var power = await _powerRatings.GetCharacterOverallRatingAsync(request.CharacterId, cancellationToken);
        var currentPartyPower = power.State is PowerAnalysisState.Available or PowerAnalysisState.LowConfidence
            ? power.Overall
            : 0;

        var dungeons = _dungeonDefinitions.GetAll()
            .OrderBy(x => DungeonDefinitionIdentity.GetFamilyId(x.Id))
            .ThenBy(x => x.Grade)
            .ToList();
        var completionRecords = await _dungeonRuns.GetCompletionRecordsAsync(
            request.CharacterId,
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var records = completionRecords.ToDictionary(
            x => x.DungeonDefinitionId,
            StringComparer.OrdinalIgnoreCase);
        var masteryByDungeon = await _mastery.GetMasteryByDungeonAsync(
            request.CharacterId,
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var sigilSettings = _sigilAssemblySettings.GetSettings();

        foreach (var dungeon in dungeons)
        {
            _powerRecommendations.TryGet(dungeon.Id, out var powerRecommendation);
            records.TryGetValue(dungeon.Id, out var record);
            masteryByDungeon.TryGetValue(dungeon.Id, out var mastery);
            var access = await _dungeonAccess.EvaluateAsync(
                request.CharacterId,
                dungeon,
                currentPartyPower,
                cancellationToken);
            var sigilAssemblyAccess = string.IsNullOrWhiteSpace(dungeon.SigilItemId)
                ? null
                : await _dungeonAccess.EvaluateForSigilAssemblyAsync(
                    request.CharacterId,
                    dungeon,
                    currentPartyPower,
                    cancellationToken);
            var sigilRequirement = access.EntryRequirements.FirstOrDefault(x =>
                x.ItemId.Equals(dungeon.SigilItemId, StringComparison.OrdinalIgnoreCase));

            previews.Add(new DungeonPreviewDto
            {
                Id = dungeon.Id,
                FamilyId = DungeonDefinitionIdentity.GetFamilyId(dungeon.Id),
                FamilyTitle = DungeonDefinitionIdentity.GetFamilyTitle(dungeon.Name),
                Title = dungeon.Name,
                Difficulty = FormatDifficulty(dungeon.Grade),
                Tier = dungeon.Tier,
                Grade = FormatGrade(dungeon.Grade),
                CurrentPartyPower = access.CurrentPartyPower,
                RecommendedPartyPower = powerRecommendation?.RecommendedPartyPower,
                PowerRecommendationLowConfidence = powerRecommendation is not null &&
                    (powerRecommendation.Confidence == PowerRatingConfidence.Low ||
                     powerRecommendation.State == PowerAnalysisState.LowConfidence),
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
                Rewards = await MapRewardsAsync(dungeon, cancellationToken),
                GatheringNodes = await MapGatheringNodesAsync(dungeon, cancellationToken)
            });
        }

        return new DungeonHubDto
        {
            SigilFragments = character?.SigilFragments ?? 0,
            SigilAssemblyEnabled = sigilSettings.Enabled,
            SigilAssemblyCost = sigilSettings.FragmentCost,
            Dungeons = previews
        };
    }

    private async Task<List<DungeonPreviewRewardDto>> MapRewardsAsync(
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var rewards = await _previewRewards.GetPossibleCompletionRewardsAsync(
            dungeon,
            cancellationToken);

        return _mapper.Map<List<DungeonPreviewRewardDto>>(rewards);
    }

    private async Task<List<DungeonGatheringNodePreviewDto>> MapGatheringNodesAsync(
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        if (dungeon.GatheringNodes.Count == 0)
        {
            return [];
        }

        var itemIds = dungeon.GatheringNodes
            .SelectMany(node => node.Loot)
            .Select(loot => loot.ItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

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
