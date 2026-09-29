using System.Text.Json;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences;
using Services.LL.Dungeons;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessGrowthTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static (OfflineContent Content,FixtureCharacter Character) Reference()
    {
        var content = OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var p = TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json"));
        return (content,TowerBootstrapCohorts.Create(Root,Fixtures,p,content).First(c => c.Gear == "common" && c.EssenceLevel == 1).Character);
    }
    [Fact]
    public async Task Native_training_preserves_XP_remainders_and_excludes_unowned_fourth_essence()
    {
        var (content,character) = Reference(); var model = new TowerGrowthProgression(Root,content,character);
        await model.AwardIdle(83_799,default);
        Assert.Equal(30,model.Character.Level); Assert.Empty(model.LevelEvents);
        await model.AwardIdle(1,default);
        Assert.Equal(31,model.Character.Level); Assert.Equal(0,model.Character.Experience);
        Assert.Equal(740,model.Character.BaseAttributes.Single(a => a.AttributeType == AttributeType.MaxHealth).Value);
        Assert.All(model.State(0).Essences,e => Assert.Equal(83_800,e.CurrentXp));
        model.Attune(4); await model.AwardIdle(49_060,default);
        var state=model.State(0);
        Assert.All(state.Essences.Take(3),e => { Assert.Equal(2,e.Level); Assert.Equal(0,e.CurrentXp); });
        Assert.Equal(1,state.Essences[3].Level); Assert.Equal(49_060,state.Essences[3].CurrentXp);
        Assert.Equal(HarnessJson.Hash(character.Equipment),HarnessJson.Hash(model.Snapshot(character).Equipment));
        Assert.Throws<InvalidDataException>(() => model.Attune(3));
    }
    [Fact]
    public async Task Native_training_caps_without_funding_ascension_and_is_batch_invariant()
    {
        var (content,character)=Reference(); var a=new TowerGrowthProgression(Root,content,character); var b=new TowerGrowthProgression(Root,content,character);
        a.Attune(4); b.Attune(4); await a.AwardIdle(2_000_000,default);
        for(var i=0;i<20;i++) await b.AwardIdle(100_000,default);
        Assert.Equal(HarnessJson.Hash(a.State(0)),HarnessJson.Hash(b.State(0)));
        Assert.All(a.State(0).Essences,e => { Assert.Equal(10,e.Level); Assert.Equal(0,e.CurrentXp); Assert.Equal(0,e.AscensionTier); });
    }
    [Fact]
    public async Task Growing_prophecy_rewards_use_generated_level_and_apply_character_XP_only()
    {
        var (content,character)=Reference(); var growth=new TowerGrowthProgression(Root,content,character);
        var source=new TowerEntrySources(Root,character.Id,growth.Character,growth.Leveling);
        var day=new DateTimeOffset(2026,9,28,0,0,0,TimeSpan.Zero);
        var first=await source.Overview(day,default);
        var reward=first.DailyProphecies.Select(TowerProphecyOffers.Snapshot).ToArray();
        await growth.AwardIdle(1_000_000,default);
        var repeated=await source.Overview(day.AddHours(1),default);
        Assert.Equal(HarnessJson.Hash(reward),HarnessJson.Hash(repeated.DailyProphecies.Select(TowerProphecyOffers.Snapshot).ToArray()));
        var selected=first.DailyProphecies.First(); Assert.True(await source.AcceptGeneratedOffer(selected.Id,day.AddHours(1),default));
        // Exercise a generated reward snapshot even when this owner's offered objective is not combat-supported.
        selected.CurrentValue=selected.TargetValue; selected.Status=Domain.Models.Prophecies.ProphecyStatus.Completed;
        var before=growth.State(0); Assert.True(await source.Claim(selected,day.AddHours(2),default));
        Assert.Equal(HarnessJson.Hash(before.Essences),HarnessJson.Hash(growth.State(0).Essences));
        Assert.Equal(0,source.State().UnappliedCharacterExperience);
        Assert.Single(source.Claims); Assert.Equal(1,source.DuplicateClaimsRejected);
        var next=await source.Overview(day.AddDays(1),default);
        Assert.True(next.DailyProphecies.Select(TowerProphecyOffers.Snapshot).All(p => p.Reward.CharacterExperience > reward.Min(r => r.Reward.CharacterExperience)));
    }
    [Theory]
    [InlineData("Combat Readiness",10)]
    [InlineData("Attrition",15)]
    public async Task Mastery_reconstruction_counts_resolved_routes_and_excludes_lost_room(string failure,int expected)
    {
        var owner=Guid.Parse("420e993d-ebd4-4d06-ab34-1a818117008a");
        var recorded=new DungeonAcquisitionRun("mastery","goblin_mines",12,DungeonRunStatus.Failed,failure,0,1,new Dictionary<string,int>(),
            [new(1,RoomType.Combat,"choose_route",null,100,90,DungeonRunStatus.Active),new(2,RoomType.Combat,"choose_route",null,90,0,DungeonRunStatus.Failed)],[],[],
            JsonSerializer.SerializeToElement(new {rooms=new[] {new RoomInstance {RoomIndex=0,Type=RoomType.Entrance,Status=RoomInstanceStatus.Completed},new RoomInstance {RoomIndex=1,Type=RoomType.Combat},new RoomInstance {RoomIndex=2,Type=RoomType.Combat}}},HarnessJson.Options),0,0);
        var repository=new TowerGrowthStudy.MasteryRepository(); var service=new DungeonMasteryService(repository);
        var run=TowerGrowthStudy.Reconstruct(owner,recorded); var award=await service.AwardRunMasteryAsync(run,default);
        Assert.Equal(expected,award.ExperienceAwarded); Assert.Equal(0,award.CompletionCount);
        Assert.True((await service.AwardRunMasteryAsync(run,default)).AlreadyAwarded);
        Assert.Equal(0,(await service.GetMasteryByDungeonAsync(Guid.NewGuid(),["goblin_mines"],default))["goblin_mines"].Experience);
    }
    [Fact]
    public async Task Projection_prepares_retained_items_and_stops_before_any_dungeon_outcome()
    {
        var (content,character)=Reference();
        var history=JsonSerializer.SerializeToElement(new {
            summary=new { key="growth-test",questAt=10,outcome="four-of-five" },
            steps=new[] {new {before=character,encounter=8640}},
            windows=new[] {new {from=0,until=8640,area="region_01_area_06"}}
        },HarnessJson.Options);
        var result=JsonSerializer.SerializeToElement(await TowerGrowthStudy.Project(Root,content,history,default),HarnessJson.Options);
        Assert.True(result.GetProperty("state").GetProperty("level").GetInt32()>30);
        Assert.Equal(0,result.GetProperty("transferredDungeonOutcomes").GetInt32());
        Assert.Equal(4,result.GetProperty("state").GetProperty("essences").GetArrayLength());
        Assert.Equal(HarnessJson.Hash(character.Equipment),HarnessJson.Hash(result.GetProperty("candidate").Deserialize<FixtureCharacter>(HarnessJson.Options)!.Equipment));
    }
    private sealed class ProjectionFactAttribute : FactAttribute
    {
        public ProjectionFactAttribute() { if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_GROWTH_PROJECTION"))) Skip="Requires a frozen seed-free growth owner."; }
    }
    [ProjectionFact]
    public async Task Qualify_growth_without_transferring_changed_combat()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        await TowerGrowthStudy.Run(HarnessJson.Read<TowerEntrySourceRequest>(Environment.GetEnvironmentVariable("LL_TOWER_GROWTH_PROJECTION")!),deadline.Token);
    }
}
