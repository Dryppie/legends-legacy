using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Rewards;
using Application.UseCases.Regions.Queries.GetRegionGatheringPreview;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Rewards;

namespace EssenceSystem.Tests;

public sealed class RegionGatheringPreviewQueryTests
{
    [Fact]
    public async Task Handle_ReportsSuccessfulMaterialQuantityWithAreaAbundance()
    {
        var region = new Region
        {
            Id = 1,
            Name = "Shenic",
            Areas =
            [
                new Area
                {
                    Id = "region_01_area_01",
                    GatheringNodes =
                    [
                        new AreaGatheringNode
                        {
                            Id = "lumo_ruins_ore_vein",
                            Name = "Ore Vein",
                            Type = GatheringType.Mining,
                            ProcChance = 0.0037f,
                            YieldBonusPercent = 50,
                            RewardTableId = "reward.gathering.ore"
                        }
                    ]
                }
            ]
        };
        var rewardTables = new StaticRewardTableProvider(
            new RewardTableDefinition
            {
                Id = "reward.gathering.ore",
                Rolls =
                [
                    new RewardRollDefinition
                    {
                        Type = RewardRollType.Weighted,
                        Entries =
                        [
                            new RewardEntryDefinition
                            {
                                Type = RewardEntryType.Item,
                                ItemId = "ore",
                                Quantity = new RewardQuantityRange { Min = 8, Max = 24 }
                            },
                            new RewardEntryDefinition
                            {
                                Type = RewardEntryType.RewardTableReference,
                                RewardTableId = "reward.gathering.catalysts"
                            }
                        ]
                    }
                ]
            },
            new RewardTableDefinition
            {
                Id = "reward.gathering.catalysts",
                Rolls =
                [
                    new RewardRollDefinition
                    {
                        Type = RewardRollType.WeightedWithNoDrop,
                        Entries =
                        [
                            new RewardEntryDefinition
                            {
                                Type = RewardEntryType.Item,
                                ItemId = "fury_heart",
                                Tags = ["rare", "catalyst"]
                            }
                        ]
                    }
                ]
            });
        var handler = new GetRegionGatheringPreviewQueryHandler(
            new StaticRegionService(region),
            rewardTables);

        var preview = await handler.Handle(
            new GetRegionGatheringPreviewQuery(1),
            CancellationToken.None);

        var node = Assert.Single(Assert.Single(preview.Areas).GatheringNodes);
        Assert.Equal(0.0037f, node.ProcChance);
        Assert.Equal(8, node.MinQuantity);
        Assert.Equal(24, node.MaxQuantity);
        Assert.Equal(50d, node.YieldBonusPercent);
    }

    private sealed class StaticRegionService(Region region) : IRegionService
    {
        public Task<Region> GetRegionByIdAsync(
            int regionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(region);
    }

    private sealed class StaticRewardTableProvider(params RewardTableDefinition[] tables)
        : IRewardTableDefinitionProvider
    {
        public RewardTableDefinition GetById(string id) =>
            FindById(id) ?? throw new KeyNotFoundException(id);

        public RewardTableDefinition? FindById(string id) =>
            tables.FirstOrDefault(table =>
                table.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<RewardTableDefinition> GetAll() => tables;
    }
}
