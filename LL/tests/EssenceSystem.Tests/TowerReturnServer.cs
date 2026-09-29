using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Common.Randomness;
using Domain.Models.Achievements;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace EssenceSystem.Tests;

public sealed partial class WorldTowerServiceTests
{
    internal sealed partial class UnlockServer
    {
        public static async Task<UnlockServer> RestoreReturn(string root,OfflineContent content,TowerReturnProof proof,CancellationToken ct)
        {
            var members=proof.Party.Members.Select(m=>m.Character.Id).ToHashSet();
            var outsiderId=proof.PreviousState.GetProperty("tokens").EnumerateArray().Select(t=>t.GetProperty("owner").GetGuid()).Single(id=>!members.Contains(id));
            var outsider=proof.Owners.Single(o=>o.Point.Character.Id==outsiderId).Point;
            var experience=proof.Owners.ToDictionary(o=>o.Point.Character.Id,o=>checked((int)o.Runtime.Growth.State.Experience));
            if(proof.Party.StartsAt<proof.PreviousState.GetProperty("at").GetDateTimeOffset())throw new InvalidDataException("Server rewind.");
            // Create old completion decisions while floor 3 is still locked, then restore the actual server history.
            var server=await Create(root,content,proof.Party,outsider,proof.Path,ct,experience);
            try
            {
                await server.RestoreHistory(proof,ct);
                return server;
            }
            catch {await server.DisposeAsync();throw;}
        }
        private async Task RestoreHistory(TowerReturnProof proof,CancellationToken ct,int? personalRefreshBefore=null,JsonElement? expandedExpected=null,int? populationRefreshBefore=null)
        {
            var previous=proof.PreviousState;
            if(proof.HistoricalAttempts.Length>0)
                TowerRuntimeCopy.Equal(previous,proof.HistoricalAttempts[^1].GetProperty("receipt").GetProperty("after"),"historical terminal server");
            for(var i=0;i<proof.HistoricalAttempts.Length;i++)
            {
                var a=proof.HistoricalAttempts[i];var r=a.GetProperty("receipt");var before=r.GetProperty("before");var after=r.GetProperty("after");
                if(i>0)
                {
                    var prior=proof.HistoricalAttempts[i-1].GetProperty("receipt").GetProperty("after");
                    if(i==populationRefreshBefore)
                        ValidatePopulationBoundary(prior,before);
                    else if(i==personalRefreshBefore)
                    {
                        // The acquisition phase advances personal XP and time but cannot change Tower rewards.
                        if(before.GetProperty("at").GetDateTimeOffset()<prior.GetProperty("at").GetDateTimeOffset())throw new InvalidDataException("Historical clock rewind.");
                        foreach(var field in new[] {"floors","titles","unlocks"})TowerRuntimeCopy.Equal(prior.GetProperty(field),before.GetProperty(field),"acquisition server boundary");
                        TowerRuntimeCopy.Equal(prior.GetProperty("tokens").EnumerateArray().Select(t=>new {owner=t.GetProperty("owner").GetGuid(),tokens=t.GetProperty("towerTokens").GetInt32()}).ToArray(),
                            before.GetProperty("tokens").EnumerateArray().Select(t=>new {owner=t.GetProperty("owner").GetGuid(),tokens=t.GetProperty("towerTokens").GetInt32()}).ToArray(),"acquisition token boundary");
                    }
                    else TowerRuntimeCopy.Equal(prior,before,"historical server sequence");
                }
                var id=r.GetProperty("attemptId").GetGuid();var floor=a.GetProperty("floor").GetInt32();var success=a.GetProperty("battle").GetProperty("succeeded").GetBoolean();
                if(after.GetProperty("at").GetDateTimeOffset()>clock.Now)throw new InvalidDataException("Future historical attempt.");
                var historicalMembers=before.GetProperty("probes").EnumerateArray().Where(p=>p.GetProperty("participating").GetBoolean())
                    .Select(p=>actors.Single(c=>c.Id==p.GetProperty("owner").GetGuid())).ToArray();
                if(populationRefreshBefore.HasValue&&i>=populationRefreshBefore.Value)
                {
                    if(!historicalMembers.Select(c=>c.Id).ToHashSet().SetEquals(starting.Members.Select(m=>m.Character.Id)))
                        throw new InvalidDataException("Historical expanded roster changed owners.");
                    historicalMembers=starting.Members.Select(m=>m.Character).ToArray();
                }
                if(historicalMembers.Length!=definitions.GetFloor(floor)!.RequiredSlots)throw new InvalidDataException("Historical roster changed size.");
                var rally=new TowerRally {Id=id,ServerId=Server,FloorNumber=floor,Mode=TowerRallyMode.FirstClear,Status=TowerRallyStatus.Completed,
                    CreatedByCharacterId=historicalMembers[0].Id,RequiredSlots=historicalMembers.Length,
                    CreatedAt=before.GetProperty("at").GetDateTimeOffset(),StartedAt=before.GetProperty("at").GetDateTimeOffset(),CompletedAt=after.GetProperty("at").GetDateTimeOffset(),
                    Participants=historicalMembers.Select((m,slot)=>new TowerRallyParticipant {Id=StableRandom.Guid(id.ToString(),m.Id.ToString()),
                        CharacterId=m.Id,AccountId=m.Id,CharacterName=m.Name,PartySlot=slot+1,JoinedAt=before.GetProperty("at").GetDateTimeOffset()}).ToList()};
                db.TowerAttempts.Add(new TowerAttempt {Id=id,TowerRally=rally,ServerId=Server,FloorNumber=floor,Mode=TowerRallyMode.FirstClear,
                    Status=success?TowerAttemptStatus.Succeeded:TowerAttemptStatus.Failed,Succeeded=success,AttemptNumberForFloor=a.GetProperty("attempt").GetInt32()+1,
                    StartedAt=rally.StartedAt!.Value,CompletedAt=rally.CompletedAt,CombatResultJson=a.GetProperty("battle").GetRawText()});
            }
            foreach(var f in previous.GetProperty("floors").EnumerateArray())
            {
                var p=await db.TowerFloorProgresses.SingleAsync(p=>p.FloorNumber==f.GetProperty("floorNumber").GetInt32(),ct);
                p.IsCleared=f.GetProperty("isCleared").GetBoolean();p.ScoutingProgress=f.GetProperty("scoutingProgress").GetInt32();
                p.FirstClearAttemptId=f.GetProperty("firstClearAttemptId").Deserialize<Guid?>(HarnessJson.Options);
                p.UnlockedAt=f.GetProperty("unlockedAt").Deserialize<DateTimeOffset?>(HarnessJson.Options);
                p.ClearedAt=f.GetProperty("clearedAt").Deserialize<DateTimeOffset?>(HarnessJson.Options);
            }
            foreach(var t in previous.GetProperty("tokens").EnumerateArray())
                (await db.Characters.SingleAsync(c=>c.Id==t.GetProperty("owner").GetGuid(),ct)).TowerTokens=t.GetProperty("towerTokens").GetInt32();
            foreach(var t in previous.GetProperty("titles").EnumerateArray())
            {
                var definition=await db.TitleDefinitions.SingleAsync(d=>d.Key==t.GetProperty("key").GetString(),ct);
                var floor=definitions.GetFloors().Single(f=>f.RewardTitleKey==definition.Key).FloorNumber;
                var earned=proof.HistoricalAttempts.Single(a=>a.GetProperty("floor").GetInt32()==floor&&a.GetProperty("battle").GetProperty("succeeded").GetBoolean());
                var owner=t.GetProperty("owner").GetGuid();var receipt=earned.GetProperty("receipt");
                db.PlayerTitleUnlocks.Add(new PlayerTitleUnlock {Id=StableRandom.Guid(TowerReturnStudy.Version,proof.Key,owner.ToString(),definition.Key),
                    AccountId=owner,CharacterId=owner,TitleDefinitionId=definition.Id,
                    UnlockedAt=receipt.GetProperty("after").GetProperty("at").GetDateTimeOffset(),
                    MetadataJson=JsonSerializer.Serialize(new {source="WorldTower",floorNumber=floor,attemptId=receipt.GetProperty("attemptId").GetGuid(),rallyId=receipt.GetProperty("attemptId").GetGuid()})});
            }
            foreach(var u in previous.GetProperty("unlocks").EnumerateArray())db.ServerUnlocks.Add(new ServerUnlock {
                Id=StableRandom.Guid(TowerReturnStudy.Version,proof.Key,u.GetProperty("unlockKey").GetString()!),ServerId=Server,
                UnlockKey=u.GetProperty("unlockKey").GetString()!,SourceFloorNumber=u.GetProperty("sourceFloorNumber").GetInt32(),UnlockedAt=u.GetProperty("unlockedAt").GetDateTimeOffset()});
            await db.SaveChangesAsync(ct);db.ChangeTracker.Clear();
            var expected=JsonNode.Parse(previous.GetRawText())!;expected["at"]=JsonSerializer.SerializeToNode(clock.Now,HarnessJson.Options);
            foreach(var t in expected["tokens"]!.AsArray())
            {
                var owner=proof.Owners.Single(o=>o.Point.Character.Id==t!["owner"]!.GetValue<Guid>());
                t!["level"]=owner.Point.Character.Level;t["experience"]=owner.Runtime.Growth.State.Experience;
            }
            foreach(var p in expected["probes"]!.AsArray())p!["level"]=proof.Owners.Single(o=>o.Point.Character.Id==p["owner"]!.GetValue<Guid>()).Point.Character.Level;
            TowerRuntimeCopy.Equal(expandedExpected??expected.Deserialize<JsonElement>(HarnessJson.Options),await State(ct),"preserved server rewards and progression");
        }
        internal static void ValidatePopulationBoundary(JsonElement prior,JsonElement next)
        {
            if(prior.GetProperty("at").GetDateTimeOffset()!=next.GetProperty("at").GetDateTimeOffset())
                throw new InvalidDataException("Population expansion cannot award activity time.");
            foreach(var field in new[] {"floors","titles","unlocks","earnedNewEquipment"})
                TowerRuntimeCopy.Equal(prior.GetProperty(field),next.GetProperty(field),"population expansion rewards");
            var old=prior.GetProperty("tokens").EnumerateArray().ToDictionary(t=>t.GetProperty("owner").GetGuid());
            var added=next.GetProperty("tokens").EnumerateArray().ToDictionary(t=>t.GetProperty("owner").GetGuid());
            if(old.Count!=6||added.Count!=16||old.Keys.Any(id=>!added.ContainsKey(id)))throw new InvalidDataException("Invalid population expansion.");
            foreach(var (id,t) in old)TowerRuntimeCopy.Equal(t,added[id],"old population growth/tokens");
            if(added.Where(p=>!old.ContainsKey(p.Key)).Any(p=>p.Value.GetProperty("towerTokens").GetInt32()!=0))
                throw new InvalidDataException("Retroactive newcomer tokens.");
            var before=prior.GetProperty("probes").EnumerateArray().ToDictionary(p=>p.GetProperty("owner").GetGuid());
            var after=next.GetProperty("probes").EnumerateArray().ToDictionary(p=>p.GetProperty("owner").GetGuid());
            if(!before.Keys.ToHashSet().SetEquals(old.Keys)||!after.Keys.ToHashSet().SetEquals(added.Keys)
                ||before.Values.Count(p=>p.GetProperty("participating").GetBoolean())!=5||after.Values.Count(p=>p.GetProperty("participating").GetBoolean())!=10)
                throw new InvalidDataException("Changed population or participant coverage.");
            foreach(var (id,p) in before)
            {
                if(p.GetProperty("participating").GetBoolean()&&!after[id].GetProperty("participating").GetBoolean())
                    throw new InvalidDataException("Lost prior participant.");
                foreach(var field in new[] {"level","hypotheticalNextCompletedDungeonChest","oldCompletedDecisionChest","towerEquipmentSupplyProcessed"})
                    TowerRuntimeCopy.Equal(p.GetProperty(field),after[id].GetProperty(field),"retained population probe");
            }
            foreach(var (id,p) in after)
                if(p.GetProperty("level").GetInt32()!=added[id].GetProperty("level").GetInt32())throw new InvalidDataException("Population probe level mismatch.");
        }
    }
}
