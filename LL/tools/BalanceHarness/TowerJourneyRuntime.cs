using System.Text.Json;
using Application.Interfaces.Services.LL.Prophecies;
using Common.Randomness;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Attributes;
using Domain.Models.Prophecies;

namespace BalanceHarness;

public sealed record TowerSourceRuntime(TowerEntrySourceState State, PlayerProphecyInstance[] Instances,
    WeeklyRevelationProgress[] Weeks, DailyProphecyRerollState[] Rerolls, TowerEntrySourceClaim[] Claims,
    int DuplicateClaimsRejected, bool ResourcesReconciled);
public sealed record TowerGrowthRuntime(Guid Owner, TowerGrowthState State, TowerGrowingEssence[] OwnedEssences,
    int Attuned, int LastEssenceExperience, long DungeonExperience, EntityAttribute[] BaseAttributes,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    TowerEssenceAcquisition[]? Acquisitions = null);
public sealed record TowerJourneyRuntime(TowerGrowthRuntime Growth, TowerSourceRuntime Sources,
    CharacterDungeonMastery[] Mastery, TowerJourneyDay[] Days, TowerJourneyEvent[] DungeonEvents,
    long IdleSeconds, long CombatTicks, long WaitingTicks, DateTime? ObservedDay, PropheciesOverview? Overview);

/// <summary>Detached continuation snapshots. Database-only GUIDs/timestamps are normalized, never gameplay state.</summary>
public static class TowerRuntimeCopy
{
    public static T Of<T>(T value) => JsonSerializer.SerializeToElement(value,HarnessJson.Options).Deserialize<T>(HarnessJson.Options)!;
    public static void Equal(object expected, object actual, string context)
    {
        if(HarnessJson.Hash(expected)!=HarnessJson.Hash(actual)) throw new InvalidDataException("Continuation mismatch: "+context);
    }
}

