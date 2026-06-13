using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Common.Exceptions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.LootTables;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetAvailableDungeons;

public record GetAvailableDungeonsQuery(Guid CharacterId) : IQuery<List<DungeonPreviewDto>>;

public sealed class GetAvailableDungeonsQueryHandler : IRequestHandler<GetAvailableDungeonsQuery, List<DungeonPreviewDto>>
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly ILootTableRepository _lootTables;
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly IItemBaseRepository _itemBases;
    private readonly ICharacterService _characters;
    private readonly IMapper _mapper;

    public GetAvailableDungeonsQueryHandler(
        IDungeonDefinitions dungeonDefinitions,
        ILootTableRepository lootTables,
        IDungeonRunRepository dungeonRuns,
        IItemBaseRepository itemBases,
        ICharacterService characters,
        IMapper mapper)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _lootTables = lootTables;
        _dungeonRuns = dungeonRuns;
        _itemBases = itemBases;
        _characters = characters;
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

        foreach (var dungeon in _dungeonDefinitions.GetAll())
        {
            var missingRequirements = await GetMissingRequirements(
                request.CharacterId,
                dungeon,
                combatRating,
                cancellationToken);

            previews.Add(new DungeonPreviewDto
            {
                Id = dungeon.Id,
                Title = dungeon.Name,
                Tier = dungeon.Tier,
                Grade = FormatGrade(dungeon.Grade),
                RecommendedCombatRating = dungeon.RecommendedCombatRating,
                MinimumCombatRating = dungeon.MinimumCombatRating,
                CurrentCombatRating = combatRating,
                CanEnter = missingRequirements.Count == 0,
                MissingRequirements = missingRequirements,
                RequiredPreviousDungeonId = dungeon.RequiredPreviousDungeonId,
                MinRooms = dungeon.MinRooms,
                MaxRooms = dungeon.MaxRooms,
                Rewards = await GetPossibleCompletionRewards(dungeon, cancellationToken)
            });
        }

        return previews;
    }

    private async Task<List<string>> GetMissingRequirements(
        Guid characterId,
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        int combatRating,
        CancellationToken cancellationToken)
    {
        var missingRequirements = new List<string>();

        if (combatRating < dungeon.MinimumCombatRating)
        {
            missingRequirements.Add($"Requires {dungeon.MinimumCombatRating} Combat Rating.");
        }

        if (!string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
            && !await _dungeonRuns.HasCompletedDungeonAsync(
                characterId,
                dungeon.RequiredPreviousDungeonId,
                cancellationToken))
        {
            missingRequirements.Add("Complete the previous difficulty first.");
        }

        return missingRequirements;
    }

    private async Task<List<DungeonPreviewRewardDto>> GetPossibleCompletionRewards(
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var rewards = new List<DungeonPreviewRewardDto>();

        if (dungeon.CompletionLootTableId.HasValue)
        {
            var completionTable = await TryGetLootTableAsync(
                dungeon.CompletionLootTableId.Value,
                cancellationToken);

            if (completionTable is not null)
                rewards.AddRange(MapRewards(completionTable, "Dungeon Completion"));
        }

        if (dungeon.TierLootTableId.HasValue)
        {
            var tierTable = await TryGetLootTableAsync(
                dungeon.TierLootTableId.Value,
                cancellationToken);

            if (tierTable is not null)
                rewards.AddRange(MapRewards(tierTable, $"Tier {dungeon.Tier} Completion"));
        }

        rewards.AddRange(await MapFirstCompletionRewards(dungeon, cancellationToken));

        return rewards
            .GroupBy(x => x.ItemBase.Id)
            .Select(x =>
            {
                var firstReward = x.First();
                firstReward.Source = string.Join(", ", x.Select(reward => reward.Source).Distinct());

                return firstReward;
            })
            .ToList();
    }

    private IEnumerable<DungeonPreviewRewardDto> MapRewards(LootTable lootTable, string source)
    {
        foreach (var item in FlattenItems(lootTable))
        {
            yield return new DungeonPreviewRewardDto
            {
                Id = item.Item.Id,
                ItemBase = _mapper.Map<ItemBaseDto>(item.Item),
                Source = source
            };
        }
    }

    private async Task<IEnumerable<DungeonPreviewRewardDto>> MapFirstCompletionRewards(
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var grants = dungeon.RewardTable.FirstClearRewards;
        if (grants.Count == 0)
        {
            return [];
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            grants.Select(x => x.ItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
            cancellationToken);

        return grants
            .Where(x => itemBases.ContainsKey(x.ItemId))
            .Select(x => new DungeonPreviewRewardDto
            {
                Id = x.ItemId,
                ItemBase = _mapper.Map<ItemBaseDto>(itemBases[x.ItemId]),
                Source = "First Completion"
            })
            .ToList();
    }

    private async Task<LootTable?> TryGetLootTableAsync(Guid lootTableId, CancellationToken cancellationToken)
    {
        try
        {
            return await _lootTables.GetLootTableByIdAsync(lootTableId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static IEnumerable<LootTableItem> FlattenItems(LootTable lootTable)
    {
        foreach (var entry in lootTable.Entries)
        {
            if (entry is LootTableItem { Item: not null } item)
            {
                yield return item;
                continue;
            }

            if (entry is LootTable nestedTable)
            {
                foreach (var nestedItem in FlattenItems(nestedTable))
                {
                    yield return nestedItem;
                }
            }
        }
    }

    private static string FormatGrade(Domain.Models.Dungeons.Definitions.DungeonGrade grade) =>
        grade switch
        {
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeII => "Grade II",
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeIII => "Grade III",
            _ => "Grade I"
        };
}
