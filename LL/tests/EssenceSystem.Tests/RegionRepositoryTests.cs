using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Regions;

namespace EssenceSystem.Tests;

public sealed class RegionRepositoryTests
{
    [Fact]
    public async Task GetRegionByIdAsync_LoadsGatheringNodesForAreaPreviews()
    {
        await using var db = CreateDb();
        db.Regions.Add(new Region
        {
            Id = 1,
            Name = "Shenic",
            Areas =
            [
                new Area
                {
                    Id = "region_01_area_01",
                    Name = "Lumo Ruins",
                    DifficultyTier = 1,
                    GatheringNodes =
                    [
                        new AreaGatheringNode
                        {
                            Id = "lumo_ruins_ore_vein",
                            Name = "Ore Vein",
                            Type = GatheringType.Mining,
                            ProcChance = 0.0037f,
                            YieldBonusPercent = 50
                        }
                    ]
                }
            ]
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var region = await new RegionRepository(db)
            .GetRegionByIdAsync(1, CancellationToken.None);

        var node = Assert.Single(Assert.Single(region.Areas).GatheringNodes);
        Assert.Equal("lumo_ruins_ore_vein", node.Id);
        Assert.Equal(0.0037f, node.ProcChance);
        Assert.Equal(50d, node.YieldBonusPercent);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }
}
