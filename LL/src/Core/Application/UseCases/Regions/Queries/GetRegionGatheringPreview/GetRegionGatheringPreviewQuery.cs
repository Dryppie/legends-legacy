using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Rewards;
using Application.UseCases.Regions.Dtos;
using Domain.Models.Regions.Areas;
using Domain.Models.Rewards;
using MediatR;

namespace Application.UseCases.Regions.Queries.GetRegionGatheringPreview;

public sealed record GetRegionGatheringPreviewQuery(int RegionId)
    : IRequest<RegionGatheringPreviewDto>;

public sealed class GetRegionGatheringPreviewQueryHandler(
    IRegionService regions,
    IRewardTableDefinitionProvider rewardTables)
    : IRequestHandler<GetRegionGatheringPreviewQuery, RegionGatheringPreviewDto>
{
    public async Task<RegionGatheringPreviewDto> Handle(
        GetRegionGatheringPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var region = await regions.GetRegionByIdAsync(request.RegionId, cancellationToken);

        return new RegionGatheringPreviewDto
        {
            Areas = region.Areas
                .Select(area => new AreaGatheringPreviewDto
                {
                    Id = area.Id,
                    GatheringNodes = area.GatheringNodes
                        .Select(MapNode)
                        .ToList()
                })
                .ToList()
        };
    }

    private AreaGatheringNodePreviewDto MapNode(AreaGatheringNode node)
    {
        var quantity = ResolveSuccessfulQuantity(node);

        return new AreaGatheringNodePreviewDto
        {
            Id = node.Id,
            Name = node.Name,
            Type = node.Type.ToString(),
            LevelRequirement = node.LevelRequirement,
            ProcChance = node.ProcChance,
            YieldBonusPercent = node.YieldBonusPercent,
            MinQuantity = quantity?.Min,
            MaxQuantity = quantity?.Max
        };
    }

    private SuccessfulQuantityRange? ResolveSuccessfulQuantity(AreaGatheringNode node)
    {
        if (string.IsNullOrWhiteSpace(node.RewardTableId))
        {
            return null;
        }

        var table = rewardTables.FindById(node.RewardTableId);
        if (table is null)
        {
            return null;
        }

        var ranges = FindOrdinaryMaterialRanges(table, []).ToList();
        if (ranges.Count == 0)
        {
            return null;
        }

        var multiplier = AreaGatheringYieldBalance.ResolveMultiplier(node.YieldBonusPercent);
        return new SuccessfulQuantityRange(
            ranges.Min(range => ScaleQuantity(range.Min, multiplier)),
            ranges.Max(range => ScaleQuantity(range.Max, multiplier)));
    }

    private IEnumerable<RewardQuantityRange> FindOrdinaryMaterialRanges(
        RewardTableDefinition table,
        HashSet<string> visitedTableIds)
    {
        if (!visitedTableIds.Add(table.Id))
        {
            yield break;
        }

        foreach (var entry in table.Rolls.SelectMany(roll => roll.Entries))
        {
            if (entry.Type == RewardEntryType.Item &&
                !string.IsNullOrWhiteSpace(entry.ItemId) &&
                !entry.Tags.Contains("rare", StringComparer.OrdinalIgnoreCase) &&
                !entry.Tags.Contains("catalyst", StringComparer.OrdinalIgnoreCase))
            {
                yield return entry.Quantity;
                continue;
            }

            if (entry.Type != RewardEntryType.RewardTableReference ||
                string.IsNullOrWhiteSpace(entry.RewardTableId))
            {
                continue;
            }

            var nestedTable = rewardTables.FindById(entry.RewardTableId);
            if (nestedTable is null)
            {
                continue;
            }

            foreach (var range in FindOrdinaryMaterialRanges(nestedTable, visitedTableIds))
            {
                yield return range;
            }
        }
    }

    private static int ScaleQuantity(int quantity, double multiplier) =>
        Math.Max(0, (int)Math.Round(Math.Max(0, quantity) * Math.Max(0d, multiplier)));

    private sealed record SuccessfulQuantityRange(int Min, int Max);
}
