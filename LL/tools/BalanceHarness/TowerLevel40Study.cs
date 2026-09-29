using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Items.Equipments.Progression;

namespace BalanceHarness;

public sealed record TowerLevel40Request(string ApiRoot,string Fixtures,string ExpansionArchive,string PreparationArchive,
    string ReturnArchive,string Output,IReadOnlyDictionary<string,string> InputHashes);
public sealed record TowerLevel40Growth(TowerReturnOwner Owner,int From,int Until,int Victories,int ExperiencePerVictory,
    int AssembledSigils,int ClaimGuards,TowerActivityRewards Rewards,JsonElement[] Changes,JsonElement Source);

/// <summary>Seed-free, costed forward activity. Quest feasibility probes never become earned ownership.</summary>
public static class TowerLevel40Study
{
    public const string Version="tower-old-forest-growth-v2";
    public const string Area="region_01_area_08";
    public const string ExpansionPin="81306b47b1aa870356dcdb43cc0fb7266449a031e2397a1debf781f19467a722";
    public const string PreparationPin="b756d8027ae3b6e12258c6773c6eba0ca68b957f9bb976680f0e3e3352771e91";
    public static async Task<TowerLevel40Growth> Grow(string root,OfflineContent content,TowerReturnOwner owner,
        Func<TowerReturnOwner,int,CancellationToken,Task<JsonElement>> source,CancellationToken ct)
    {
        var journey=TowerJourneyProgression.RestoreRuntime(root,content,owner.Origin,owner.Runtime);
        var inventory=new TowerActivityInventory(root,content);
        TowerEarnedPartyStudy.ValidateInventory(owner.Point.Character,owner.Point.Owned,inventory);
        if(journey.Growth.Character.Level is <30 or >=40||owner.Runtime.Growth.Attuned!=4||owner.Runtime.IdleSeconds%10!=0)
            throw new InvalidDataException("Unexpected forward growth checkpoint.");
        var from=checked((int)(journey.IdleSeconds/10));var until=from;int victories=0;
        var xp=journey.Growth.Areas.CalculateEncounterExperience(Area,1);var changes=new List<JsonElement>();
        var area=HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/world/regions.json")).GetProperty("regions").EnumerateArray()
            .SelectMany(r=>r.GetProperty("areas").EnumerateArray()).Single(a=>a.GetProperty("id").GetString()==Area);
        if(area.GetProperty("levelRequirement").GetInt32()>journey.Growth.Character.Level
            ||area.GetProperty("requiredCompletedQuestId").GetString()!="quest.shenic.between_day_and_night")throw new InvalidDataException("Changed Old Forest gate.");
        while(journey.Growth.Character.Level<40&&until<86400)
        {
            ct.ThrowIfCancellationRequested();var before=journey.Growth.Character.Level;var old=journey.Growth.State(0).Essences.Select(e=>e.Level).ToArray();
            var victory=TowerActivityInventory.Victory(owner.Point.Outcome,until+1);
            await journey.Idle(Area,victory,10,ct);until++;if(victory)victories++;
            if(before!=journey.Growth.Character.Level||!old.SequenceEqual(journey.Growth.State(0).Essences.Select(e=>e.Level)))
                changes.Add(JsonSerializer.SerializeToElement(new {encounter=until,at=journey.Now,state=journey.Growth.State(journey.Sources.Claims.Sum(c=>(long)c.Reward.CharacterExperience))},HarnessJson.Options));
        }
        if(until==from)throw new InvalidDataException("No forward activity.");
        var rewards=await inventory.Rewards(owner.Point.Character.Id,owner.Point.Outcome,Area,from,until,10,ct);
        if(rewards.Victories!=victories)throw new InvalidDataException("Idle outcome disagreement.");
        var middle=from+(until-from)/2;
        if(middle>from)
        {
            var left=await inventory.Rewards(owner.Point.Character.Id,owner.Point.Outcome,Area,from,middle,10,ct);
            var right=await inventory.Rewards(owner.Point.Character.Id,owner.Point.Outcome,Area,middle,until,10,ct);
            TowerRuntimeCopy.Equal(rewards.Equipment,left.Equipment.Concat(right.Equipment).ToArray(),"forward idle reward partition");
            foreach(var key in rewards.Sigils.Keys.Concat(left.Sigils.Keys).Concat(right.Sigils.Keys).Distinct())
                if(rewards.Sigils.GetValueOrDefault(key)!=left.Sigils.GetValueOrDefault(key)+right.Sigils.GetValueOrDefault(key))throw new InvalidDataException("Sigil partition drift.");
        }
        journey.Sources.AddClaimedItems(rewards.Sigils);
        var assembled=await journey.Assemble(ct);var guards=await journey.Sources.VerifyClaimGuards(ct);
        var owned=owner.Point.Owned.Concat(rewards.Equipment).ToArray();
        if(owned.Select(e=>e.State.Id).Distinct().Count()!=owned.Length)throw new InvalidDataException("Reused idle award identity.");
        var character=journey.Growth.Snapshot(owner.Point.Character with {Equipment=inventory.Select(owned)});
        // Horizon identifies the original cohort, while Encounter records its new personal activity.
        var point=owner.Point with {Character=character,Owned=owned,Encounter=until,AvailableAt=journey.Now};
        TowerEarnedPartyStudy.ValidateInventory(character,owned,inventory);
        var grown=owner with {Point=point,Runtime=journey.ExportRuntime()};
        TowerJourneyProgression.RestoreRuntime(root,content,grown.Origin,grown.Runtime);
        return new(grown,from,until,victories,xp,assembled,guards,rewards,changes.ToArray(),await source(grown,victories,ct));
    }

