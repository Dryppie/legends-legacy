using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Guilds;
using Common.Randomness;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.Extensions.Configuration;
using Services.LL.Essences;
using Services.LL.Inventories;
using Services.LL.Levels;
using static BalanceHarness.TowerGrowthProgression;

namespace BalanceHarness;

/// <summary>Disposable native loadout checks; never grant XP, a fifth owned Essence, or unrecorded historical drops.</summary>
public static class TowerEssenceEligibility
{
    public static async Task<JsonElement> Inspect(string root,OfflineContent content,TowerReturnOwner owner,CancellationToken ct)
    {
        var original=HarnessJson.Hash(owner);var c=owner.Point.Character;var level=c.Level;
        var owned=owner.Runtime.Growth.OwnedEssences.Select(e=>new PlayerEssence {
            Id=e.Id,CharacterId=c.Id,EssenceDefinitionId=e.Definition,Level=e.Level,CurrentXp=e.CurrentXp,AscensionTier=e.AscensionTier}).ToList();
        var loadouts=new List<EssenceLoadout>();
        var repo=Boundary<IEssenceRepository>((m,a)=>m.Name switch {
            "GetCharacterLevelAsync"=>Task.FromResult(level),
            "GetPlayerEssencesAsync"=>Task.FromResult(owned),
            "CountOwnedPlayerEssencesAsync"=>Task.FromResult(((IReadOnlyCollection<Guid>)a[1]!).Count(id=>owned.Any(e=>e.Id==id))),
            "HasLoadoutNameAsync"=>Task.FromResult(false),
            "GetLoadoutsWithSlotsAsync"=>Task.FromResult(loadouts),
            "AddLoadoutAsync"=>Add((EssenceLoadout)a[0]!),
            "ReplaceLoadoutSlotsAsync"=>Task.CompletedTask,
            _=>throw new InvalidDataException("Unexpected Essence eligibility boundary: "+m.Name)
        });
        Task Add(EssenceLoadout value){loadouts.Add(value);return Task.CompletedTask;}
        var config=new ConfigurationBuilder().Build();var slots=new EssenceSlotUnlockService();
        var tables=new JsonCreatureEssenceLootTableRepository(config,root,HarnessJson.Options,content.Essences);
        var service=new EssenceSystemService(repo,Boundary<IInventoryRepository>(),Boundary<IItemBaseRepository>(),content.Essences,tables,
            new EssenceProgressionService(),slots,new EssenceLoadoutLimitService(),new InventoryItemFactory(),Boundary<IRandomProvider>(),
            Boundary<IGameEventOutbox>((m,a)=>m.Name=="EnqueueAsync"?Task.CompletedTask:throw new InvalidDataException(m.Name)),Boundary<IGuildMissionService>());
        var requested=owner.Runtime.Growth.State.Essences.Select((e,i)=>new SaveEssenceLoadoutSlotRequest(i,e.Id)).ToArray();
        var actual=await service.SaveLoadoutAsync(c.Id,new(null,"earned-tower",requested),ct);
        if(!actual.Succeeded)throw new InvalidDataException("Native earned Essence loadout rejected: "+actual.Message);
        var unownedId=StableRandom.Guid("tower-expansion-unowned-fifth",c.Id.ToString());
        if(requested.Length==5)
        {
            if(c.Level!=40||owned.Count!=5)throw new InvalidDataException("Expected five retained level-40 Essences.");
            level=39;var locked=await service.SaveLoadoutAsync(c.Id,new(null,"locked-fifth",requested),ct);
            level=40;var foreign=requested.Take(4).Append(new SaveEssenceLoadoutSlotRequest(4,unownedId)).ToArray();
            var unowned=await service.SaveLoadoutAsync(c.Id,new(null,"unowned-fifth",foreign),ct);
            if(locked.Succeeded||unowned.Succeeded||loadouts.Count!=1||HarnessJson.Hash(owner)!=original)
                throw new InvalidDataException("Retained fifth eligibility probe changed ownership.");
            return JsonSerializer.SerializeToElement(new {owner=c.Id,c.Level,unlockedSlots=slots.GetUnlockedSlotCount(c.Level),ownedEssences=owned.Count,
                actualAccepted=actual.Succeeded,savedOwnedIds=requested.Select(s=>s.PlayerEssenceId).ToArray(),
                at39=new {locked.Succeeded,locked.Message},foreignAt40=new {unowned.Succeeded,unowned.Message},newEssencesGranted=0,probeRetained=false},HarnessJson.Options);
        }
        var five=requested.Append(new SaveEssenceLoadoutSlotRequest(4,unownedId)).ToArray();
        var current=await service.SaveLoadoutAsync(c.Id,new(null,"unearned-fifth",five),ct);
        level=40;var atForty=await service.SaveLoadoutAsync(c.Id,new(null,"unowned-at-forty",five),ct);
        if(current.Succeeded||atForty.Succeeded||loadouts.Count!=1||HarnessJson.Hash(owner)!=original)
            throw new InvalidDataException("Eligibility probe granted or changed personal state.");
        var experience=new JsonCharacterExperienceProgressionProvider(config,root,HarnessJson.Options);
        var costs=Enumerable.Range(c.Level,Math.Max(0,40-c.Level)).Select(l=>new {level=l,experience=experience.GetRequiredExperience(l)}).ToArray();
        return JsonSerializer.SerializeToElement(new {owner=c.Id,c.Level,unlockedSlots=slots.GetUnlockedSlotCount(c.Level),ownedEssences=owned.Count,
            actualAccepted=actual.Succeeded,actual.Message,fifthAtCurrentLevel=new {current.Succeeded,current.Message},
            fifthAtHypotheticalLevel40=new {atForty.Succeeded,atForty.Message},unearnedLevelProbeRetained=false,
            currentExperience=owner.Runtime.Growth.State.Experience,experienceToLevel40=Math.Max(0,costs.Sum(x=>x.experience)-owner.Runtime.Growth.State.Experience),experienceSteps=costs,
            trackedUnboundItems=owner.Runtime.Sources.State.Items.Where(p=>p.Key.StartsWith("item.essence.",StringComparison.Ordinal)).ToDictionary(),
            historicalCreatureResonanceRecorded=false,historicalEssenceDropsRecorded=false,newEssencesGranted=0},HarnessJson.Options);
    }
    public static object SourceRules(string root,OfflineContent content)=>new {
        slots=Enumerable.Range(30,31).Select(level=>new {level,slots=EssenceSlotProgression.GetUnlockedSlotCount(level)}).ToArray(),
        CreatureResonanceConstants.GainPerFailedEligibleKill,CreatureResonanceConstants.FailedEligibleKillsToMaximumBonus,
        CreatureResonanceConstants.MaximumRelativeDropChanceBonus,CreatureFocusRules.BaseDropChanceMultiplier,CreatureFocusRules.SpawnChanceMultiplier,
        tables=new JsonCreatureEssenceLootTableRepository(new ConfigurationBuilder().Build(),root,HarnessJson.Options,content.Essences).GetAll(),
        rule="Unbound Essence acquisition requires eligible creature-specific drop/variant draws and retained resonance/focus state. Absorption consumes one distinct unbound item; attunement also requires the native slot and unique creature source. Historical drops/resonance were not captured by the four-Essence model, so no retrospective fifth award, focused spawn assumption, rate forecast or historical reset is credited."
    };
}
