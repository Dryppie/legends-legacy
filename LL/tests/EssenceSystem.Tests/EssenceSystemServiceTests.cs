using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using Domain.Components.Attributes;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Essences;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Combat;
using Services.LL.Combat.CombatEngine;
using Services.LL.Combat.Stats;
using Services.LL.Essences;
using Services.LL.Interfaces;

namespace EssenceSystem.Tests;

public sealed class EssenceSystemServiceTests
{
    [Fact]
    public async Task AbsorbUnboundEssence_consumes_item_creates_archive_entry_and_rejects_duplicates()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var itemInstanceId = await AddEssenceItemAsync(db, characterId, quantity: 2);
        var service = CreateService(db);

        var first = await service.AbsorbUnboundEssenceAsync(characterId, itemInstanceId, CancellationToken.None);
        await db.SaveChangesAsync();
        var second = await service.AbsorbUnboundEssenceAsync(characterId, itemInstanceId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Single(db.PlayerEssences.Where(x => x.CharacterId == characterId));
        Assert.Equal(1, db.InventoryItems.Single(x => x.ItemInstanceId == itemInstanceId).Quantity);
    }

    [Fact]
    public async Task DismantleUnboundEssence_consumes_item_and_grants_essence_dust()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var itemInstanceId = await AddEssenceItemAsync(db, characterId, quantity: 1, dust: 3);
        var service = CreateService(db);