    public static async Task Run(TowerLevel40Request q,Func<TowerReturnOwner,int,CancellationToken,Task<JsonElement>> source,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        var plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-level40.json"));
        if(plan.GetProperty("version").GetString()!=Version||plan.GetProperty("targetLevel").GetInt32()!=40||plan.GetProperty("area").GetString()!=Area
            ||plan.GetProperty("maximumIdleEncounters").GetInt32()!=86400||plan.GetProperty("cadenceSeconds").GetInt32()!=10||plan.GetProperty("maximumOwners").GetInt32()!=240
            ||Path.Exists(q.Output)||HarnessJson.FileHash(Path.Combine(q.ExpansionArchive,"files.json"))!=ExpansionPin
            ||HarnessJson.FileHash(Path.Combine(q.PreparationArchive,"files.json"))!=PreparationPin||HarnessJson.FileHash(Path.Combine(q.ReturnArchive,"files.json"))!=TowerExpansionStudy.ReturnPin)
            throw new InvalidDataException("Changed forward growth contract.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        var roster=HarnessJson.Read<TowerEarnedPlan>(Path.Combine(q.Fixtures,"tower-earned-party.json"));
        Directory.CreateDirectory(q.Output);var rows=new List<object>();int count=0;
        foreach(var row in TowerUnlockStudy.Read(Path.Combine(q.ExpansionArchive,"result.json")).GetProperty("journeys").EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();var key=row.GetProperty("key").GetString()!;var file=key+"--continuation.json.gz";
            var current=TowerUnlockStudy.Read(Path.Combine(q.ExpansionArchive,file));
            var proof=TowerUnlockStudy.Read(Path.Combine(q.PreparationArchive,key+"--expansion.json.gz")).Deserialize<TowerExpansionProof>(HarnessJson.Options)!;
            var original=current.GetProperty("owners").Deserialize<TowerReturnOwner[]>(HarnessJson.Options)!;
            if(current.GetProperty("highestCleared").GetInt32()!=4||original.Length!=16)throw new InvalidDataException("Unexpected floor-5 population.");
            var growth=new List<TowerLevel40Growth>();
            foreach(var owner in original)
            {
                if(++count>240)throw new InvalidDataException("Growth population cap.");
                growth.Add(await Grow(q.ApiRoot,content,owner,source,ct));
            }
            var at=growth.Max(g=>TowerReturnStudy.At(g.Owner.Runtime));
            var owners=growth.Select(g=>g.Owner with {Runtime=TowerReturnStudy.Wait(g.Owner.Runtime,at)}).ToArray();
            var members=TowerExpansionStudy.Roster(roster,owners,proof.Continuation.Party.Rotation,10);
            var party=proof.Continuation.Party with {StartsAt=at,Members=members};
            var prepared=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,party,0,ct));
            var historical=proof.Continuation.HistoricalAttempts.ToList();
            foreach(var step in current.GetProperty("steps").EnumerateArray())
            {
                var a=TowerUnlockStudy.Read(Path.Combine(q.ExpansionArchive,step.GetProperty("file").GetString()!));
                historical.Add(JsonSerializer.SerializeToElement(new {floor=5,attempt=a.GetProperty("index").GetInt32(),battle=a.GetProperty("battles").GetProperty("actual"),receipt=a.GetProperty("receipt")},HarnessJson.Options));
            }
            // Keep native server reward state at its last combat timestamp. Personal growth is a distinct, explicit refresh;
            // a future native restoration must advance that clock and personal XP without replaying rewards.
            TowerReturnStudy.Save(q.Output,key+"--growth.json.gz",new {key,sourceFile=file,sourceHash=HarnessJson.FileHash(Path.Combine(q.ExpansionArchive,file)),
                preparationHash=HarnessJson.FileHash(Path.Combine(q.PreparationArchive,key+"--expansion.json.gz")),
                server=current.GetProperty("final"),historicalAttempts=historical,personalRefreshBefore=proof.PersonalRefreshBefore,
                nextPersonalRefreshBefore=historical.Count,growth,owners,party,prepared,
                questProgressScenario="Prospective unclaimed Roots Remember; fresh wins only; no historical dungeon completion credit",newEssencesGranted=0});
            rows.Add(new {key,owners=16,partySize=10,startedAt=current.GetProperty("final").GetProperty("at").GetDateTimeOffset(),endedAt=at,
                levelMin=owners.Min(o=>o.Point.Character.Level),levelMax=owners.Max(o=>o.Point.Character.Level),
                newEquipment=growth.Sum(g=>g.Rewards.Equipment.Count),newIdleEncounters=growth.Sum(g=>g.Until-g.From),assembledSigils=growth.Sum(g=>g.AssembledSigils)});
        }
        if(count!=240||rows.Count!=15)throw new InvalidDataException("Incomplete growth population.");
        var token=ShenicEssenceTokenCatalog.Definitions.Single(t=>t.ItemBaseId=="item.essence_token.old_forest");
        TowerReturnStudy.Save(q.Output,"source-rules.json",new {quest=HarnessJson.Read<JsonElement>(Path.Combine(q.ApiRoot,"Data/quests/region-01/roots-remember.v4.json")),token,
            initialQuestStateAssumed=true,historicalDungeonCredit=0,historicalDropCredit=0,previouslyOmittedTokensCredited=0,
            newEssencesGranted=0,rule="Prospective unclaimed quest scenario only. Past quest inventory and resonance are unknown; no retrospective entitlement. Six new wins and actual level qualify two objectives; a future paid successful Mines run must qualify the third. Options are source candidates, not searched builds."});
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="Level40GrowthQualified",plan,journeys=rows,
            held=TowerUnlockStudy.Read(Path.Combine(q.ExpansionArchive,"result.json")).GetProperty("held"),newFights=0,newSeeds=0,newEssencesGranted=0,
            measuredPlayerSamples=0,searchPerformed=false,combatAdmission=false});
        TowerReturnStudy.Save(q.Output,"files.json",Directory.GetFiles(q.Output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    }
}
