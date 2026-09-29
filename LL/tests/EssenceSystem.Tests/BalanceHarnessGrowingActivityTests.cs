using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Layers.Rewards.Models;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessGrowingActivityTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static TowerJourneyProgression Journey(bool enabled)
    {
        var content = OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var plan = TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json"));
        var reference = TowerBootstrapCohorts.Create(Root,Fixtures,plan,content).First(c => c.Gear == "common" && c.EssenceLevel == 1).Character;
        return new(Root,content,reference,enabled);
    }
    [Theory]
    [InlineData(true,true)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(false,false)]
    public async Task Dungeon_XP_is_pending_until_success_claim_and_failure_keeps_only_mastery(bool enabled,bool complete)
    {
        var journey = Journey(enabled); await journey.Observe(default);
        var dungeon = new TowerJourneyDungeon(Root,journey);
        var run = new DungeonRun { Id = Guid.NewGuid(), CharacterId = journey.Growth.Character.Id, DungeonDefinitionId = "goblin_mines",
            Rooms = [new() { RoomIndex=0,Type=RoomType.Entrance,Status=RoomInstanceStatus.Completed },
                new() { RoomIndex=1,Type=RoomType.Combat,Status=RoomInstanceStatus.Completed }], CurrentRoomIndex=1 };
        var repository = TowerGrowthProgression.Boundary<IDungeonRunRepository>((m,_) => m.Name == "GetDungeonRunByDungeonIdAsync"
            ? Task.FromResult<DungeonRun?>(run) : throw new InvalidOperationException(m.Name));
        var enemy = new Creature { Id = Guid.NewGuid() };
        var result = new CombatResult { Outcome = BattleOutcome.Victory, Duration = 50 };
        await dungeon.Apply(run,new(run.Id,run.CharacterId,1,1,1,RoomType.Combat,null,new Dictionary<Domain.Models.Items.ItemType,double>(),
            [run.CharacterId],[new(Guid.NewGuid(),BattleOutcome.Victory,[enemy.Id],[enemy],result)]),repository,default);
        Assert.Equal(1000,run.PendingExperience); Assert.Equal(0,journey.Growth.Character.Experience);
        Assert.Equal(TowerJourneyProgression.Epoch.AddSeconds(5),journey.Now);
        run.Status = complete ? DungeonRunStatus.Completed : DungeonRunStatus.Failed;
        if (!complete)
        {
            run.State.FailureAnalysis = new() { PrimaryCause="Attrition",LostPendingLoot = new() { Experience=run.PendingExperience } };
            run.PendingExperience=0; run.PendingCinders=0; run.PendingSoulstones=0;
        }
        await dungeon.Finish(run,default);
        var receipt = dungeon.Receipt!;
        Assert.Equal(complete && enabled ? 1000 : 0,receipt.AppliedExperience);
        Assert.Equal(complete && !enabled ? 1000 : 0,receipt.WithheldExperience);
        Assert.Equal(complete ? 0 : 1000,receipt.LostExperience);
        Assert.Equal(complete && enabled ? 3000 : 0,receipt.EssenceExperience);
        Assert.Equal(complete ? 110 : 10,receipt.Mastery.ExperienceAwarded);
        Assert.Equal(receipt.AppliedExperience,journey.Growth.Character.Experience);
        await Assert.ThrowsAsync<InvalidDataException>(() => dungeon.Finish(run,default));
    }
    [Fact]
    public async Task Dungeon_time_advances_next_offer_day_without_crediting_idle_activity()
    {
        var journey=Journey(true);
        for(var i=0;i<8639;i++) await journey.Idle("region_01_area_04",false,10,default);
        Assert.Single(journey.Days); journey.AdvanceCombat(200);
        await journey.Observe(default);
        Assert.Equal(2,journey.Days.Count);
        Assert.Equal(TowerJourneyProgression.Epoch.AddDays(1).AddSeconds(10),journey.Now);
        Assert.Equal(86390,journey.IdleSeconds); Assert.Equal(0,journey.Growth.IdleExperience);
        Assert.Equal(0,journey.Sources.Claims.Count);
    }
    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute() { if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_GROWING_ACTIVITY"))) Skip="Requires a frozen bounded growing-activity owner."; }
    }
    [StudyFact]
    public async Task Frozen_growing_activity_comparison()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerActivityStudy.RunGrowingActivityAsync(HarnessJson.Read<TowerActivityRequest>(Environment.GetEnvironmentVariable("LL_TOWER_GROWING_ACTIVITY")!),deadline.Token);
    }
}
