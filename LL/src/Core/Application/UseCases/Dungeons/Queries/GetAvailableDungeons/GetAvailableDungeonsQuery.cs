using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Common.Exceptions;
using Domain.Models.LootTables;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetAvailableDungeons;

public record GetAvailableDungeonsQuery() : IQuery<List<DungeonPreviewDto>>;

public sealed class GetAvailableDungeonsQueryHandler : IRequestHandler<GetAvailableDungeonsQuery, List<DungeonPreviewDto>>
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly ILootTableRepository _lootTables;
    private readonly IMapper _mapper;

    public GetAvailableDungeonsQueryHandler(
        IDungeonDefinitions dungeonDefinitions,
        ILootTableRepository lootTables,
        IMapper mapper)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _lootTables = lootTables;
        _mapper = mapper;
    }

    public async Task<List<DungeonPreviewDto>> Handle(
        GetAvailableDungeonsQuery request,
        CancellationToken cancellationToken)
    {
        var previews = new List<DungeonPreviewDto>();

        foreach (var dungeon in _dungeonDefinitions.GetAll())
        {
            previews.Add(new DungeonPreviewDto
            {
                Id = dungeon.Id,
                Title = dungeon.Name,
                Tier = dungeon.Tier,
                Grade = FormatGrade(dungeon.Grade),
                RecommendedPowerScore = dungeon.RecommendedPowerScore,
                MinimumPowerScore = dungeon.MinimumPowerScore,
                RequiredPreviousDungeonId = dungeon.RequiredPreviousDungeonId,
                MinRooms = dungeon.MinRooms,
                MaxRooms = dungeon.MaxRooms,
                Rewards = await GetPossibleCompletionRewards(dungeon, cancellationToken)
            });
        }

        return previews;
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
