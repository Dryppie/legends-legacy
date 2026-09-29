using System.Text.Json;
using BalanceHarness;
using Domain.Models.Items.Equipments.Progression;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessEarnedPartyTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static (OfflineContent Content, TowerActivityInventory Inventory, TowerEarnedPoint Point) Example()
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var inventory=new TowerActivityInventory(Root,content);
        var reference=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian" && c.Gear=="common" && c.EssenceLevel==1).Character;
        var owned=new[] {inventory.QuestMace(reference.Id),inventory.QuestArmor(reference.Id,7)};
        var c=reference with {Equipment=inventory.Select(owned)};
        return (content,inventory,new("test","earned-progression","perfect","guardian",0,2160,2160,0,
            TowerJourneyProgression.Epoch.AddHours(6),c,owned));
    }
    [Fact]
    public void Ownership_and_prefix_selection_reject_foreign_and_unearned_equipment()
    {
        var (_,inventory,p)=Example();
        TowerEarnedPartyStudy.ValidateInventory(p.Character,p.Owned,inventory);
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateInventory(p.Character,p.Owned.Append(inventory.QuestMace(Guid.NewGuid())).ToArray(),inventory));
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateInventory(p.Character,p.Owned.Concat(p.Owned).ToArray(),inventory));
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateInventory(p.Character,p.Owned.Take(1).ToArray(),inventory));
    }
    [Fact]
    public void Checkpoint_extraction_cannot_borrow_terminal_rewards_or_advance_a_stopped_character()
    {
        var (content,inventory,p)=Example();
        var later=EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(),content.Equipment.Evaluator,p.Owned[1].State.DefinitionId,
            1,0,new(EquipmentAwardKind.RandomDiscovery,"test","later"),new(EquipmentOwnershipKind.UnboundPersonal,p.Character.Id)),content.Equipment.Evaluator);
        var history=JsonSerializer.SerializeToElement(new {
            summary=new {key="test",policy=p.Policy,outcome=p.Outcome,recipe=p.Recipe,path=0,encounters=86400,completed=true},
            checkpoints=new[] {new {encounter=2160,supplyItems=0,character=p.Character}},starting=p.Owned,
            windows=new[] {new {until=86400,equipment=new[] {later}}},steps=Array.Empty<object>()},HarnessJson.Options);
        var early=TowerEarnedPartyStudy.Extract(history,2160,inventory);
        Assert.Equal(p.Owned.Count,early.Owned.Count); Assert.DoesNotContain(early.Owned,e=>e.State.Id==later.State.Id);
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.Extract(history,86400,inventory));
    }
    [Fact]
    public async Task Production_preparation_preserves_personal_gear_and_ordered_essence_identities()
    {
        var (content,inventory,p)=Example();
        var members=Enumerable.Range(0,5).Select(i=> {
            var owner=Guid.NewGuid(); var owned=new[] {inventory.QuestMace(owner),inventory.QuestArmor(owner,i)};
            return p with {Character=p.Character with {Id=owner,Equipment=inventory.Select(owned)},Owned=owned};
        }).ToArray();
        var floor=TowerContentProviders.Floors(Path.Combine(Root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options).GetFloor(1)!;
        var party=new TowerEarnedParty("test",0,p.Policy,p.Outcome,2160,p.AvailableAt,floor,members);
        var prepared=await TowerEarnedPartyStudy.Prepare(Root,content,party,0,default);
        Assert.Equal(5,prepared.FriendlyParticipants.Count);
        Assert.Equal(members.Select(m=>m.Character.Id),prepared.FriendlyParticipants.Select(p=>p.Slot.SourceEntityId));
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateParty(party with {Members=Enumerable.Repeat(p,5).ToArray()}));
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateParty(party with {Members=members.Skip(1).ToArray()}));
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateParty(party with {StartsAt=p.AvailableAt.AddSeconds(-1)}));
        Assert.Throws<InvalidDataException>(()=>TowerEarnedPartyStudy.ValidateParty(party with {Members=members.Select((m,i)=>i==0?m with {Policy="fixed-progression"}:m).ToArray()}));
    }
    private sealed class QualificationFactAttribute:FactAttribute
    {
        public QualificationFactAttribute() {if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_EARNED_PARTY"))) Skip="Requires a frozen earned-party qualification owner.";}
    }
    [QualificationFact]
    public async Task Frozen_earned_party_qualification()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        await TowerEarnedPartyStudy.Qualify(HarnessJson.Read<TowerEntrySourceRequest>(Environment.GetEnvironmentVariable("LL_TOWER_EARNED_PARTY")!),deadline.Token);
    }
    private sealed class CombatFactAttribute:FactAttribute
    {
        public CombatFactAttribute() {if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_EARNED_COMBAT"))) Skip="Requires audited preparation and a frozen bounded earned-party combat owner.";}
    }
    [CombatFact]
    public async Task Frozen_earned_floor_one_diagnostic()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(9));
        await TowerEarnedPartyStudy.Combat(HarnessJson.Read<TowerEarnedCombatRequest>(Environment.GetEnvironmentVariable("LL_TOWER_EARNED_COMBAT")!),deadline.Token);
    }
}
