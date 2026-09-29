using System.Text.Json;
using BalanceHarness;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Services.LL.Items;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessNextEntryTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    [Theory]
    [InlineData(0,DungeonRunStatus.Completed,"item.tower_supply.v1.floor_04")]
    [InlineData(3,DungeonRunStatus.Completed,"item.tower_supply.v1.floor_04")]
    [InlineData(3,DungeonRunStatus.Failed,"item.tower_supply.v1.floor_04")]
    [InlineData(3,DungeonRunStatus.Completed,"item.tower_supply.v1.floor_01")]
    public async Task Disabled_legacy_supply_claim_retains_owned_gear_without_new_rewards(int cleared,DungeonRunStatus status,string ownedSupply)
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));var plan=TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json"));
        var recipe=TowerBootstrapCohorts.Create(Root,Fixtures,plan,content).First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1);
        var c=recipe.Character;var j=new TowerJourneyProgression(Root,content,c,true);var runtime=j.ExportRuntime();
        j=TowerJourneyProgression.RestoreRuntime(Root,content,c,runtime with {Sources=runtime.Sources with {ResourcesReconciled=true}});
        var catalog=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(Root,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        var owned=TowerContinuationSupply.Targets(content,catalog,ownedSupply,c.Id,recipe,plan.PurchaseOrder);
        var run=new DungeonAcquisitionRun(c.Name,"goblin_mines",123,status,null,status==DungeonRunStatus.Completed?1:0,1,
            new Dictionary<string,int>{{"sigil_goblin_mines",1}},[new(0,RoomType.RestSite,"rest",null,100,100,status)],[],[],
            JsonSerializer.SerializeToElement(new {rooms=new[] {new RoomInstance {RoomIndex=0,Type=RoomType.RestSite}}},HarnessJson.Options),0,0);
        var before=HarnessJson.Hash(owned);
        var receipt=await TowerContinuationSupply.Claim(Root,content,c,run,cleared,j.Sources,owned,recipe,plan.PurchaseOrder,default);
        Assert.False(receipt.Opened);Assert.Null(receipt.Equipment);Assert.Equal(before,HarnessJson.Hash(owned));
        Assert.Empty(receipt.Claimed);
        Assert.DoesNotContain(j.Sources.State().Items.Keys, item => item.StartsWith("item.tower_supply.", StringComparison.Ordinal));
    }
    [Fact]
    public void Paid_entry_is_atomic_and_cannot_reuse_spent_stock()
    {
        var owner=Guid.NewGuid();var source=new TowerEntrySources(Root,owner,new Domain.Models.Entities.Characters.Character {Id=owner,UserId=owner,Level=30},
            TowerGrowthProgression.Boundary<Services.LL.Interfaces.ILevelingService>());
        var runtime=source.ExportRuntime();source.RestoreRuntime(runtime with {ResourcesReconciled=true,
            State=runtime.State with {Items=new Dictionary<string,int>{{"sigil_goblin_mines",1}}}});
        source.SpendEntry(new Dictionary<string,int>{{"sigil_goblin_mines",1}});Assert.Equal(0,source.State().Items["sigil_goblin_mines"]);
        var before=HarnessJson.Hash(source.ExportRuntime());
        Assert.Throws<InvalidDataException>(()=>source.SpendEntry(new Dictionary<string,int>{{"sigil_goblin_mines",1}}));
        Assert.Equal(before,HarnessJson.Hash(source.ExportRuntime()));
    }
    private sealed class NextEntryFactAttribute:FactAttribute
    {
        public NextEntryFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_NEXT_ENTRY")))Skip="Requires audited runtime and frozen funded next-entry owner.";}
    }
    [NextEntryFact]
    public async Task Frozen_funded_next_entries()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(840));
        await TowerNextEntryStudy.Run(HarnessJson.Read<TowerNextEntryRequest>(Environment.GetEnvironmentVariable("LL_TOWER_NEXT_ENTRY")!),deadline.Token);
    }
}