public sealed partial class TowerEntrySources
{
    public bool ResourcesReconciled { get; private set; }
    public TowerSourceRuntime ExportRuntime()
    {
        var result=TowerRuntimeCopy.Of(new TowerSourceRuntime(State(),instances.OrderBy(i=>i.PeriodStart).ThenBy(i=>i.SlotType).ToArray(),
            weeks.Values.OrderBy(w=>w.PeriodStart).ToArray(),rerolls.Values.OrderBy(r=>r.PeriodStart).ToArray(),Claims.ToArray(),DuplicateClaimsRejected,ResourcesReconciled));
        foreach(var w in result.Weeks) w.Id=StableRandom.Guid("tower-week-row-v1",character.Id.ToString(),w.PeriodStart.ToString("O"));
        foreach(var r in result.Rerolls) r.Id=StableRandom.Guid("tower-reroll-row-v1",character.Id.ToString(),r.PeriodStart.ToString("O"));
        return result;
    }
    public void RestoreRuntime(TowerSourceRuntime snapshot)
    {
        if(instances.Count!=0||weeks.Count!=0||rerolls.Count!=0||items.Count!=0||Claims.Count!=0)
            throw new InvalidDataException("Restore requires a fresh personal source boundary.");
        var s=TowerRuntimeCopy.Of(snapshot);
        if(s.State.Owner!=character.Id||s.State.UnappliedCharacterExperience!=0||!appliesExperience
            ||s.State.Items.Any(p=>p.Value<0)||s.Instances.Select(i=>i.Id).Distinct().Count()!=s.Instances.Length
            ||s.DuplicateClaimsRejected!=s.Claims.Length)
            throw new InvalidDataException("Invalid personal source snapshot.");
        foreach(var i in s.Instances)
        {
            if(i.PlayerId!=character.Id||i.CharacterId!=character.Id||i.PeriodEnd<=i.PeriodStart
                ||i.CurrentValue<0||i.CurrentValue>i.TargetValue) throw new InvalidDataException("Invalid prophecy instance.");
            var d=definitions.Single(d=>d.Id==i.ProphecyDefinitionId);
            TowerRuntimeCopy.Equal(d,i.ProphecyDefinition!,"prophecy definition"); i.ProphecyDefinition=d;
        }
        if(s.Weeks.Any(w=>w.CharacterId!=character.Id||w.PlayerId!=character.Id)
            ||s.Rerolls.Any(r=>r.CharacterId!=character.Id||r.PlayerId!=character.Id)) throw new InvalidDataException("Cross-owner source runtime.");
        instances.AddRange(s.Instances);
        foreach(var w in s.Weeks) weeks.Add(w.PeriodStart,w);
        foreach(var r in s.Rerolls) rerolls.Add(r.PeriodStart,r);
        foreach(var p in s.State.Items) items.Add(p.Key,p.Value);
        character.Cinders=s.State.Cinders;character.Soulstones=s.State.Soulstones;character.FateEcho=s.State.FateEcho;
        Claims.AddRange(s.Claims);DuplicateClaimsRejected=s.DuplicateClaimsRejected;ResourcesReconciled=s.ResourcesReconciled;
        TowerRuntimeCopy.Equal(snapshot,ExportRuntime(),"source round trip");
    }
    public PropheciesOverview BindOverview(PropheciesOverview value) => value with {
        DailyProphecies=value.DailyProphecies.Select(i=>instances.Single(p=>p.Id==i.Id)).ToArray(),
        ActiveDailyProphecy=value.ActiveDailyProphecy is null?null:instances.Single(p=>p.Id==value.ActiveDailyProphecy.Id),
        GreaterProphecy=instances.Single(p=>p.Id==value.GreaterProphecy.Id),
        WeeklyRevelation=weeks[value.WeeklyRevelation.PeriodStart]
    };
    public void ReconcileResources(TowerUpgradeCheckpoint checkpoint)
    {
        if(ResourcesReconciled||checkpoint.Point.Character.Id!=character.Id||checkpoint.Inventory.Any(p=>p.Value<0))
            throw new InvalidDataException("Invalid or repeated resource reconciliation.");
        TowerRuntimeCopy.Equal(checkpoint.State.GetProperty("source"),State(),"source grant witness before reconciliation");
        items.Clear();foreach(var p in checkpoint.Inventory)items.Add(p.Key,p.Value);ResourcesReconciled=true;
    }
    public void SpendEntry(IReadOnlyDictionary<string,int> costs)
    {
        if(!ResourcesReconciled||costs.Count!=1||costs.Any(p=>p.Value!=1||p.Key is not ("sigil_goblin_mines" or "sigil_forgotten_catacombs")))
            throw new InvalidDataException("Unqualified entry costs.");
        if(costs.Any(p=>items.GetValueOrDefault(p.Key)<p.Value))throw new InvalidDataException("Unfunded entry.");
        foreach(var p in costs)items[p.Key]-=p.Value;
    }
    public void AddClaimedItems(IReadOnlyDictionary<string,int> awarded)
    {
        if(!ResourcesReconciled||awarded.Any(p=>p.Value<=0))throw new InvalidDataException("Invalid continuation item claim.");
        foreach(var p in awarded)items[p.Key]=checked(items.GetValueOrDefault(p.Key)+p.Value);
    }
    public void OpenSupply(string item)
    {
        if(!ResourcesReconciled||!item.StartsWith("item.tower_supply.v1.floor_",StringComparison.Ordinal)||items.GetValueOrDefault(item)<1)
            throw new InvalidDataException("Unowned supply opening.");
        items[item]--;
    }
    public async Task<int> VerifyClaimGuards(CancellationToken ct)
    {
        var before=HarnessJson.Hash(ExportRuntime());var count=0;
        foreach(var i in instances.Where(i=>i.Status==ProphecyStatus.Claimed))
        {
            if((await prophecies.ClaimAsync(character.Id,character.Id,i.Id,i.ClaimedAt!.Value,ct)).Succeeded)
                throw new InvalidDataException("Restored prophecy claimed twice.");
            count++;
        }
        foreach(var w in weeks.Values)
        foreach(var (favor,claimed) in new[] {(3,w.Milestone3Claimed),(5,w.Milestone5Claimed),(7,w.Milestone7Claimed)})
            if(claimed)
            {
                if((await prophecies.ClaimWeeklyMilestoneAsync(character.Id,character.Id,favor,w.PeriodStart,ct)).Succeeded)
                    throw new InvalidDataException("Restored milestone claimed twice.");
                count++;
            }
        if(before!=HarnessJson.Hash(ExportRuntime()))throw new InvalidDataException("Claim retry mutated restored runtime.");
        return count;
    }
}

