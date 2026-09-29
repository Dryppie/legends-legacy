using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Common.Randomness;
using Domain.Extensions;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerBootstrapRecipe(string Id, string Starter, string Lumo, string Crystal, string Twilight);
public sealed record TowerBootstrapPlan(string Version, int Level, int MaximumAttempts, int PathsPerSource,
    IReadOnlyList<int> EssenceLevels, IReadOnlyList<string> GearConditions,
    IReadOnlyList<EquipmentSlotType> PurchaseOrder, IReadOnlyList<TowerBootstrapRecipe> Recipes, string Assumptions);
public sealed record TowerBootstrapDrop(string Definition, double PerEligibleVictory, double ExpectedVictoriesForThisItemAlone,
    string Requirement);
public sealed record TowerBootstrapCell(string Id, string Recipe, string Gear, int EssenceLevel,
    FixtureCharacter Character, IReadOnlyList<FixtureEquipment> OwnedItems, IReadOnlyList<FixtureEquipment> Target,
    long ReinforcementParts, long ReinforcementCinders, long QuestCinders, long AdditionalEarnedCindersRequired,
    long TrainingXpPerEssence, long SumOfEssenceXp, int RequiredOrdinaryItems, double ArmorBoxOutcomeProbability,
    IReadOnlyList<TowerBootstrapDrop> DropRequirements);

public static class TowerBootstrapCohorts
{
    public const string Version = "tower-first-supply-v1";
    public static TowerBootstrapPlan Read(string path)
    {
        var plan = TowerContractJson.Read<TowerBootstrapPlan>(path);
        if (plan.Version != Version || plan.Level != 30 || plan.MaximumAttempts != 12 || plan.PathsPerSource != 4
            || !plan.EssenceLevels.SequenceEqual(new[] { 1, 10 })
            || !plan.GearConditions.SequenceEqual(new[] { "quest-and-drop", "common", "uncommon", "common-rank1", "rare-control" })
            || plan.Recipes.Count != 4 || plan.Recipes.Select(r => r.Id).Distinct().Count() != 4
            || plan.PurchaseOrder.Count != 7 || plan.PurchaseOrder.Distinct().Count() != 7 || string.IsNullOrWhiteSpace(plan.Assumptions))
            throw new InvalidDataException("Changed bootstrap contract requires a new bounded declaration.");
        return plan;
    }

    public static IReadOnlyDictionary<string, JsonElement> PreDungeonQuests(string root, int level)
    {
        var all = Directory.GetFiles(Path.Combine(root, "Data/quests"), "*.json", SearchOption.AllDirectories)
            .Select(HarnessJson.Read<JsonElement>).Where(q => q.TryGetProperty("id", out _))
            .ToDictionary(q => q.GetProperty("id").GetString()!);
        var eligible = new Dictionary<string, JsonElement>();
        // Dependency closure; a reachable quest still requires its authored activity, not just level.
        bool changed;
        do
        {
            changed = false;
            foreach (var (id, quest) in all)
            {
                if (eligible.ContainsKey(id) || !(id.StartsWith("quest.onboarding.") || id.StartsWith("quest.shenic.") || id.StartsWith("quest.region01."))) continue;
                if (quest.GetProperty("objectives").EnumerateArray().Any(o => o.GetProperty("type").GetString()!.StartsWith("Dungeon"))) continue;
                if (quest.TryGetProperty("availability", out var a))
                {
                    if (a.TryGetProperty("minimumLevel", out var minimum) && minimum.GetInt32() > level) continue;
                    if (a.TryGetProperty("completedQuestIds", out var predecessors)
                        && predecessors.EnumerateArray().Any(p => !eligible.ContainsKey(p.GetString()!))) continue;
                }
                if (quest.GetProperty("objectives").EnumerateArray().Any(o => o.GetProperty("type").GetString() == "CharacterLevelReached"
                    && o.GetProperty("requiredAmount").GetInt32() > level)) continue;
                eligible.Add(id, quest); changed = true;
            }
        } while (changed);
        return eligible;
    }

