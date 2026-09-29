using System.Text.Json;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences;
using Domain.Models.Prophecies;

namespace BalanceHarness;

/// <summary>Reconstructs only unchanged historical activity. Never executes or transfers combat to changed inputs.</summary>
public static class TowerJourneyReplay
{
    public static FixtureCharacter Origin(string root,string fixtures,OfflineContent content,TowerEarnedPoint point) =>
        TowerBootstrapCohorts.Create(root,fixtures,TowerBootstrapCohorts.Read(Path.Combine(fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe==point.Recipe&&c.Gear=="common"&&c.EssenceLevel==1).Character with {Id=point.Character.Id};

    public static async Task<TowerJourneyProgression> Restore(string root,string fixtures,string archive,OfflineContent content,
        TowerUpgradeCheckpoint checkpoint,CancellationToken ct)
    {
        var p=checkpoint.Point;var history=HarnessJson.Read<JsonElement>(Path.Combine(archive,p.History+"--history.json"));
        TowerRuntimeCopy.Equal(checkpoint,TowerUpgradeEntryStudy.Restore(p,history),"qualified checkpoint");
        var journey=new TowerJourneyProgression(root,content,Origin(root,fixtures,content,p),true);
        var windows=history.GetProperty("windows").EnumerateArray().Where(w=>w.GetProperty("from").GetInt32()<p.Encounter).ToArray();
        var checkpoints=history.GetProperty("progression").GetProperty("checkpoints").EnumerateArray()
            .Where(c=>c.GetProperty("encounter").GetInt32()<=p.Encounter).ToDictionary(c=>c.GetProperty("encounter").GetInt32());
        var questAt=history.GetProperty("summary").GetProperty("questAt").GetInt32();int window=0,eventIndex=0;
        async Task Event(ProphecyProgressKind kind,int amount,int? enemies=null)
        {
            if(eventIndex>=checkpoint.DungeonEvents.Length)throw new InvalidDataException("Missing historical event.");
            var expected=checkpoint.DungeonEvents[eventIndex++].Deserialize<TowerJourneyEvent>(HarnessJson.Options)!;
            if(expected.Kind!=kind.ToString()||expected.Amount!=amount||expected.EnemyCount!=enemies||expected.At!=journey.Now
                ||(kind!=ProphecyProgressKind.CreatureDefeated&&expected.Creature is not null))throw new InvalidDataException("Historical event order/clock mismatch.");
            await journey.Event(kind,amount,ct,enemies,expected.Creature);
        }
        for(var ordinal=1;ordinal<=p.Encounter;ordinal++)
        {
            ct.ThrowIfCancellationRequested();
            while(windows[window].GetProperty("until").GetInt32()<ordinal)window++;
            var w=windows[window];if(ordinal<=w.GetProperty("from").GetInt32())throw new InvalidDataException("Idle prefix gap.");
            await journey.Idle(w.GetProperty("area").GetString()!,TowerActivityInventory.Victory(p.Outcome,ordinal),10,ct);
            if(ordinal==questAt)journey.Growth.Attune(4);
            if(!checkpoints.TryGetValue(ordinal,out var checkpointBefore))continue;
            if(await journey.Assemble(ct)!=checkpointBefore.GetProperty("assembled").GetInt32())throw new InvalidDataException("Assembly prefix mismatch.");
            TowerRuntimeCopy.Equal(checkpointBefore.GetProperty("state"),journey.State(),p.History+" pre-entry "+ordinal);
            foreach(var step in checkpoint.Steps.Where(s=>s.GetProperty("encounter").GetInt32()==ordinal))
            {
                var entry=checkpoint.Entries.Single(e=>e.GetProperty("ordinal").GetInt32()==step.GetProperty("ordinal").GetInt32());
                var receipt=entry.GetProperty("receipt").Deserialize<TowerJourneyDungeonReceipt>(HarnessJson.Options)!;
                var run=TowerUnlockStudy.Read(Path.Combine(archive,step.GetProperty("file").GetString()!)).Deserialize<DungeonAcquisitionRun>(HarnessJson.Options)!;
                TowerRuntimeCopy.Equal(entry.GetProperty("before"),journey.State(),"entry before");
                TowerRuntimeCopy.Equal(step.GetProperty("before"),journey.Growth.Snapshot(step.GetProperty("before").Deserialize<FixtureCharacter>(HarnessJson.Options)!),"entry earned growth");
                if(receipt.Started!=journey.Now||receipt.MasteryAtEntry!=(await journey.Mastery.GetMasteryByDungeonAsync(p.Character.Id,[run.Dungeon],ct))[run.Dungeon].Level)
                    throw new InvalidDataException("Entry clock/mastery mismatch.");
                var roomCount=0;
                foreach(var action in run.Actions)
                {
                    await journey.Observe(ct);
                    var room=receipt.Rooms.SingleOrDefault(r=>r.Room==action.Room);
                    if(room is not null)
                    {
                        roomCount++;journey.AdvanceCombat(room.DurationTicks);
                        await Event(room.Victory?ProphecyProgressKind.EncounterWon:ProphecyProgressKind.EncounterLost,1,room.Enemies);
                        for(var kill=0;kill<room.Kills;kill++)await Event(ProphecyProgressKind.CreatureDefeated,1);
                        await journey.Claim(ct);
                    }
                    else if(action.Type!=RoomType.RestSite)throw new InvalidDataException("Missing historical room receipt.");
                    if(action.Status!=DungeonRunStatus.Failed&&(room?.Victory==true||action.Type==RoomType.RestSite))await Event(ProphecyProgressKind.DungeonRoomCleared,1);
                    if(action.Status==DungeonRunStatus.Completed)await Event(ProphecyProgressKind.DungeonCompleted,1);
                    await journey.Claim(ct);
                }
                if(roomCount!=receipt.Rooms.Count||receipt.Ended!=journey.Now)throw new InvalidDataException("Room/clock conservation mismatch.");
                var reconstructed=TowerGrowthStudy.Reconstruct(p.Character.Id,run);
                TowerRuntimeCopy.Equal(receipt.Mastery,await journey.Mastery.AwardRunMasteryAsync(reconstructed,ct),"mastery award");
                if(!(await journey.Mastery.AwardRunMasteryAsync(reconstructed,ct)).AlreadyAwarded)throw new InvalidDataException("Duplicate mastery award.");
                if(run.Status==DungeonRunStatus.Completed)
                {
                    journey.Growth.BeginDungeonClaim();
                    await journey.Growth.ExperienceWriter.AddSplitExperienceAsync([p.Character.Id],receipt.PendingExperience,EssenceCombatActivity.Dungeon,ct);
                    journey.Growth.RecordDungeonClaim(receipt.PendingExperience);
                    if(receipt.AppliedExperience!=receipt.PendingExperience||receipt.EssenceExperience!=journey.Growth.LastEssenceExperience)
                        throw new InvalidDataException("Claimed XP mismatch.");
                    if(receipt.EssenceExperience>0)await Event(ProphecyProgressKind.EssenceXpGained,receipt.EssenceExperience);
                    await journey.Claim(ct);
                }
                else if(receipt.AppliedExperience!=0||receipt.EssenceExperience!=0)throw new InvalidDataException("Failed run XP credited.");
                TowerRuntimeCopy.Equal(entry.GetProperty("after"),journey.State(),"entry after");
            }
        }
        TowerRuntimeCopy.Equal(checkpoint.State,journey.State(),"final checkpoint");
        TowerRuntimeCopy.Equal(checkpoint.Days,journey.Days,"offers/choice prefix");
        TowerRuntimeCopy.Equal(checkpoint.Claims,journey.Sources.Claims,"claim prefix");
        TowerRuntimeCopy.Equal(checkpoint.DungeonEvents,journey.DungeonEvents,"dungeon event prefix");
        TowerRuntimeCopy.Equal(p.Character,journey.Growth.Snapshot(p.Character),"checkpoint loadout");
        if(eventIndex!=checkpoint.DungeonEvents.Length)throw new InvalidDataException("Unconsumed historical events.");
        return journey;
    }
}
