using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetAvailableDungeons;

public record GetAvailableDungeonsQuery(Guid CharacterId) : IQuery<List<DungeonPreviewDto>>;

public sealed class GetAvailableDungeonsQueryHandler : IRequestHandler<GetAvailableDungeonsQuery, List<DungeonPreviewDto>>
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonAccessPolicy _dungeonAccess;
    private readonly IDungeonPreviewRewardService _previewRewards;
    private readonly ICharacterService _characters;
    private readonly IDungeonRunService _dungeonRuns;
    private readonly IDungeonMasteryService _mastery;
    private readonly IItemBaseRepository _itemBases;
    private readonly IMapper _mapper;

    public GetAvailableDungeonsQueryHandler(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonAccessPolicy dungeonAccess,
        IDungeonPreviewRewardService previewRewards,
        ICharacterService characters,
        IDungeonRunService dungeonRuns,
        IDungeonMasteryService mastery,
        IItemBaseRepository itemBases,
        IMapper mapper)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonAccess = dungeonAccess;
        _previewRewards = previewRewards;
        _characters = characters;
        _dungeonRuns = dungeonRuns;
        _mastery = mastery;
        _itemBases = itemBases;
        _mapper = mapper;
    }

    public async Task<List<DungeonPreviewDto>> Handle(
        GetAvailableDungeonsQuery request,
        CancellationToken cancellationToken)
    {
        var previews = new List<DungeonPreviewDto>();
        var character = await _characters.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);
        var combatRating = character is null
            ? 0
            : Domain.Components.Attributes.CombatRatingCalculator.Calculate(character.BaseCombatAttributes, character.Level);

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

        foreach (var dungeon in dungeons)
        {
            records.TryGetValue(dungeon.Id, out var record);
            masteryByDungeon.TryGetValue(dungeon.Id, out var mastery);
            var access = await _dungeonAccess.EvaluateAsync(
                request.CharacterId,
                dungeon,
                combatRating,
                cancellationToken);

            previews.Add(new DungeonPreviewDto
            {
                Id = dungeon.Id,
                FamilyId = DungeonDefinitionIdentity.GetFamilyId(dungeon.Id),
                FamilyTitle = DungeonDefinitionIdentity.GetFamilyTitle(dungeon.Name),
                Title = dungeon.Name,
                Difficulty = FormatDifficulty(dungeon.Grade),
                Tier = dungeon.Tier,
                Grade = FormatGrade(dungeon.Grade),
                RecommendedCombatRating = dungeon.RecommendedCombatRating,
                CurrentCombatRating = access.CurrentCombatRating,
                CanEnter = access.CanEnter,
                MissingRequirements = [.. access.MissingRequirements],
                EntryRequirements = access.EntryRequirements
                    .Select(x => new DungeonEntryRequirementDto
                    {
                        ItemId = x.ItemId,
                        Name = x.Name,
                        RequiredAmount = x.RequiredAmount,
                        OwnedAmount = x.OwnedAmount
                    })
                    .ToList(),
                RequiredPreviousDungeonId = dungeon.RequiredPreviousDungeonId,
                MinRooms = dungeon.MinRooms,
                MaxRooms = dungeon.MaxRooms,
                Record = MapRecord(record),
                Mastery = MapMastery(mastery),
                Rewards = await MapRewardsAsync(dungeon, cancellationToken),
                GatheringNodes = await MapGatheringNodesAsync(dungeon, cancellationToken)
            });
        }

        return previews;
    }

    private async Task<List<DungeonPreviewRewardDto>> MapRewardsAsync(
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var rewards = await _previewRewards.GetPossibleCompletionRewardsAsync(
            dungeon,
            cancellationToken);

        return rewards
            .Select(x => new DungeonPreviewRewardDto
            {
                Id = x.ItemBase.Id,
                ItemBase = _mapper.Map<ItemBaseDto>(x.ItemBase),
                Category = x.Category,
                Source = x.Source,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                DropChancePercent = x.DropChancePercent,
                CanDropNothing = x.CanDropNothing,
                NoDropChancePercent = x.NoDropChancePercent
            })
            .ToList();
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

    private static DungeonRecordDto MapRecord(DungeonCompletionRecord? record)
    {
        if (record is null)
        {
            return new DungeonRecordDto();
        }

        return new DungeonRecordDto
        {
            HasCleared = true,
            FirstClearedAt = record.FirstCompletedAt,
            LastClearedAt = record.LastCompletedAt,
            TotalClears = record.CompletionCount
        };
    }

    private static DungeonMasteryDto MapMastery(DungeonMasterySnapshot? mastery)
    {
        if (mastery is null)
        {
            return new DungeonMasteryDto();
        }

        return new DungeonMasteryDto
        {
            Experience = mastery.Experience,
            Level = mastery.Level,
            ExperienceRequiredForNextLevel = mastery.ExperienceRequiredForNextLevel,
            CompletionCount = mastery.CompletionCount,
            Bonuses = mastery.Bonuses
                .Select(x => new DungeonMasteryBonusPreviewDto
                {
                    Id = x.Id,
                    RequiredLevel = x.RequiredLevel,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToList()
        };
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