    public static IReadOnlyList<TowerBootstrapCell> Create(string root, string fixtures, TowerBootstrapPlan plan, OfflineContent content)
    {
        var quests = PreDungeonQuests(root, plan.Level);
        var rewards = quests.Values.SelectMany(q => q.GetProperty("rewards").EnumerateArray()).ToArray();
        int Count(string item) => rewards.Where(r => r.TryGetProperty("itemBaseId", out var id) && id.GetString() == item)
            .Sum(r => r.GetProperty("quantity").GetInt32());
        var cinders = rewards.Where(r => r.GetProperty("type").GetString() == "Cinders").Sum(r => r.GetProperty("quantity").GetInt64());
        if (Count("sigil_goblin_mines") != 1 || Count("sigil_forgotten_catacombs") != 1 || Count("item.armor_chest") != 1
            || Count("item.essence_token.old_forest") != 0 || cinders != 500)
            throw new InvalidDataException("Review changed pre-dungeon quest budget.");
        var starterChoices = HarnessJson.Read<JsonElement>(Path.Combine(root, "Data/quests/onboarding/training-day.v4.json"))
            .GetProperty("choice").GetProperty("options").EnumerateArray().Select(o => o.GetProperty("key").GetString()).ToHashSet();
        var curve = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(fixtures, "tower-curve.json"));
        var acquisition = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(root, "Data/equipment/equipment-ordinary.v1.json"));
        var rules = acquisition.FindRegion(1)!;
        var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(root, "Data/equipment/equipment-upgrades.v1.json"));
        var blueprints = JsonEquipmentBlueprintCatalog.Load(Path.Combine(root, "Data/equipment/equipment-blueprints.v1.json"), content.Equipment);
        var supplies = JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(root, "Data/equipment/tower-equipment-supplies.v1.json"), content.Equipment);
        var supply = supplies.Candidates(1, plan.Level).Single(s => s.RequiredClearedFloor == 0);
        var evaluator = content.Equipment.Evaluator;
        var armorPool = acquisition.BaseDropDefinitions(EquipmentRarity.Common).Where(d =>
            RandomEquipmentBoxCatalog.ArmorChest.RandomEquipment!.EquipmentTypes!.Contains(evaluator.GetArchetype(d.ArchetypeId).EquipmentType)).ToArray();
        var result = new List<TowerBootstrapCell>();
        foreach (var recipe in plan.Recipes)
        {
            if (!starterChoices.Contains(recipe.Starter)) throw new InvalidDataException("Unobtainable First Hunt choice.");
            foreach (var (area, essence) in new[] { ("lumo_ruins", recipe.Lumo), ("crystal_creek", recipe.Crystal), ("twilight_clearing", recipe.Twilight) })
            {
                var token = ShenicEssenceTokenCatalog.Definitions.Single(t => t.ItemBaseId == ShenicEssenceTokenCatalog.ItemBaseId(area));
                if (Count(token.ItemBaseId) != 1 || !token.Options.Any(o => o.Id == essence && o.Quantity == 1))
                    throw new InvalidDataException("Token unavailable before any dungeon completion.");
            }
            var template = curve.Profiles.Single(p => p.Id == $"slots-4-{recipe.Id}").Build;
            var owner = StableRandom.Guid(Version, "owner", recipe.Id);
            var essences = new[] { recipe.Starter, recipe.Lumo, recipe.Crystal, recipe.Twilight }.Select(e => "essence." + e).ToArray();
            var baseCharacter = FixtureCharacter.From(content.CreateBuild(template with { CharacterLevel = plan.Level, EssenceIds = essences }));
            EquipmentData Award(string archetype, EquipmentRarity rarity, string source, double roll = .95)
            {
                var definition = evaluator.Definitions.Single(d => d.ArchetypeId == archetype && d.Rarity == rarity && d.SpecializationId == "default" && d.NativeStyleId is null);
                return EquipmentData.Create(EquipmentState.Award(StableRandom.Guid(Version, recipe.Id, source, archetype), evaluator,
                    definition.Id, 1, 0, new(source == "conditional-region-one-area-drop" ? EquipmentAwardKind.RandomDiscovery : EquipmentAwardKind.ProtectedReward, source, recipe.Id),
                    new(EquipmentOwnershipKind.BoundPersonal, owner), ItemQuality.Standard, roll), evaluator);
            }
            var questArmor = new FixtureEquipment(EquipmentSlotType.Chest, Award(evaluator.GetDefinition(template.Equipment.Single(e => e.Slot == EquipmentSlotType.Chest).DefinitionId).ArchetypeId,
                EquipmentRarity.Common, "item.armor_chest", 1));
            if (!armorPool.Any(d => d.Id == questArmor.Data.State.DefinitionId)) throw new InvalidDataException("Illegal quest armor outcome.");
            var questWeapon = new FixtureEquipment(EquipmentSlotType.MainHand, Award("plain.mace", EquipmentRarity.Common, "item.arms_chest", 1));
            foreach (var gear in plan.GearConditions)
            foreach (var level in plan.EssenceLevels)
            {
                var rarity = gear == "uncommon" ? EquipmentRarity.Uncommon : EquipmentRarity.Common;
                var equipment = template.Equipment.Where(e => gear != "quest-and-drop" || e.Slot is EquipmentSlotType.MainHand or EquipmentSlotType.Chest)
                    .Select(e => e.Slot == EquipmentSlotType.Chest && rarity == EquipmentRarity.Common ? questArmor
                        : new FixtureEquipment(e.Slot, Award(evaluator.GetDefinition(e.DefinitionId).ArchetypeId, rarity, "conditional-region-one-area-drop"))).ToArray();
                long parts = 0, cost = 0;
                if (gear == "common-rank1")
                    equipment = equipment.Select(e => {
                        var slots = e.Data.EquipmentType.OccupiedSlotCount();
                        parts += prices.ForTier(1).RankPartCosts[0] * slots;
                        cost += prices.ForTier(1).RankCinderCosts[0] * slots;
                        return e with { Data = EquipmentData.Create(e.Data.EquipmentState.Reinforce(evaluator), evaluator) };
                    }).ToArray();
                var character = baseCharacter with { Id = owner, Name = recipe.Id, Equipment = equipment,
                    Essences = essences.Select(e => new FixtureEssence(e, level, 0, false)).ToArray() };
                var target = template.Equipment.Select(e => {
                    var original = evaluator.GetDefinition(e.DefinitionId);
                    var choice = supplies.Choices(supply.ItemBaseId).Single(c => evaluator.GetDefinition(c.Id).ArchetypeId == original.ArchetypeId
                        && evaluator.GetDefinition(c.Id).SpecializationId == "default");
                    return new FixtureEquipment(e.Slot, supplies.Award(supply.ItemBaseId, choice.Id, owner,
                        StableRandom.Guid(Version, recipe.Id, "target", e.Slot.ToString()), "target-descriptor-only"));
                }).ToArray();
                var drops = equipment.Where(e => e.Data.State.Provenance.SourceId == "conditional-region-one-area-drop")
                    .Select(e => {
                        var definition = evaluator.GetDefinition(e.Data.State.DefinitionId);
                        var probability = DefinitionProbability(acquisition, definition.Id) * rules.AreaEquipment.DropChance
                            * (rarity == EquipmentRarity.Common ? rules.AreaEquipment.Rarities.Common : rules.AreaEquipment.Rarities.Uncommon)
                            * (1 - rules.AreaEquipment.Qualities.Crude) * (1 - blueprints.AreaVariantChance);
                        return new TowerBootstrapDrop(definition.Id, probability, 1 / probability,
                            "Exact default archetype; this rarity; Standard-or-better quality; unstyled. Conservative 0.95 roll. Individual expectation, not joint set time.");
                    }).ToArray();
                var xp = Enumerable.Range(1, level - 1).Sum(i => (long)EssenceProgressionConstants.GetXpRequiredForLevel(i));
                if (gear == "rare-control")
                {
                    equipment = target;
                    character = character with { Equipment = target };
                    drops = [];
                }
                result.Add(new($"{recipe.Id}--{gear}--essence-{level}", recipe.Id, gear, level, character,
                    equipment.Concat(new[] { questArmor, questWeapon }).DistinctBy(e => e.Data.State.Id).ToArray(), target,
                    parts, cost, cinders, Math.Max(0, cost - cinders), xp, xp * 4, drops.Length, 1d / armorPool.Length, drops));
            }
        }
        if (result.Count != 40) throw new InvalidDataException("Unexpected bootstrap matrix.");
        return result;
    }

    // Analytic counterpart of production hierarchical selection; adding profiles must not change archetype frequency.
    public static double DefinitionProbability(CombatAcquisitionCatalog catalog, string definitionId)
    {
        var evaluator = catalog.Equipment.Evaluator;
        var definition = evaluator.GetDefinition(definitionId);
        var type = evaluator.GetArchetype(definition.ArchetypeId).EquipmentType;
        var weights = catalog.FindRegion(1)!.SelectionWeights;
        bool Group(EquipmentType t) => type switch {
            EquipmentType.TwoHanded => t == EquipmentType.TwoHanded,
            EquipmentType.OneHanded or EquipmentType.OffHand => t is EquipmentType.OneHanded or EquipmentType.OffHand,
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs => t is EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs,
            _ => t is EquipmentType.Ring or EquipmentType.Necklace or EquipmentType.Relic };
        var group = catalog.BaseDropDefinitions(definition.Rarity).Where(d => Group(evaluator.GetArchetype(d.ArchetypeId).EquipmentType)).ToArray();
        var categoryWeight = type switch {
            EquipmentType.TwoHanded => weights.Weapons * weights.TwoHanded,
            EquipmentType.OneHanded or EquipmentType.OffHand => weights.Weapons * weights.OneHanded,
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs => weights.Armor, _ => weights.Jewelry };
        return categoryWeight / group.Select(d => d.ArchetypeId).Distinct().Count() / group.Count(d => d.ArchetypeId == definition.ArchetypeId);
    }
}
