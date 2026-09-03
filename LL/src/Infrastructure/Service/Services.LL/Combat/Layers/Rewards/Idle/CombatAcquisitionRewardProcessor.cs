using System.Globalization;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Outbox;
using Common.Randomness;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class CombatAcquisitionRewardProcessor(CombatAcquisitionCatalog catalog, ICombatAcquisitionRepository repository,
    IItemBaseRepository itemBases, IGameEventOutbox outbox, IOptions<EquipmentProgressionOptions> options,
    IPlainEquipmentRepository entitlements) : ICombatAcquisitionRewardProcessor
{
    public async Task<CombatAcquisitionRewardOutcome> ProcessAsync(IdleCombatRewardFacts facts, CancellationToken ct)
    {
        var rules = catalog.Pools.SingleOrDefault(p => p.Areas.Any(x => x.AreaId == facts.Area.Id));
        var area = rules?.Areas.Single(x => x.AreaId == facts.Area.Id);
        if (!options.Value.OrdinaryAcquisitionEnabled || area == null || facts.Encounters.Count == 0)
            return CombatAcquisitionRewardOutcome.Empty;
        await repository.LockAsync(facts.CharacterId, ct);
        var progress = await repository.GetAsync(facts.CharacterId, rules!.PoolId, ct);
        if (progress == null)
        {
            progress = new() { CharacterId = facts.CharacterId, PoolId = rules!.PoolId };
            repository.Add(progress);
        }
        var equipment = new List<EquipmentData>();
        var resources = new Dictionary<string, int>(StringComparer.Ordinal);
        var targetAwards = new List<EquipmentData>();
        var definitions = catalog.Equipment.Options.OrderBy(x => x.DefinitionId, StringComparer.Ordinal).ToArray();
        foreach (var encounter in facts.Encounters.OrderBy(x => x.StartedAt))
        {
            var result = progress.Apply(facts.ScheduleGeneration, encounter.StartedAt, encounter.IsVictory, area.ScrapPerPerfectDay);
            if (!result.Applied) continue;
            if (result.Scrap > 0) AddResource("tempered_scrap", result.Scrap);
            if (result.SigilItemBaseId != null) AddResource(result.SigilItemBaseId, 1);
            if (result.Target is { } target) { equipment.Add(target); targetAwards.Add(target); }
            if (!encounter.IsVictory) continue;
            // Encounter IDs include batch sequence; use the stable schedule boundary instead so regrouping cannot reroll discovery.
            var identity = new[] { EquipmentKeys.OrdinaryRewardIdentity, facts.CharacterId.ToString("N"),
                facts.ScheduleGeneration.ToString(CultureInfo.InvariantCulture), encounter.StartedAt.UtcTicks.ToString(CultureInfo.InvariantCulture) };
            var random = new Random(StableRandom.Seed(identity));
            if (random.NextDouble() >= rules!.DiscoveryChance) continue;
            var definition = definitions[random.Next(definitions.Length)].DefinitionId;
            var data = EquipmentData.Create(EquipmentState.Award(StableRandom.Guid(identity), catalog.Equipment.Evaluator,
                definition, rules!.EquipmentTier, 0,
                new(EquipmentAwardKind.RandomDiscovery, facts.Area.Id, string.Join(":", identity.Skip(1))),
                new(EquipmentOwnershipKind.UnboundPersonal, facts.CharacterId)), catalog.Equipment.Evaluator);
            // Plain starter definitions retain zero base value; only this discovery route receives its authored salvage entitlement.
            equipment.Add(new(data.State with { BaseSalvageScrap = rules!.DiscoveryBaseScrap }, data.ItemBaseId,
                data.DisplayName, data.Rarity, data.EquipmentType, data.Behavior, data.Stats, data.EquipmentSetId));
        }
        var ids = equipment.Select(x => x.ItemBaseId).Concat(resources.Keys).Distinct().ToArray();
        if (ids.Length == 0) return CombatAcquisitionRewardOutcome.Empty;
        var bases = await itemBases.GetItemBasesByIdsAsync(ids, ct);
        if (equipment.Any(x => !bases.TryGetValue(x.ItemBaseId, out var b) || b is not EquipmentBase e || b.Stackable || e.EquipmentType != x.EquipmentType)
            || resources.Keys.Any(x => !bases.TryGetValue(x, out var b) || !b.Stackable)
            || resources.Keys.Where(x => x != "tempered_scrap").Any(x => !bases[x].IsBound))
            throw new InvalidOperationException("Ordinary Equipment progression reward definitions are unavailable or invalid.");
        var items = equipment.Select(data =>
        {
            var instance = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId, ItemBase = bases[data.ItemBaseId] };
            instance.ApplyProgressionData(data);
            return new InventoryItem { InventoryId = facts.CharacterId, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = 1 };
        }).ToArray();
        var resourceItems = resources.Select(pair =>
        {
            var instance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = pair.Key, ItemBase = bases[pair.Key] };
            return new InventoryItem { InventoryId = facts.CharacterId, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = pair.Value };
        }).ToArray();
        foreach (var target in targetAwards)
        {
            await entitlements.RecordAwardAsync(facts.CharacterId, target, ct);
            await outbox.EnqueueAsync(GameEventTypes.PlainEquipmentTargetSecured, target, facts.CharacterId, null, ct);
        }
        return new(items, resourceItems.Where(x => x.ItemInstance.ItemBaseId == "tempered_scrap").ToArray(),
            resourceItems.Where(x => x.ItemInstance.ItemBaseId != "tempered_scrap").ToArray());

        void AddResource(string id, int count) => resources[id] = checked(resources.GetValueOrDefault(id) + count);
    }
}
