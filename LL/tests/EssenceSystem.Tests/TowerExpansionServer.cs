using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace EssenceSystem.Tests;

public sealed partial class WorldTowerServiceTests
{
    internal sealed partial class UnlockServer
    {
        public static async Task<UnlockServer> RestoreExpansion(string root,OfflineContent content,TowerExpansionProof proof,CancellationToken ct)
            =>await RestorePopulation(root,content,proof.Continuation,proof.PersonalRefreshBefore,null,ct);
        public static async Task<UnlockServer> RestoreFifthReturn(string root,OfflineContent content,TowerFifthReturnProof proof,CancellationToken ct)
            =>await RestorePopulation(root,content,proof.Continuation,proof.PersonalRefreshBefore,proof.PopulationRefreshBefore,ct);
        private static async Task<UnlockServer> RestorePopulation(string root,OfflineContent content,TowerReturnProof p,int personalRefreshBefore,int? populationRefreshBefore,CancellationToken ct)
        {
            var ids=p.Party.Members.Select(m=>m.Character.Id).ToHashSet();
            if(p.Party.StartsAt<p.PreviousState.GetProperty("at").GetDateTimeOffset())throw new InvalidDataException("Server rewind.");
            var outsider=p.Owners.First(o=>!ids.Contains(o.Point.Character.Id)).Point;
            var population=p.Owners.Select(o=>o.Point.Character).ToArray();var xp=p.Owners.ToDictionary(o=>o.Point.Character.Id,o=>checked((int)o.Runtime.Growth.State.Experience));
            var server=await Create(root,content,p.Party,outsider,p.Path,ct,xp,population);
            try
            {
                var old=p.PreviousState;var expected=JsonNode.Parse(old.GetRawText())!;
                expected["at"]=JsonSerializer.SerializeToNode(p.Party.StartsAt,HarnessJson.Options);
                var tokens=old.GetProperty("tokens").EnumerateArray().ToDictionary(t=>t.GetProperty("owner").GetGuid(),t=>t.GetProperty("towerTokens").GetInt32());
                expected["tokens"]=JsonSerializer.SerializeToNode(population.OrderBy(c=>c.Id).Select(c=>new {owner=c.Id,towerTokens=tokens.GetValueOrDefault(c.Id),c.Level,experience=xp[c.Id]}),HarnessJson.Options);
                var probe=old.GetProperty("probes")[0];
                expected["probes"]=JsonSerializer.SerializeToNode(population.Select(c=>new {owner=c.Id,level=c.Level,participating=ids.Contains(c.Id),
                    hypotheticalNextCompletedDungeonChest=probe.GetProperty("hypotheticalNextCompletedDungeonChest").GetString(),
                    oldCompletedDecisionChest=probe.GetProperty("oldCompletedDecisionChest").GetString(),
                    towerEquipmentSupplyProcessed=probe.GetProperty("towerEquipmentSupplyProcessed").GetBoolean()}),HarnessJson.Options);
                await server.RestoreHistory(p,ct,personalRefreshBefore,expected.Deserialize<JsonElement>(HarnessJson.Options),populationRefreshBefore);
                return server;
            }
            catch {await server.DisposeAsync();throw;}
        }
        public async Task<JsonElement> QualifyExpansionRally(CancellationToken ct)
        {
            var before=await State(ct);var leader=starting.Members[0].Character.Id;
            var created=await service.CreateRallyAsync(leader,starting.Floor.FloorNumber,TowerRallyMode.FirstClear,ct);
            if(!created.Succeeded)throw new InvalidDataException(created.Error);var id=created.Value!.Id;
            foreach(var m in starting.Members.Skip(1))
            {
                var applied=await service.ApplyToRallyAsync(m.Character.Id,id,ct);
                if(!applied.Succeeded)throw new InvalidDataException(applied.Error);
                var application=await db.TowerRallyApplications.SingleAsync(a=>a.TowerRallyId==id&&a.CharacterId==m.Character.Id,ct);
                var accepted=await service.AcceptRallyApplicationAsync(leader,id,application.Id,ct);
                if(!accepted.Succeeded)throw new InvalidDataException(accepted.Error);
            }
            var assigned=await service.UpdateRallyPartiesAsync(leader,id,starting.Members.Select((m,i)=>new TowerPartyAssignment(m.Character.Id,i+1)).ToArray(),ct);
            if(!assigned.Succeeded)throw new InvalidDataException(assigned.Error);
            var rally=await db.TowerRallies.Include(r=>r.Participants).SingleAsync(r=>r.Id==id,ct);
            if(!WorldTowerPartyRules.HasCompletePartyLayout(rally))throw new InvalidDataException("Native expanded layout rejected.");
            TowerRuntimeCopy.Equal(before,await State(ct),"eligibility probe changed earned state");
            var historical=await db.TowerRallies.Include(r=>r.Participants).Where(r=>r.Status==TowerRallyStatus.Completed).OrderBy(r=>r.StartedAt).ToArrayAsync(ct);
            return JsonSerializer.SerializeToElement(new {accepted=true,rally.RequiredSlots,rally.Status,
                participants=rally.Participants.OrderBy(p=>p.PartySlot).Select(p=>new {owner=p.CharacterId,account=p.AccountId,p.PartySlot,party=WorldTowerPartyRules.GetPartyNumber(p.PartySlot!.Value)}).ToArray(),
                historicalRosters=historical.Select(r=>new {attemptId=r.Id,r.FloorNumber,r.RequiredSlots,owners=r.Participants.OrderBy(p=>p.PartySlot).Select(p=>p.CharacterId).ToArray()}).ToArray(),
                modelRatingAvailabilityAssumed=true,assumedRawRating=1,liveAccountsVerified=false,probeRetained=false},HarnessJson.Options);
        }
    }
}