        var result = await service.DismantleUnboundEssenceAsync(characterId, itemInstanceId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.DustGained);
        Assert.DoesNotContain(db.InventoryItems, x => x.ItemInstanceId == itemInstanceId);
        Assert.Equal(3, await InventoryQuantityAsync(db, characterId, "soul_dust"));
    }

    [Fact]
    public async Task SaveLoadout_rejects_unabsorbed_duplicate_and_locked_slot_assignments()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 1);
        var absorbedId = await AddPlayerEssenceAsync(db, characterId);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveLoadoutAsync(characterId, new SaveEssenceLoadoutRequest(null, "Bad", [new(0, Guid.NewGuid())]), CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveLoadoutAsync(characterId, new SaveEssenceLoadoutRequest(null, "Duplicate", [new(0, absorbedId), new(1, absorbedId)]), CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveLoadoutAsync(characterId, new SaveEssenceLoadoutRequest(null, "Locked", [new(1, absorbedId)]), CancellationToken.None));
    }

    [Fact]
    public async Task Only_attuned_essences_contribute_bonuses_and_gain_combat_xp()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var attunedId = await AddPlayerEssenceAsync(db, characterId, "essence.test");
        var inactiveId = await AddPlayerEssenceAsync(db, characterId, "essence.other");
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = attunedId }]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var bonuses = await service.GetAttunedAttributeModifiersAsync(characterId, CancellationToken.None);
        await service.GrantCombatXpToAttunedEssencesAsync(characterId, 50, CancellationToken.None);

        var attackPowerBonus = Assert.Single(bonuses, x => x.AttributeType == AttributeType.Power);
        Assert.Equal(2, attackPowerBonus.Amount);
        Assert.Equal(ModifierType.Flat, attackPowerBonus.ModifierType);
        Assert.Equal(50, db.PlayerEssences.Single(x => x.Id == attunedId).CurrentXp);
        Assert.Equal(0, db.PlayerEssences.Single(x => x.Id == inactiveId).CurrentXp);
    }

    [Fact]
    public void Attribute_calculator_applies_flat_additive_and_multiplicative_phases()
    {
        var result = AttributeCalculator.CalculateModifiedValue(100, [
            new EssenceAttributeModifier(AttributeType.Power, 10, ModifierType.Flat),
            new EssenceAttributeModifier(AttributeType.Power, 25, ModifierType.Additive),
            new EssenceAttributeModifier(AttributeType.Power, 50, ModifierType.Multiplicative)
        ]);

        Assert.Equal(206, result);
    }

    [Fact]
    public void Attribute_calculator_uses_new_stats_directly_and_applies_max_health_percent()
    {
        var projected = AttributeCalculator.CalculateProjectedAttributes(
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Spirit] = 50,
                [AttributeType.Power] = 10,
                [AttributeType.Armor] = 0,
                [AttributeType.Resistance] = 0,
                [AttributeType.DodgeChance] = 0
            },
            [
                new EssenceAttributeModifier(AttributeType.Power, 5, ModifierType.Flat),
                new EssenceAttributeModifier(AttributeType.Fortitude, 2, ModifierType.Flat),
                new EssenceAttributeModifier(AttributeType.MaxHealth, 10, ModifierType.Flat),
                new EssenceAttributeModifier(AttributeType.DodgeChance, 3, ModifierType.Flat),
                new EssenceAttributeModifier(AttributeType.Resistance, 4, ModifierType.Flat)
            ]);

        Assert.Equal(15, projected[AttributeType.Power]);
        Assert.Equal(110, projected[AttributeType.MaxHealth]);
        Assert.Equal(2, projected[AttributeType.Fortitude]);
        Assert.Equal(0, projected[AttributeType.Armor]);
        Assert.Equal(4, projected[AttributeType.Resistance]);
        Assert.Equal(3, projected[AttributeType.DodgeChance]);
    }

    [Fact]
    public async Task Percent_essence_bonuses_emit_additive_stat_modifiers()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, "essence.percent");
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = essenceId }]
        });
        await db.SaveChangesAsync();
        var definition = FakeDefinitionRepository.CreateDefinition("essence.percent", "monster.percent");
        definition.AttributeBonuses.Single().ModifierKind = EssenceModifierKind.Percent;
        definition.AttributeBonuses.Single().BaseValue = 15;
        var service = CreateService(db, definitions: new SingleDefinitionRepository(definition));

        var modifiers = await service.GetAttunedAttributeModifiersAsync(characterId, CancellationToken.None);

        var modifier = Assert.Single(modifiers, x => x.AttributeType == AttributeType.Power);
        Assert.Equal(15, modifier.Amount);
        Assert.Equal(ModifierType.Additive, modifier.ModifierType);
    }

    [Fact]
    public async Task Attuned_essences_generate_scaled_combat_abilities_for_active_loadout_only()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var attunedId = await AddPlayerEssenceAsync(db, characterId, "essence.test", level: 3);
        await AddPlayerEssenceAsync(db, characterId, "essence.other", level: 3);
        db.PlayerEssences.Single(x => x.Id == attunedId).AscensionTier = 1;
        db.PlayerEssences.Single(x => x.Id == attunedId).IsEvolved = true;
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = attunedId }]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var abilities = await service.GetAttunedCombatAbilitiesAsync(characterId, CancellationToken.None);

        Assert.Equal(2, abilities.Count);
        Assert.All(abilities, x => Assert.StartsWith("essence.test", x.Definition.Id));

        var active = abilities.Single(x => x.Definition.Type == CombatAbilityType.Active).Definition;
        Assert.Equal(180, active.Cooldown);
        Assert.Equal(180, abilities.Single(x => x.Definition.Type == CombatAbilityType.Active).RemainingTimeUntilUse);
        Assert.Equal(TriggerEvent.OnAbilityUsed, active.Triggers.Single().Event);
        Assert.IsType<AbilityIdFilter>(active.Triggers.Single().Filters.Single());
        var activeAction = Assert.IsType<CombatEffectAction>(active.Triggers.Single().Actions.Single().Action);
        Assert.Equal(CombatEffectOperation.Damage, activeAction.Operation);
        Assert.Equal(26, activeAction.Magnitude);

        var passive = abilities.Single(x => x.Definition.Type == CombatAbilityType.Passive).Definition;
        Assert.Equal(0, abilities.Single(x => x.Definition.Type == CombatAbilityType.Passive).RemainingTimeUntilUse);
        Assert.Equal(TriggerEvent.OnAttack, passive.Triggers.Single().Event);
        var passiveAction = Assert.IsType<CombatEffectAction>(passive.Triggers.Single().Actions.Single().Action);
        Assert.Equal(CombatEffectOperation.ModifyAttribute, passiveAction.Operation);
        Assert.Equal(AttributeType.Power, passiveAction.Attribute);
        Assert.Equal(8, passiveAction.Magnitude);
    }

    [Fact]
    public async Task Resolve_combat_loadout_exposes_attuned_essence_abilities_bonuses_and_tags()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var attunedId = await AddPlayerEssenceAsync(db, characterId, "essence.test", level: 3);
        await AddPlayerEssenceAsync(db, characterId, "essence.other", level: 3);
        db.PlayerEssences.Single(x => x.Id == attunedId).AscensionTier = 1;
        db.PlayerEssences.Single(x => x.Id == attunedId).IsEvolved = true;
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = attunedId }]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var loadout = await service.ResolveAsync(characterId, CancellationToken.None);

        Assert.Single(loadout.EquippedEssences);
        Assert.Single(loadout.ActiveAbilities);
        Assert.Single(loadout.PassiveAbilities);
        Assert.Single(loadout.AttributeModifiers);
        Assert.Contains("Species.Beast", loadout.Tags);
        Assert.Contains("Mechanic.Execute", loadout.Tags);
        Assert.Equal("essence.test.active", loadout.ActiveAbilities.Single().AbilityDefinitionId);
        Assert.Equal(attunedId, loadout.ActiveAbilities.Single().SourcePlayerEssenceId);
        Assert.Equal(3, loadout.ActiveAbilities.Single().EssenceLevel);
        Assert.Equal(180, loadout.ActiveAbilities.Single().Cooldown);
        Assert.Equal(14, loadout.AttributeModifiers.Single().Amount);
    }

    [Fact]
    public async Task Attuned_combat_abilities_map_reusable_effect_and_condition_primitives()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, "essence.utility", level: 1);
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = essenceId }]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, definitions: new SingleDefinitionRepository(UtilityDefinition()));

        var active = (await service.GetAttunedCombatAbilitiesAsync(characterId, CancellationToken.None))
            .Single(x => x.Definition.Type == CombatAbilityType.Active)
            .Definition;

        var operations = active.Triggers.Single().Actions
            .Select(x => Assert.IsType<CombatEffectAction>(x.Action).Operation)
            .ToList();
        Assert.Contains(CombatEffectOperation.RemoveStatus, operations);
        Assert.Contains(CombatEffectOperation.Cleanse, operations);
        Assert.Contains(CombatEffectOperation.Summon, operations);
        Assert.Contains(CombatEffectOperation.TriggerSecondaryEffect, operations);
        Assert.Equal(2, operations.Count(x => x == CombatEffectOperation.RestoreResource));
        Assert.Equal(1, operations.Count(x => x == CombatEffectOperation.ModifyAttribute));
        Assert.Equal(4, operations.Count(x => x == CombatEffectOperation.Damage));
        Assert.Contains(active.Triggers.Single().Actions, x => x.Condition is CombatantStatusCondition);
        Assert.Contains(active.Triggers.Single().Actions, x => x.Condition is CombatantTagCondition);
    }

    [Fact]
    public async Task Attuned_combat_abilities_map_extended_target_selectors_and_conditions()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, "essence.extended", level: 1);
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = essenceId }]
        });
        await db.SaveChangesAsync();
        var definition = FakeDefinitionRepository.CreateDefinition("essence.extended", "monster.extended");
        definition.ActiveAbility.Targeting = AbilityTargetSelector.TwoEnemies;
        definition.ActiveAbility.Effects[0].Target = AbilityTargetSelector.TwoEnemies;
        definition.ActiveAbility.Effects[0].Conditions =
        [
            new() { Type = AbilityConditionType.SourceHealthAbovePercent, Value = 20 },
            new() { Type = AbilityConditionType.ChanceRoll, Value = 25 }
        ];
        var service = CreateService(db, definitions: new SingleDefinitionRepository(definition));

        var active = (await service.GetAttunedCombatAbilitiesAsync(characterId, CancellationToken.None))
            .Single(x => x.Definition.Type == CombatAbilityType.Active)
            .Definition;

        var action = active.Triggers.Single().Actions.Single();
        Assert.Equal(CombatTargeting.TwoEnemies, action.Targeting);
        Assert.Equal(25, action.Chance);
        Assert.IsType<CombatantHealthCondition>(action.Condition);
    }

    [Fact]
    public void Combat_event_bus_rejects_recursive_dispatch_loops()
    {
        var bus = new CombatEventBus();
        var combatEvent = new CombatEvent { Type = TriggerEvent.OnAbilityUsed };
        bus.Subscribe(_ => bus.Publish(combatEvent));

        var exception = Assert.Throws<InvalidOperationException>(() => bus.Publish(combatEvent));

        Assert.Contains("maximum depth", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateCreatureCombatEntities_adds_monster_tags_from_essence_definition()
    {
        var definition = UtilityDefinition();
        definition.SourceMonsterId = "monster.utility_beast";
        definition.Tags = ["Species.Beast", "Role.Support", "Element.Physical"];
        var service = new CombatSetupService(
            new NoopCreatureScaler(),
            new EmptyEssenceCombatLoadoutResolver(),
            new SingleDefinitionRepository(definition));

        var entities = service.CreateCreatureCombatEntities([new Creature { Name = "Utility Beast" }], new Area());

        var entity = Assert.Single(entities);
        Assert.Equal("monster.utility_beast", entity.SourceMonsterId);
        Assert.Contains("Species.Beast", entity.Tags);
        Assert.Contains("Role.Support", entity.Tags);
    }

    [Fact]
    public async Task PrepareEntitiesForCombat_applies_source_monster_essence_to_creatures()
    {
        await using var db = CreateDb();
        var definition = FakeDefinitionRepository.CreateDefinition("essence.utility", "monster.utility_beast");
        var essenceService = CreateService(db, definitions: new SingleDefinitionRepository(definition));
        var service = new CombatSetupService(
            new NoopCreatureScaler(),
            essenceService,
            new SingleDefinitionRepository(definition));
        var creature = new Creature { Name = "Utility Beast", Level = 4, Tier = 4 };
        var combatEntity = service.CreateCreatureCombatEntities([creature], new Area()).Single();

        await service.PrepareEntitiesForCombat([combatEntity]);

        Assert.Contains(combatEntity.Abilities, x => x.Definition.Id == "essence.utility.active" && x.Definition.Type == CombatAbilityType.Active);
        Assert.Contains(combatEntity.Abilities, x => x.Definition.Id == "essence.utility.passive" && x.Definition.Type == CombatAbilityType.Passive);
        Assert.Contains("Species.Beast", combatEntity.Tags);
        var modifier = Assert.Single(combatEntity.TemporaryModifiers, x => x.AttributeType == AttributeType.Power);
        Assert.Equal(5, modifier.Amount);
    }

    [Fact]
    public void Combat_stats_ignores_ability_use_events_without_stat_contribution()
    {
        var aggregator = new CombatStatsAggregator();

        var stats = aggregator.Aggregate(
        [
            new CombatLogItem
            {
                ActorId = "enemy",
                TargetId = "player",
                Source = "ability.essence.cave_bat.screech",
                EventType = EventType.AbilityUse,
                Details = "Cave Bat used Screech"
            },
            new CombatLogItem
            {
                ActorId = "enemy",
                TargetId = "player",
                Source = "Screech",
                EventType = EventType.Damage,
                Magnitude = 54
            }
        ]);

        var enemy = stats.Single(x => x.EntityId == "enemy");
        var ability = Assert.Single(enemy.Abilities);
        Assert.Equal("Screech", ability.Name);
        Assert.DoesNotContain(enemy.Abilities, x => x.Name == "ability.essence.cave_bat.screech");
    }

    [Fact]
    public async Task PrepareEntitiesForCombat_applies_essence_loadout_to_player_combat_state()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, "essence.test", level: 2);
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = essenceId }]
        });
        var character = db.Characters.Single(x => x.Id == characterId);
        character.BaseAttributes =
        [
            new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 100 },
            new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.Precision, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.Spirit, Value = 10 }
        ];
        await db.SaveChangesAsync();
        var essenceService = CreateService(db);
        var setup = new CombatSetupService(
            new NoopCreatureScaler(),
            essenceService,
            new FakeDefinitionRepository());
        var combatEntity = setup.CreatePlayerCombatEntities([character]).Single();

        await setup.PrepareEntitiesForCombat([combatEntity]);

        Assert.Contains(combatEntity.Abilities, x => x.Definition.Id == "essence.test.active" && x.Definition.Type == CombatAbilityType.Active);
        Assert.Contains(combatEntity.Abilities, x => x.Definition.Id == "essence.test.passive" && x.Definition.Type == CombatAbilityType.Passive);
        Assert.Contains("Species.Beast", combatEntity.Tags);
        Assert.Equal(13, combatEntity.CombatAttributes[AttributeType.Power]);
    }

    [Fact]
    public async Task SpendDust_respects_tier_cap_and_only_spends_applied_dust()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, level: 9);
        await AddInventoryQuantityAsync(db, characterId, "soul_dust", 100);
        var service = CreateService(db);

        var result = await service.SpendEssenceDustAsync(characterId, essenceId, 100, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        Assert.True(result.ReachedTierCap);
        Assert.Equal(10, db.PlayerEssences.Single(x => x.Id == essenceId).Level);
        Assert.Equal(0, db.PlayerEssences.Single(x => x.Id == essenceId).CurrentXp);
        Assert.Equal(96, await InventoryQuantityAsync(db, characterId, "soul_dust"));
    }

    [Fact]
    public async Task Ascend_and_evolve_require_and_consume_items()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, level: 10);
        await AddInventoryQuantityAsync(db, characterId, "item.monster_core.tier_1", 1);
        await AddInventoryQuantityAsync(db, characterId, "item.evolution_catalyst.test", 1);
        var service = CreateService(db);

        var ascend = await service.AscendEssenceAsync(characterId, essenceId, CancellationToken.None);
        await db.SaveChangesAsync();
        var evolve = await service.EvolveEssenceAsync(characterId, essenceId, CancellationToken.None);
        await db.SaveChangesAsync();

        var essence = db.PlayerEssences.Single(x => x.Id == essenceId);
        Assert.True(ascend.Succeeded);
        Assert.True(evolve.Succeeded);
        Assert.Equal(1, essence.AscensionTier);
        Assert.True(essence.IsEvolved);
        Assert.Equal(0, await InventoryQuantityAsync(db, characterId, "item.monster_core.tier_1"));
        Assert.Equal(0, await InventoryQuantityAsync(db, characterId, "item.evolution_catalyst.test"));
    }

    [Fact]
    public async Task Resonance_increases_on_failed_roll_and_resets_on_drop()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var service = CreateService(db, new QueueRandomProvider(0.99, 0.0));

        var failed = await service.RollMonsterEssenceDropAsync(characterId, "monster.test", true, CancellationToken.None);
        await db.SaveChangesAsync();
        var dropped = await service.RollMonsterEssenceDropAsync(characterId, "monster.test", true, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(failed.Dropped);
        Assert.Equal(1, failed.ResonanceValue);
        Assert.True(dropped.Dropped);
        Assert.Equal(0, dropped.ResonanceValue);
        Assert.Equal(0, db.MonsterResonances.Single().ResonanceValue);
    }

    [Fact]
    public async Task RollEssenceDrops_creates_unbound_item_for_successful_monster_roll()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        db.ItemBases.Add(new EssenceItemBase
        {
            Id = "item.essence.test",
            Name = "Test Essence",
            ItemType = ItemType.Essence,
            EssenceDefinitionId = "essence.test"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, new QueueRandomProvider(0.0));

        var drops = await service.RollEssenceDropsAsync(characterId, [new Creature { Name = "Test" }], true, CancellationToken.None);

        Assert.Single(drops);
        Assert.Equal("item.essence.test", drops.Single().ItemInstance.ItemBaseId);
        Assert.IsType<EssenceItemInstance>(drops.Single().ItemInstance);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static EssenceSystemService CreateService(LLDbContext db, IRandomProvider? random = null, IEssenceDefinitionRepository? definitions = null)
    {
        definitions ??= new FakeDefinitionRepository();
        return new EssenceSystemService(
            new EssenceRepository(db),
            new InventoryRepository(db),
            new ItemBaseRepository(db),
            definitions,
            new EssenceProgressionService(),
            new EssenceSlotUnlockService(),
            new EssenceLoadoutLimitService(),
            random ?? new QueueRandomProvider(0.99));
    }

    private static EssenceDefinition UtilityDefinition() => new()
    {
        Id = "essence.utility",
        SourceMonsterId = "monster.utility",
        Name = "Utility",
        Tags = ["Species.Beast", "Role.Support"],
        ActiveAbility = new AbilityDefinition
        {
            Id = "essence.utility.active",
            Kind = AbilityDefinitionKind.Active,
            Name = "Utility Active",
            Effects =
            [
                new() { Id = "remove", Type = AbilityEffectType.RemoveStatus, Status = "Burn" },
                new() { Id = "cleanse", Type = AbilityEffectType.Cleanse, Target = AbilityTargetSelector.Self },
                new() { Id = "summon", Type = AbilityEffectType.Summon, Status = "wolf" },
                new() { Id = "taunt", Type = AbilityEffectType.Taunt, Target = AbilityTargetSelector.Self, Scaling = new AbilityScalingFormula { BaseValue = 1 } },
                new() { Id = "reflect", Type = AbilityEffectType.ReflectDamage, Scaling = new AbilityScalingFormula { BaseValue = 1 } },
                new() { Id = "absorb", Type = AbilityEffectType.AbsorbDamage, Target = AbilityTargetSelector.Self, Scaling = new AbilityScalingFormula { BaseValue = 1 } },
                new() { Id = "secondary", Type = AbilityEffectType.TriggerSecondaryEffect, Status = "secondary.effect" },
                new() { Id = "heal", Type = AbilityEffectType.Heal, Target = AbilityTargetSelector.Self, Scaling = new AbilityScalingFormula { BaseValue = 1 } },
                new() { Id = "damage", Type = AbilityEffectType.Damage, Scaling = new AbilityScalingFormula { BaseValue = 1 }, Conditions = [new() { Type = AbilityConditionType.TargetHasStatus, Status = "Burn" }] },
                new() { Id = "tagged", Type = AbilityEffectType.Damage, Scaling = new AbilityScalingFormula { BaseValue = 1 }, Conditions = [new() { Type = AbilityConditionType.TargetHasTag, Tag = "Role.Tank" }] },
                new() { Id = "species", Type = AbilityEffectType.Damage, Scaling = new AbilityScalingFormula { BaseValue = 1 }, Conditions = [new() { Type = AbilityConditionType.IsSpecies, Tag = "Beast" }] }
            ]
        },
        PassiveAbility = new AbilityDefinition
        {
            Id = "essence.utility.passive",
            Kind = AbilityDefinitionKind.Passive,
            Name = "Utility Passive",
            Triggers = [new() { Type = "Trigger.OnHit" }],
            Effects = [new() { Id = "passive.damage", Type = AbilityEffectType.Damage, Scaling = new AbilityScalingFormula { BaseValue = 1 } }]
        },
        Evolution = new EssenceEvolutionDefinition
        {
            Id = "evolution.utility",
            RequiredCatalystItemId = "item.evolution_catalyst.test"
        }
    };

    private static async Task<Guid> SeedCharacterAndInventoryAsync(LLDbContext db, int level = 10)
    {
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character { Id = characterId, UserId = Guid.NewGuid(), Name = "Test", Level = level });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        await SeedStackableItemBaseAsync(db, "soul_dust");
        await SeedStackableItemBaseAsync(db, "item.monster_core.tier_1");
        await SeedStackableItemBaseAsync(db, "item.evolution_catalyst.test");
        await db.SaveChangesAsync();
        return characterId;
    }

    private static async Task<Guid> AddEssenceItemAsync(LLDbContext db, Guid characterId, int quantity, int dust = 1)
    {
        var baseId = $"item.essence.test.{Guid.NewGuid():N}";
        var instanceId = Guid.NewGuid();
        db.ItemBases.Add(new EssenceItemBase
        {
            Id = baseId,
            Name = "Test Essence",
            ItemType = ItemType.Essence,
            Stackable = true,
            EssenceDefinitionId = "essence.test",
            DismantleDustAmount = dust
        });
        db.ItemInstances.Add(new EssenceItemInstance { Id = instanceId, ItemBaseId = baseId });
        db.InventoryItems.Add(new InventoryItem { InventoryId = characterId, ItemInstanceId = instanceId, Quantity = quantity });
        await db.SaveChangesAsync();
        return instanceId;
    }

    private static async Task<Guid> AddPlayerEssenceAsync(LLDbContext db, Guid characterId, string definitionId = "essence.test", int level = 1)
    {
        var essenceId = Guid.NewGuid();
        db.PlayerEssences.Add(new PlayerEssence
        {
            Id = essenceId,
            CharacterId = characterId,
            EssenceDefinitionId = definitionId,
            Level = level
        });
        await db.SaveChangesAsync();
        return essenceId;
    }

    private static async Task AddInventoryQuantityAsync(LLDbContext db, Guid characterId, string itemBaseId, int quantity)
    {
        await SeedStackableItemBaseAsync(db, itemBaseId);
        var instanceId = Guid.NewGuid();
        db.ItemInstances.Add(new ItemInstance { Id = instanceId, ItemBaseId = itemBaseId });
        db.InventoryItems.Add(new InventoryItem { InventoryId = characterId, ItemInstanceId = instanceId, Quantity = quantity });
        await db.SaveChangesAsync();
    }

    private static async Task SeedStackableItemBaseAsync(LLDbContext db, string itemBaseId)
    {
        if (await db.ItemBases.AnyAsync(x => x.Id == itemBaseId)) return;
        db.ItemBases.Add(new ItemBase { Id = itemBaseId, Name = itemBaseId, ItemType = ItemType.Resource, Stackable = true });
        await db.SaveChangesAsync();
    }

    private static async Task<int> InventoryQuantityAsync(LLDbContext db, Guid characterId, string itemBaseId) =>
        await db.InventoryItems
            .Include(x => x.ItemInstance)
            .Where(x => x.InventoryId == characterId && x.ItemInstance.ItemBaseId == itemBaseId)
            .SumAsync(x => x.Quantity);

    private sealed class FakeDefinitionRepository : IEssenceDefinitionRepository
    {
        private readonly IReadOnlyList<EssenceDefinition> _definitions =
        [
            CreateDefinition("essence.test", "monster.test"),
            CreateDefinition("essence.other", "monster.other")
        ];

        public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;

        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            _definitions.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public EssenceDefinition? GetByMonsterId(string monsterId) =>
            _definitions.FirstOrDefault(x => x.SourceMonsterId.Equals(monsterId, StringComparison.OrdinalIgnoreCase));

        public AbilityDefinition? GetAbilityById(string abilityId) =>
            _definitions.SelectMany(x => new[] { x.ActiveAbility, x.PassiveAbility })
                .FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));

        public static EssenceDefinition CreateDefinition(string id, string monsterId) => new()
        {
            Id = id,
            SourceMonsterId = monsterId,
            Name = id,
            ActiveAbilityId = $"{id}.active",
            PassiveAbilityId = $"{id}.passive",
            Tags = ["Species.Beast", "Role.Offensive"],
            AttributeBonuses =
            [
                new()
                {
                    Attribute = AttributeType.Power,
                    BaseValue = 2
                }
            ],
            ActiveAbility = new AbilityDefinition
            {
                Id = $"{id}.active",
                Kind = AbilityDefinitionKind.Active,
                Name = "Active",
                CooldownSeconds = 18,
                Tags = ["Effect.Ability"],
                Effects = [new() { Id = "effect.damage.main", Type = "Damage", Scaling = new AbilityScalingFormula { BaseValue = 10 } }]
            },
            PassiveAbility = new AbilityDefinition
            {
                Id = $"{id}.passive",
                Kind = AbilityDefinitionKind.Passive,
                Name = "Passive",
                Tags = ["Trigger.OnHit"],
                Triggers = [new() { Type = "Trigger.OnHit" }],
                Effects = [new() { Id = "effect.attribute.main", Type = "ModifyAttribute", Target = "Self", Attribute = "Power", Scaling = new AbilityScalingFormula { BaseValue = 1 } }]
            },
            Evolution = new EssenceEvolutionDefinition
            {
                Id = $"{id}.evolution",
                Name = "Evolution",
                RequiredAscensionTier = 1,
                RequiredCatalystItemId = "item.evolution_catalyst.test",
                AddsTags = ["Mechanic.Execute"],
                ActiveAbilityModifiers = [new() { Target = "effect.damage.main", Operation = "AddMultiplier", Value = 0.5 }]
            },
            Drop = new EssenceDropDefinition
            {
                BaseDropChance = 0.5,
                ResonanceGainPerFailedEligibleKill = 1,
                DropChanceBonusPerResonance = 0.25,
                MaxResonanceBonus = 0.25
            }
        };
    }

    private sealed class SingleDefinitionRepository(EssenceDefinition definition) : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [definition];
        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            definition.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase) ? definition : null;
        public EssenceDefinition? GetByMonsterId(string monsterId) =>
            definition.SourceMonsterId.Equals(monsterId, StringComparison.OrdinalIgnoreCase) ? definition : null;
        public AbilityDefinition? GetAbilityById(string abilityId) =>
            new[] { definition.ActiveAbility, definition.PassiveAbility }
                .FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class NoopCreatureScaler : ICreatureScaler
    {
        public void ApplyScaling(Creature creature, Area area)
        {
        }
    }

    private sealed class EmptyEssenceCombatLoadoutResolver : IEssenceCombatLoadoutResolver
    {
        public Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(Resolve(characterId, []));

        public EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences) =>
            new(characterId, equippedEssences.ToList(), [], [], [], new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private sealed class QueueRandomProvider(params double[] values) : IRandomProvider
    {
        private readonly Queue<double> _values = new(values);

        public double NextDouble() => _values.Count == 0 ? 0.99 : _values.Dequeue();
    }
}