public sealed partial class TowerGrowthProgression
{
    public TowerGrowthRuntime ExportRuntime(long prophecyExperience) => TowerRuntimeCopy.Of(new TowerGrowthRuntime(Character.Id,State(prophecyExperience),
        essences.Select(e=>new TowerGrowingEssence(e.Id,e.EssenceDefinitionId,e.Level,e.CurrentXp,e.AscensionTier)).ToArray(),
        attuned,LastEssenceExperience,DungeonExperience,Character.BaseAttributes.ToArray(),acquisitions.Count==0?null:acquisitions.ToArray()));
    public void RestoreRuntime(TowerGrowthRuntime snapshot)
    {
        var s=TowerRuntimeCopy.Of(snapshot);
        if(Character.Level!=30||IdleExperience!=0||DungeonExperience!=0||s.Owner!=Character.Id||s.State.Level<30
            ||s.State.Experience<0||s.State.IdleExperience<0||s.DungeonExperience<0
            ||s.OwnedEssences.Length!=4+(s.Acquisitions?.Length??0)||s.Acquisitions?.Length is >1)
            throw new InvalidDataException("Invalid growth snapshot or nonempty destination.");
        Character.Level=s.State.Level;
        foreach(var receipt in s.Acquisitions??[])
        {
            var saved=s.OwnedEssences[essences.Count];
            RetainAcquisition(receipt,new Domain.Models.Essences.PlayerEssence {Id=saved.Id,CharacterId=Character.Id,
                EssenceDefinitionId=saved.Definition,Level=1},attune:false);
        }
        for(var i=0;i<essences.Count;i++)
        {
            var e=essences[i];var saved=s.OwnedEssences[i];
            if(saved.Id!=e.Id||saved.Definition!=e.EssenceDefinitionId||saved.AscensionTier!=0||saved.Level is <1 or >10||saved.CurrentXp<0)
                throw new InvalidDataException("Unmodeled Essence ownership/training.");
            e.Level=saved.Level;e.CurrentXp=saved.CurrentXp;e.AscensionTier=saved.AscensionTier;
        }
        Character.Level=s.State.Level;Character.Experience=s.State.Experience;Character.BaseAttributes=s.BaseAttributes;
        IdleExperience=s.State.IdleExperience;DungeonExperience=s.DungeonExperience;LastEssenceExperience=s.LastEssenceExperience;Attune(s.Attuned);
        TowerRuntimeCopy.Equal(snapshot,ExportRuntime(s.State.ProphecyExperience),"growth round trip");
    }
}

public sealed partial class TowerJourneyProgression
{
    public long WaitingTicks { get; private set; }
    public void WaitUntil(DateTimeOffset at)
    {
        if(at<Now)throw new InvalidDataException("Cannot rewind continuation clock.");
        WaitingTicks=checked(WaitingTicks+(at-Now).Ticks);
    }
    public TowerJourneyRuntime ExportRuntime()
    {
        if(!Enabled)throw new InvalidDataException("Continuation requires earned progression.");
        var source=Sources.ExportRuntime();var mastery=TowerRuntimeCopy.Of(MasteryRepository.Rows.OrderBy(r=>r.DungeonDefinitionId).ToArray());
        foreach(var m in mastery){m.CreatedAt=Epoch;m.UpdatedAt=Epoch;}
        var bound=overview is null?null:TowerRuntimeCopy.Of(overview) with {WeeklyRevelation=source.Weeks.Single(w=>w.PeriodStart==overview.WeeklyRevelation.PeriodStart)};
        return TowerRuntimeCopy.Of(new TowerJourneyRuntime(Growth.ExportRuntime(Sources.Claims.Sum(c=>(long)c.Reward.CharacterExperience)),source,
            mastery,Days.ToArray(),DungeonEvents.ToArray(),IdleSeconds,CombatTicks,WaitingTicks,observedDay,bound));
    }
    public static TowerJourneyProgression RestoreRuntime(string root,OfflineContent content,FixtureCharacter origin,TowerJourneyRuntime snapshot)
    {
        var s=TowerRuntimeCopy.Of(snapshot);
        if(s.IdleSeconds<0||s.CombatTicks<0||s.WaitingTicks<0||s.Mastery.Any(m=>m.CharacterId!=origin.Id))
            throw new InvalidDataException("Invalid continuation clock/owner.");
        var j=new TowerJourneyProgression(root,content,origin,true);
        j.Growth.RestoreRuntime(s.Growth);j.Sources.RestoreRuntime(s.Sources);j.MasteryRepository.Rows.AddRange(s.Mastery);
        j.Days.AddRange(s.Days);j.DungeonEvents.AddRange(s.DungeonEvents);j.IdleSeconds=s.IdleSeconds;j.CombatTicks=s.CombatTicks;j.WaitingTicks=s.WaitingTicks;
        if(s.Growth.Acquisitions?.Any(a=>a.At>j.Now)==true)throw new InvalidDataException("Future Essence acquisition.");
        j.observedDay=s.ObservedDay;j.overview=s.Overview is null?null:j.Sources.BindOverview(s.Overview);
        TowerRuntimeCopy.Equal(snapshot,j.ExportRuntime(),"journey round trip");return j;
    }
}
