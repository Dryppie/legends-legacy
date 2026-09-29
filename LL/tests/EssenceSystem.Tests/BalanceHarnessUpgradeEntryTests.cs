using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessUpgradeEntryTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static (OfflineContent Content,TowerEarnedPoint Point,JsonElement History) Example()
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var at=TowerJourneyProgression.Epoch.AddHours(72);
        var owned=c.Equipment.Select(e=>e.Data).ToArray();
        var point=new TowerEarnedPoint("unit","earned-progression","perfect","guardian",0,25920,25920,0,at,c,owned);
        var state=new {at,growth=new {level=c.Level,experience=123},source=new {owner=c.Id,items=new Dictionary<string,int> { ["sigil_goblin_mines"]=2,["sigil_fragment"]=4 }}};
        var history=JsonSerializer.SerializeToElement(new {
            summary=new {key="unit",outcome="perfect",questAt=1},starting=owned,windows=new object[0],
            checkpoints=new[] {new {encounter=25920,character=c,sigils=new Dictionary<string,int> { ["sigil_goblin_mines"]=0,["sigil_forgotten_catacombs"]=1 }}},
            steps=Enumerable.Range(0,3).Select(i=>new {ordinal=i,encounter=25920,dungeon="goblin_mines",award=(object?)null,dungeonLoot=new {equipment=new object[0],blueprints=new Dictionary<string,int>()}}).ToArray(),
            progression=new {checkpoints=new[] {new {encounter=25920,assembled=2,state}},entries=Enumerable.Range(0,3).Select(i=>new {ordinal=i,after=state}).ToArray(),
                claims=new object[0],days=new object[0],dungeonEvents=new object[0]}
        },HarnessJson.Options);
        return(content,point,history);
    }
    [Fact]
    public void Source_grant_receipts_cannot_restore_spent_sigils()
    {
        var (_,point,history)=Example();var checkpoint=TowerUpgradeEntryStudy.Restore(point,history);
        Assert.Equal(0,checkpoint.Inventory["sigil_goblin_mines"]);
        Assert.Equal(1,checkpoint.Inventory["sigil_forgotten_catacombs"]);
        Assert.Equal(2,checkpoint.State.GetProperty("source").GetProperty("items").GetProperty("sigil_goblin_mines").GetInt32());
        Assert.Equal(new TowerUpgradeStock("sigil_goblin_mines",1,0,2,3,0),checkpoint.SigilReconciliation[0]);
    }
    [Fact]
    public void Offers_at_terminal_idle_clock_belong_to_next_activity()
    {
        var (_,point,history)=Example();
        var node=System.Text.Json.Nodes.JsonNode.Parse(history.GetRawText())!;
        node["progression"]!["days"]=new System.Text.Json.Nodes.JsonArray(
            JsonSerializer.SerializeToNode(new {at=point.AvailableAt.AddSeconds(-10),selected="past"},HarnessJson.Options),
            JsonSerializer.SerializeToNode(new {at=point.AvailableAt,selected="future"},HarnessJson.Options));
        var restored=TowerUpgradeEntryStudy.Restore(point,JsonSerializer.SerializeToElement(node));
        Assert.Equal("past",Assert.Single(restored.Days).GetProperty("selected").GetString());
    }
    [Fact]
    public void Invented_inventory_and_future_checkpoint_are_rejected()
    {
        var (_,point,history)=Example();
        Assert.Throws<InvalidDataException>(()=>TowerUpgradeEntryStudy.Restore(point with {Owned=point.Owned.Concat(point.Owned.Take(1)).ToArray()},history));
        Assert.Throws<InvalidDataException>(()=>TowerUpgradeEntryStudy.Restore(point with {AvailableAt=point.AvailableAt.AddSeconds(1)},history));
        Assert.Throws<InvalidDataException>(()=>TowerUpgradeEntryStudy.Restore(point with {Encounter=86400},history));
    }
    [Fact]
    public async Task Next_entry_uses_remaining_stock_after_seven_awards_without_spending_or_changing_gear()
    {
        var (content,point,history)=Example();var checkpoint=TowerUpgradeEntryStudy.Restore(point,history);
        checkpoint=checkpoint with {Point=point with {SupplyItems=7}};
        var before=HarnessJson.Hash(checkpoint);
        var entry=JsonSerializer.SerializeToElement(await TowerUpgradeEntryStudy.Entry(Root,content,checkpoint,3,point.AvailableAt.AddMinutes(5),default),HarnessJson.Options);
        Assert.Equal("Enter",entry.GetProperty("reason").GetString());
        Assert.Equal("forgotten_catacombs",entry.GetProperty("selectedDungeon").GetString());
        Assert.Equal("item.tower_supply.v1.floor_04",entry.GetProperty("prospectiveSupply").GetProperty("itemBaseId").GetString());
        Assert.Equal(0,entry.GetProperty("inventoryAfterHypotheticalEntry").GetProperty("sigil_forgotten_catacombs").GetInt32());
        Assert.Equal(before,HarnessJson.Hash(checkpoint));Assert.Equal(0,entry.GetProperty("actualEntries").GetInt32());
    }
    [Fact]
    public async Task Incomplete_coverage_and_empty_stock_remain_distinct_gates()
    {
        var (content,point,history)=Example();var checkpoint=TowerUpgradeEntryStudy.Restore(point,history);
        var sparse=checkpoint with {Point=point with {Character=point.Character with {Equipment=[]}}};
        var coverage=JsonSerializer.SerializeToElement(await TowerUpgradeEntryStudy.Entry(Root,content,sparse,3,point.AvailableAt,default),HarnessJson.Options);
        var empty=checkpoint with {Inventory=new Dictionary<string,int>()};
        var noSigil=JsonSerializer.SerializeToElement(await TowerUpgradeEntryStudy.Entry(Root,content,empty,0,point.AvailableAt,default),HarnessJson.Options);
        Assert.Equal("EquipmentCoverage",coverage.GetProperty("reason").GetString());
        Assert.Equal("NoSigil",noSigil.GetProperty("reason").GetString());
        Assert.Equal("item.tower_supply.v1.floor_01",noSigil.GetProperty("prospectiveSupply").GetProperty("itemBaseId").GetString());
    }
    private sealed class QualificationFactAttribute:FactAttribute
    {
        public QualificationFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_UPGRADE_ENTRY")))Skip="Requires a frozen seed-free upgrade-entry owner.";}
    }
    [QualificationFact]
    public async Task Frozen_upgrade_entry_qualification()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        await TowerUpgradeEntryStudy.Run(HarnessJson.Read<TowerUpgradeEntryRequest>(Environment.GetEnvironmentVariable("LL_TOWER_UPGRADE_ENTRY")!),deadline.Token);
    }
}
