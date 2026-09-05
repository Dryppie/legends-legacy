using System.Globalization;
using Application.Interfaces.Services.LL.Items;
using Common.Randomness;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class CombatAcquisitionRewardProcessor(
    CombatAcquisitionCatalog catalog,
    IItemBaseRepository itemBases,
    IOptions<EquipmentProgressionOptions> options,
    IPlainEquipmentRepository entitlements,
    EquipmentBlueprintCatalog? blueprints = null) : ICombatAcquisitionRewardProcessor
{
    public async Task<CombatAcquisitionRewardOutcome> ProcessAsync(
        IdleCombatRewardFacts facts,
        CancellationToken ct)
    {
        var rules = catalog.FindArea(facts.Area.Id);
        if (!options.Value.OrdinaryAcquisitionEnabled || rules is null || facts.Encounters.Count == 0)
            return CombatAcquisitionRewardOutcome.Empty;

        var equipment = new List<EquipmentData>();
        var sigils = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var encounter in facts.Encounters.OrderBy(x => x.StartedAt))
        {
            if (!encounter.IsVictory) continue;

            var identity = new[]
            {
                EquipmentKeys.AreaDropRewardIdentity,
                facts.CharacterId.ToString("N"),
                facts.ScheduleGeneration.ToString(CultureInfo.InvariantCulture),
                encounter.StartedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)
            };

            var equipmentRandom = new Random(StableRandom.Seed([.. identity, "equipment"]));
            if (equipmentRandom.NextDouble() < rules.AreaEquipment.DropChance)
            {
                var rarity = rules.AreaEquipment.Rarities.Roll(equipmentRandom.NextDouble());
                var definitions = blueprints is null ? catalog.DropDefinitions(rarity) : catalog.BaseDropDefinitions(rarity);
                var definition = definitions[equipmentRandom.Next(definitions.Count)];
                var quality = rules.AreaEquipment.Qualities.Roll(equipmentRandom.NextDouble());
                var attributeRollMultiplier = 0.95d + equipmentRandom.NextDouble() * 0.10d;
                var state = EquipmentState.Award(
                    StableRandom.Guid([.. identity, "equipment"]),
                    catalog.Equipment.Evaluator,
                    definition.Id,
                    rules.EquipmentTier,
                    rules.AreaEquipment.Rank,
                    new(EquipmentAwardKind.RandomDiscovery, facts.Area.Id, string.Join(":", identity.Skip(1))),
                    new(EquipmentOwnershipKind.UnboundPersonal, facts.CharacterId),
                    quality,
                    attributeRollMultiplier);
                if (blueprints is not null)
                    state = blueprints.RollVariant(state, catalog.Equipment,
                        blueprints.Sources.Where(x => x.Region == rules.Region).SelectMany(x => x.StyleIds).Distinct().ToArray(),
                        blueprints.AreaVariantChance, new Random(StableRandom.Seed([.. identity, "variant"])));
                equipment.Add(EquipmentData.Create(state, catalog.Equipment.Evaluator));
            }

            var sigilRandom = new Random(StableRandom.Seed([.. identity, "sigil"]));
            if (sigilRandom.NextDouble() < rules.SigilDropChance)
            {
                var sigil = rules.Sigils[sigilRandom.Next(rules.Sigils.Count)];
                sigils[sigil.ItemBaseId] = checked(sigils.GetValueOrDefault(sigil.ItemBaseId) + 1);
            }
        }

        var ids = equipment.Select(x => x.ItemBaseId).Concat(sigils.Keys).Distinct().ToArray();
        if (ids.Length == 0) return CombatAcquisitionRewardOutcome.Empty;
        var bases = await itemBases.GetItemBasesByIdsAsync(ids, ct);
        if (equipment.Any(x => !bases.TryGetValue(x.ItemBaseId, out var itemBase)
                || itemBase is not EquipmentBase equipmentBase || itemBase.Stackable
                || equipmentBase.EquipmentType != x.EquipmentType)
            || sigils.Keys.Any(x => !bases.TryGetValue(x, out var itemBase) || !itemBase.Stackable || !itemBase.IsBound))
            throw new InvalidOperationException("Regional equipment or Sigil definitions are unavailable or invalid.");

        var equipmentItems = equipment.Select(data =>
        {
            var instance = new EquipmentInstance
            {
                Id = data.State.Id,
                ItemBaseId = data.ItemBaseId,
                ItemBase = bases[data.ItemBaseId]
            };
            instance.ApplyProgressionData(data);
            return new InventoryItem
            {
                InventoryId = facts.CharacterId,
                ItemInstanceId = instance.Id,
                ItemInstance = instance,
                Quantity = 1
            };
        }).ToArray();

        var sigilItems = sigils.Select(pair =>
        {
            var instance = new ItemInstance
            {
                Id = StableRandom.Guid([
                    EquipmentKeys.AreaDropRewardIdentity,
                    facts.CharacterId.ToString("N"),
                    facts.ScheduleGeneration.ToString(CultureInfo.InvariantCulture),
                    facts.From.UtcTicks.ToString(CultureInfo.InvariantCulture),
                    facts.ProcessedUntil.UtcTicks.ToString(CultureInfo.InvariantCulture),
                    pair.Key]),
                ItemBaseId = pair.Key,
                ItemBase = bases[pair.Key]
            };
            return new InventoryItem
            {
                InventoryId = facts.CharacterId,
                ItemInstanceId = instance.Id,
                ItemInstance = instance,
                Quantity = pair.Value
            };
        }).ToArray();

        foreach (var item in equipment)
            await entitlements.RecordAwardAsync(facts.CharacterId, item, ct);

        return new(equipmentItems, sigilItems);
    }
}
