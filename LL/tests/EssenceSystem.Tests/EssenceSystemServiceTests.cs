using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using Domain.Components.Attributes;
using Domain.Models.Achievements;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Bonuses;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Essences;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Combat;
using Services.LL.Combat.Stats;
using Services.LL.Essences;
using Services.LL.Inventories;
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
    public async Task AbsorbUnboundEssence_infers_definition_from_item_base_id_when_missing()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var itemInstanceId = Guid.NewGuid();
        db.ItemBases.Add(new EssenceItemBase
        {
            Id = "item.essence.test",
            Name = "Test Essence",
            ItemType = ItemType.Essence,
            Stackable = true
        });
        db.ItemInstances.Add(new EssenceItemInstance { Id = itemInstanceId, ItemBaseId = "item.essence.test" });
        db.InventoryItems.Add(new InventoryItem { InventoryId = characterId, ItemInstanceId = itemInstanceId, Quantity = 1 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.AbsorbUnboundEssenceAsync(characterId, itemInstanceId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        var essence = Assert.Single(db.PlayerEssences.Where(x => x.CharacterId == characterId));
        Assert.Equal("essence.test", essence.EssenceDefinitionId);
        Assert.DoesNotContain(db.InventoryItems, x => x.ItemInstanceId == itemInstanceId);
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
    public async Task DismantleUnboundEssence_grants_extra_dust_for_duplicate_echo_bonus()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        await AddPlayerEssenceAsync(db, characterId, "essence.test");
        var itemInstanceId = await AddEssenceItemAsync(db, characterId, quantity: 1, dust: 3);
        var service = CreateService(
            db,
            new QueueRandomProvider(0.0),
            bonusService: new StaticBonusService(new Dictionary<BonusKind, double>
            {
                [BonusKind.DuplicateEssenceExtraMaterialChanceBps] = 10000
            }));

        var result = await service.DismantleUnboundEssenceAsync(characterId, itemInstanceId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.DustGained);
        Assert.Equal(4, await InventoryQuantityAsync(db, characterId, "soul_dust"));
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
    public async Task Loadouts_allow_one_essence_per_creature_and_revalidate_on_activation()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var firstVariantId = await AddPlayerEssenceAsync(db, characterId, "essence.test");
        var secondVariantId = await AddPlayerEssenceAsync(db, characterId, "essence.alternate");
        var otherCreatureId = await AddPlayerEssenceAsync(db, characterId, "essence.other");
        var lootTables = new StaticCreatureEssenceLootTableRepository(
        [
            new CreatureEssenceLootTableDefinition
            {
                CreatureId = "monster.test",
                BaseDropChance = 0.5,
                PassiveAbilityId = "essence.test.passive",
                Variants =
                [
                    new() { EssenceDefinitionId = "essence.test", ActiveAbilityId = "essence.test.active" },
                    new() { EssenceDefinitionId = "essence.alternate", ActiveAbilityId = "essence.alternate.active" }
                ]
            },
            CreateLootTable("monster.other", "essence.other")
        ]);
        var service = CreateService(db, creatureEssenceLootTables: lootTables);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveLoadoutAsync(
                characterId,
                new SaveEssenceLoadoutRequest(null, "Same creature", [new(0, firstVariantId), new(1, secondVariantId)]),
                CancellationToken.None));

        var allowedSavedLoadout = new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Different creatures",
            Slots =
            [
                new() { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = firstVariantId },
                new() { Id = Guid.NewGuid(), SlotIndex = 1, PlayerEssenceId = otherCreatureId }
            ]
        };
        var invalidSavedLoadout = new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Legacy invalid",
            Slots =
            [
                new() { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = firstVariantId },
                new() { Id = Guid.NewGuid(), SlotIndex = 1, PlayerEssenceId = secondVariantId }
            ]
        };
        db.EssenceLoadouts.AddRange(allowedSavedLoadout, invalidSavedLoadout);
        await db.SaveChangesAsync();

        var allowedActivation = await service.ActivateLoadoutAsync(characterId, allowedSavedLoadout.Id, CancellationToken.None);
        var activation = await service.ActivateLoadoutAsync(characterId, invalidSavedLoadout.Id, CancellationToken.None);

        Assert.Contains("same creature", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(allowedActivation.Succeeded);
        Assert.False(activation.Succeeded);
        Assert.Contains("same creature", activation.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task Attuned_essences_return_ability_specs_for_active_loadout_only()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db, level: 20);
        var attunedId = await AddPlayerEssenceAsync(db, characterId, "essence.test", level: 3);
        await AddPlayerEssenceAsync(db, characterId, "essence.other", level: 3);
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

        var abilities = await service.GetAttunedAbilitiesAsync(characterId, CancellationToken.None);

        Assert.Equal(2, abilities.Count);
        Assert.All(abilities, x => Assert.StartsWith("essence.test", x.Id));
        Assert.Contains(abilities, x => x.Kind == AbilitySpecKind.Active && x.CooldownTicks == 180);
        Assert.Contains(abilities, x => x.Kind == AbilitySpecKind.Passive && x.Triggers.Single().Event == AbilityTriggerEvent.OnHit);
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
        Assert.Single(loadout.AttributeModifiers);
        Assert.Contains("Species.Beast", loadout.Tags);
        Assert.Contains("Mechanic.Execute", loadout.Tags);
        Assert.Equal(2.16f, loadout.AttributeModifiers.Single().Amount, 2);
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
            new SingleDefinitionRepository(definition),
            new StaticCreatureEssenceLootTableRepository([CreateLootTable("monster.utility_beast", definition.Id)]));

        var entities = service.CreateCreatureCombatEntities([new Creature { Name = "Utility Beast" }], new Area());

        var entity = Assert.Single(entities);
        Assert.Equal("monster.utility_beast", entity.SourceMonsterId);
        Assert.Contains("Species.Beast", entity.Tags);
        Assert.Contains("Role.Support", entity.Tags);
    }

    [Fact]
    public void CreateCreatureCombatEntities_uses_one_shared_passive_and_every_active_variant()
    {
        var definitions = new FakeDefinitionRepository();
        var lootTable = new CreatureEssenceLootTableDefinition
        {
            CreatureId = "monster.test",
            BaseDropChance = 0.5,
            PassiveAbilityId = "essence.test.passive",
            Variants =
            [
                new() { EssenceDefinitionId = "essence.test", ActiveAbilityId = "essence.test.active", Weight = 1 },
                new() { EssenceDefinitionId = "essence.alternate", ActiveAbilityId = "essence.alternate.active", Weight = 1 }
            ]
        };
        var service = new CombatSetupService(
            new NoopCreatureScaler(),
            new EmptyEssenceCombatLoadoutResolver(),
            definitions,
            new StaticCreatureEssenceLootTableRepository([lootTable]));

        var entity = Assert.Single(service.CreateCreatureCombatEntities([new Creature { Name = "Test" }], new Area()));

        Assert.Equal(3, entity.NativeAbilityIds.Count);
        Assert.Contains("essence.test.passive", entity.NativeAbilityIds);
        Assert.Contains("essence.test.active", entity.NativeAbilityIds);
        Assert.Contains("essence.alternate.active", entity.NativeAbilityIds);
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
            new SingleDefinitionRepository(definition),
            new StaticCreatureEssenceLootTableRepository([CreateLootTable("monster.utility_beast", definition.Id)]));
        var creature = new Creature { Name = "Utility Beast", Level = 4, Tier = 4 };
        var combatEntity = service.CreateCreatureCombatEntities([creature], new Area()).Single();

        await service.PrepareEntitiesForCombat([combatEntity]);

        Assert.Contains(combatEntity.EquippedEssences, x => x.EssenceDefinitionId == "essence.utility");
        Assert.True(combatEntity.HasEquippedEssenceSnapshot);
        Assert.Contains("Species.Beast", combatEntity.Tags);
        var modifier = Assert.Single(combatEntity.TemporaryModifiers, x => x.AttributeType == AttributeType.Power);
        Assert.Equal(2.24f, modifier.Amount, 2);
    }

    [Fact]
    public void Combat_stats_counts_ability_use_events()
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
        Assert.Contains(enemy.Abilities, x =>
            x.Name == "ability.essence.cave_bat.screech"
            && x.Uses == 1
            && x.TotalDamage == 0);
        Assert.Contains(enemy.Abilities, x =>
            x.Name == "Screech"
            && x.Uses == 0
            && x.TotalDamage == 54);
    }

    [Fact]
    public void Combat_stats_groups_effect_output_by_stats_source()
    {
        var aggregator = new CombatStatsAggregator();

        var stats = aggregator.Aggregate(
        [
            new CombatLogItem
            {
                ActorId = "player",
                Source = "Sneak Attack",
                EventType = EventType.AbilityUse,
                Details = "Player used Sneak Attack"
            },
            new CombatLogItem
            {
                ActorId = "player",
                TargetId = "enemy",
                Source = "effect.initial.damage",
                StatsSource = "Sneak Attack",
                EventType = EventType.Damage,
                Magnitude = 33
            },
            new CombatLogItem
            {
                ActorId = "player",
                TargetId = "enemy",
                Source = "effect.poison.dot",
                StatsSource = "Sneak Attack",
                EventType = EventType.DamageOverTime,
                Magnitude = 14
            }
        ]);

        var player = stats.Single(x => x.EntityId == "player");
        var ability = Assert.Single(player.Abilities);
        Assert.Equal("Sneak Attack", ability.Name);
        Assert.Equal(1, ability.Uses);
        Assert.Equal(47, ability.TotalDamage);
        Assert.Equal(47, player.DamageDone);

        var enemy = stats.Single(x => x.EntityId == "enemy");
        Assert.Equal(47, enemy.DamageTaken);
    }

    [Fact]
    public void Combat_stats_assigns_team_metadata_and_balances_side_totals()
    {
        var aggregator = new CombatStatsAggregator();

        var stats = aggregator.Aggregate(
        [
            new CombatLogItem
            {
                ActorId = "enemy",
                TargetId = "player",
                Source = "Claw",
                EventType = EventType.Damage,
                Magnitude = 9
            },
            new CombatLogItem
            {
                ActorId = "enemy:summon:shadow",
                TargetId = "player",
                Source = "Shadow Burn",
                EventType = EventType.DamageOverTime,
                Magnitude = 5
            }
        ],
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["player"] = "Friendly",
            ["enemy"] = "Hostile",
            ["enemy:summon:shadow"] = "Hostile"
        });

        var friendlyDamageTaken = stats
            .Where(x => x.Team == "Friendly")
            .Sum(x => x.DamageTaken);
        var hostileDamageDone = stats
            .Where(x => x.Team == "Hostile")
            .Sum(x => x.DamageDone);

        Assert.Equal(14, friendlyDamageTaken);
        Assert.Equal(friendlyDamageTaken, hostileDamageDone);
        Assert.Equal("Hostile", stats.Single(x => x.EntityId == "enemy:summon:shadow").Team);
    }

    [Fact]
    public void Combat_stats_separates_self_damage_from_opponent_damage()
    {
        var aggregator = new CombatStatsAggregator();

        var stats = aggregator.Aggregate(
        [
            new CombatLogItem
            {
                ActorId = "player",
                TargetId = "enemy",
                Source = "Strike",
                EventType = EventType.Damage,
                Magnitude = 16
            },
            new CombatLogItem
            {
                ActorId = "player",
                TargetId = "player",
                Source = "Recoil",
                StatsSource = "Strike",
                EventType = EventType.Damage,
                Magnitude = 12
            },
            new CombatLogItem
            {
                ActorId = "enemy",
                TargetId = "player",
                Source = "Bite",
                EventType = EventType.Damage,
                Magnitude = 9
            }
        ],
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["player"] = "Friendly",
            ["enemy"] = "Hostile"
        });

        var player = stats.Single(x => x.EntityId == "player");
        var enemy = stats.Single(x => x.EntityId == "enemy");

        Assert.Equal(16, player.DamageDone);
        Assert.Equal(9, player.DamageTaken);
        Assert.Equal(12, player.SelfDamageDone);
        Assert.Equal(12, player.SelfDamageTaken);
        Assert.Equal(player.DamageDone, enemy.DamageTaken);
        Assert.Equal(enemy.DamageDone, player.DamageTaken);
        Assert.Equal(12, player.Abilities.Single(x => x.Name == "Strike").SelfDamage);
    }

    [Fact]
    public void Combat_stats_counts_marked_passive_outcomes_as_procs()
    {
        var aggregator = new CombatStatsAggregator();

        var stats = aggregator.Aggregate(
        [
            new CombatLogItem
            {
                ActorId = "imp",
                TargetId = "player",
                Source = "effect.hot_aura.damage",
                StatsSource = "Hot Aura",
                CountsAsActivation = true,
                EventType = EventType.Damage,
                Magnitude = 4
            },
            new CombatLogItem
            {
                ActorId = "archer",
                TargetId = "enemy",
                Source = "status.poisoned_arrow",
                StatsSource = "Poisoned Arrows",
                CountsAsActivation = true,
                EventType = EventType.StatusEffect,
                Magnitude = 1
            },
            new CombatLogItem
            {
                ActorId = "archer",
                TargetId = "enemy",
                Source = "effect.poison.dot",
                StatsSource = "Poisoned Arrows",
                EventType = EventType.Damage,
                Magnitude = 6
            }
        ]);

        var hotAura = stats.Single(x => x.EntityId == "imp").Abilities.Single();
        Assert.Equal("Hot Aura", hotAura.Name);
        Assert.Equal(1, hotAura.Uses);
        Assert.Equal(4, hotAura.TotalDamage);

        var poisonedArrows = stats.Single(x => x.EntityId == "archer").Abilities.Single();
        Assert.Equal("Poisoned Arrows", poisonedArrows.Name);
        Assert.Equal(1, poisonedArrows.Uses);
        Assert.Equal(6, poisonedArrows.TotalDamage);
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
            new FakeDefinitionRepository(),
            new StaticCreatureEssenceLootTableRepository([]));
        var combatEntity = setup.CreatePlayerCombatEntities([character]).Single();

        await setup.PrepareEntitiesForCombat([combatEntity]);

        Assert.Contains(combatEntity.EquippedEssences, x => x.Id == essenceId && x.EssenceDefinitionId == "essence.test");
        Assert.True(combatEntity.HasEquippedEssenceSnapshot);
        Assert.Contains("Species.Beast", combatEntity.Tags);
        Assert.Equal(12, combatEntity.CombatAttributes[AttributeType.Power]);
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
        Assert.Equal(84, await InventoryQuantityAsync(db, characterId, "soul_dust"));
    }

    [Fact]
    public async Task UpgradePotential_requires_current_level_cap_and_consumes_region_core()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, level: 9);
        await AddInventoryQuantityAsync(db, characterId, "item.essence_potential_core.region_1", 3);
        var service = CreateService(db);

        var tooEarly = await service.UpgradePotentialAsync(characterId, essenceId, CancellationToken.None);
        db.PlayerEssences.Single(x => x.Id == essenceId).Level = 10;
        var upgraded = await service.UpgradePotentialAsync(characterId, essenceId, CancellationToken.None);
        await db.SaveChangesAsync();

        var essence = db.PlayerEssences.Single(x => x.Id == essenceId);
        Assert.False(tooEarly.Succeeded);
        Assert.True(upgraded.Succeeded);
        Assert.Equal(2, essence.PotentialTier);
        Assert.Equal(0, await InventoryQuantityAsync(db, characterId, "item.essence_potential_core.region_1"));
    }

    [Fact]
    public async Task Ascension_does_not_raise_potential_level_cap()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, level: 10);
        await AddInventoryQuantityAsync(db, characterId, "item.monster_core.lesser", 6);
        await AddInventoryQuantityAsync(db, characterId, "soul_dust", 100);
        var service = CreateService(db);

        var ascend = await service.AscendEssenceAsync(characterId, essenceId, CancellationToken.None);
        var dust = await service.SpendEssenceDustAsync(characterId, essenceId, 100, CancellationToken.None);
        await db.SaveChangesAsync();

        var essence = db.PlayerEssences.Single(x => x.Id == essenceId);
        Assert.True(ascend.Succeeded);
        Assert.False(dust.Succeeded);
        Assert.Equal(1, essence.AscensionTier);
        Assert.Equal(1, essence.PotentialTier);
        Assert.Equal(10, essence.Level);
    }

    [Fact]
    public async Task Ascend_and_evolve_require_and_consume_items()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var essenceId = await AddPlayerEssenceAsync(db, characterId, level: 10);
        await AddInventoryQuantityAsync(db, characterId, "item.monster_core.lesser", 6);
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
        Assert.Equal(0, await InventoryQuantityAsync(db, characterId, "item.monster_core.lesser"));
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
        Assert.Equal(CreatureResonanceConstants.GainPerFailedEligibleKill, failed.ResonanceValue);
        Assert.True(dropped.Dropped);
        Assert.Equal(0.5 + CreatureResonanceConstants.DropChanceBonusPerPoint, dropped.EffectiveDropChance);
        Assert.Equal(0, dropped.ResonanceValue);
        Assert.Equal(0, db.CreatureResonances.Single().ResonanceValue);
    }

    [Fact]
    public async Task Resonance_failure_gain_uses_echo_memory_bonus()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var service = CreateService(
            db,
            new QueueRandomProvider(0.99),
            bonusService: new StaticBonusService(new Dictionary<BonusKind, double>
            {
                [BonusKind.EssencePityProgressionGainBps] = 2500
            }));

        var failed = await service.RollMonsterEssenceDropAsync(characterId, "monster.test", true, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(failed.Dropped);
        var expectedResonance = CreatureResonanceConstants.GainPerFailedEligibleKill * 1.25;
        Assert.Equal(expectedResonance, failed.ResonanceValue);
        Assert.Equal(expectedResonance, db.CreatureResonances.Single().ResonanceValue);
    }

    [Fact]
    public async Task Resonance_drop_chance_bonus_reaches_its_shared_cap_after_twelve_thousand_failed_kills()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        db.CreatureResonances.Add(new CreatureResonance
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            CreatureId = "monster.test",
            ResonanceValue = CreatureResonanceConstants.FailedEligibleKillsToMaximumBonus - 1
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, new QueueRandomProvider(0.99, 0.99));

        var finalProgressionRoll = await service.RollMonsterEssenceDropAsync(characterId, "monster.test", true, CancellationToken.None);
        var cappedRoll = await service.RollMonsterEssenceDropAsync(characterId, "monster.test", true, CancellationToken.None);

        Assert.False(finalProgressionRoll.Dropped);
        Assert.Equal(CreatureResonanceConstants.FailedEligibleKillsToMaximumBonus, finalProgressionRoll.ResonanceValue);
        Assert.Equal(
            0.5 + CreatureResonanceConstants.MaximumDropChanceBonus - CreatureResonanceConstants.DropChanceBonusPerPoint,
            finalProgressionRoll.EffectiveDropChance,
            precision: 12);
        Assert.False(cappedRoll.Dropped);
        Assert.Equal(0.5 + CreatureResonanceConstants.MaximumDropChanceBonus, cappedRoll.EffectiveDropChance, precision: 12);
    }

    [Fact]
    public async Task Focused_monster_drop_bonus_only_applies_to_focused_creature()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var service = CreateService(
            db,
            new QueueRandomProvider(0.6, 0.6),
            bonusService: new StaticBonusService(new Dictionary<BonusKind, double>
            {
                [BonusKind.FocusedMonsterEssenceDropRateRelativeBps] = 5000
            }),
            creatureArchiveService: new StaticCreatureArchiveService("monster.test"));

        var focused = await service.RollMonsterEssenceDropAsync(characterId, "monster.test", true, CancellationToken.None);
        var unfocused = await service.RollMonsterEssenceDropAsync(characterId, "monster.other", true, CancellationToken.None);

        Assert.True(focused.Dropped);
        Assert.Equal(0.75, focused.EffectiveDropChance);
        Assert.False(unfocused.Dropped);
        Assert.Equal(0.5, unfocused.EffectiveDropChance);
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

    [Fact]
    public async Task Successful_creature_drop_uses_weighted_essence_loot_table()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var lootTables = new StaticCreatureEssenceLootTableRepository(
        [
            new CreatureEssenceLootTableDefinition
            {
                CreatureId = "monster.test",
                BaseDropChance = 1,
                PassiveAbilityId = "essence.test.passive",
                Variants =
                [
                    new() { EssenceDefinitionId = "essence.test", ActiveAbilityId = "essence.test.active", Weight = 1 },
                    new() { EssenceDefinitionId = "essence.alternate", ActiveAbilityId = "essence.alternate.active", Weight = 3 }
                ]
            }
        ]);
        var service = CreateService(
            db,
            new QueueRandomProvider(0, 0.9),
            creatureEssenceLootTables: lootTables);

        var result = await service.RollMonsterEssenceDropAsync(
            characterId,
            "monster.test",
            true,
            CancellationToken.None);

        Assert.True(result.Dropped);
        Assert.Equal("essence.alternate", result.EssenceDefinitionId);
    }

    [Fact]
    public async Task RollEssenceDrops_reuses_bonus_factors_and_focus_lookups_for_defeated_creature_batch()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAndInventoryAsync(db);
        var bonusService = new StaticBonusService(new Dictionary<BonusKind, double>
        {
            [BonusKind.FocusedMonsterEssenceDropRateRelativeBps] = 5000
        });
        var creatureArchiveService = new StaticCreatureArchiveService("monster.test");
        var service = CreateService(
            db,
            new QueueRandomProvider(0.99, 0.99, 0.99),
            bonusService: bonusService,
            creatureArchiveService: creatureArchiveService);

        var drops = await service.RollEssenceDropsAsync(
            characterId,
            [
                new Creature { Name = "Test" },
                new Creature { Name = "Test" },
                new Creature { Name = "Other" }
            ],
            true,
            CancellationToken.None);

        Assert.Empty(drops);
        Assert.Equal(1, bonusService.GetAggregatedCallCount);
        Assert.Equal(2, creatureArchiveService.FocusLookupCount);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static EssenceSystemService CreateService(
        LLDbContext db,
        IRandomProvider? random = null,
        IEssenceDefinitionRepository? definitions = null,
        ICreatureEssenceLootTableRepository? creatureEssenceLootTables = null,
        IBonusService? bonusService = null,
        ICreatureArchiveService? creatureArchiveService = null)
    {
        definitions ??= new FakeDefinitionRepository();
        creatureEssenceLootTables ??= new StaticCreatureEssenceLootTableRepository(
        [
            CreateLootTable("monster.test", "essence.test"),
            CreateLootTable("monster.other", "essence.other")
        ]);
        return new EssenceSystemService(
            new EssenceRepository(db),
            new InventoryRepository(db),
            new ItemBaseRepository(db),
            definitions,
            creatureEssenceLootTables,
            new EssenceProgressionService(),
            new EssenceSlotUnlockService(),
            new EssenceLoadoutLimitService(),
            new InventoryItemFactory(),
            random ?? new QueueRandomProvider(0.99),
            new NoopGameEventOutbox(),
            bonusService: bonusService,
            creatureArchiveService: creatureArchiveService);
    }

    private static CreatureEssenceLootTableDefinition CreateLootTable(string creatureId, string essenceDefinitionId) =>
        new()
        {
            CreatureId = creatureId,
            BaseDropChance = 0.5,
            PassiveAbilityId = $"{essenceDefinitionId}.passive",
            Variants = [new() { EssenceDefinitionId = essenceDefinitionId, ActiveAbilityId = $"{essenceDefinitionId}.active", Weight = 1 }]
        };

    private static EssenceDefinition UtilityDefinition() => new()
    {
        Id = "essence.utility",
        SourceMonsterId = "monster.utility",
        Name = "Utility",
        Tags = ["Species.Beast", "Role.Support"],
        ActiveAbilityId = "essence.utility.active",
        PassiveAbilityId = "essence.utility.passive",
        ActiveAbility = new AbilitySpec
        {
            Id = "essence.utility.active",
            Kind = AbilitySpecKind.Active,
            Name = "Utility Active",
            CooldownTicks = 100,
            Effects =
            [
                new() { Id = "remove", Operation = AbilityEffectOperation.RemoveStatus, StatusId = "Burn" },
                new() { Id = "cleanse", Operation = AbilityEffectOperation.Cleanse, Target = AbilityTargetSelector.Self },
                new() { Id = "summon", Operation = AbilityEffectOperation.Summon, SummonId = "wolf" },
                new() { Id = "heal", Operation = AbilityEffectOperation.Heal, Target = AbilityTargetSelector.Self, BaseValue = 1 },
                new() { Id = "damage", Operation = AbilityEffectOperation.Damage, BaseValue = 1, Conditions = [new() { Type = AbilityConditionType.HasStatus, StatusId = "Burn" }] },
                new() { Id = "tagged", Operation = AbilityEffectOperation.Damage, BaseValue = 1, Conditions = [new() { Type = AbilityConditionType.HasTag, Tag = "Role.Tank" }] }
            ]
        },
        PassiveAbility = new AbilitySpec
        {
            Id = "essence.utility.passive",
            Kind = AbilitySpecKind.Passive,
            Name = "Utility Passive",
            Triggers = [new() { Event = AbilityTriggerEvent.OnHit }],
            Effects = [new() { Id = "passive.damage", Operation = AbilityEffectOperation.Damage, BaseValue = 1 }]
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
        await SeedStackableItemBaseAsync(db, "item.monster_core.lesser");
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
            CreateDefinition("essence.alternate", "monster.test", "essence.test.passive"),
            CreateDefinition("essence.other", "monster.other")
        ];

        public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;

        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            _definitions.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public AbilitySpec? GetAbilityById(string abilityId) =>
            _definitions.SelectMany(x => new[] { x.ActiveAbility, x.PassiveAbility })
                .FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<AbilitySpec> GetAllAbilities() =>
            _definitions.SelectMany(x => new[] { x.ActiveAbility, x.PassiveAbility }).ToList();

        public static EssenceDefinition CreateDefinition(string id, string monsterId, string? passiveAbilityId = null) => new()
        {
            Id = id,
            SourceMonsterId = monsterId,
            Name = id,
            ActiveAbilityId = $"{id}.active",
            PassiveAbilityId = passiveAbilityId ?? $"{id}.passive",
            Tags = ["Species.Beast", "Role.Offensive"],
            AttributeBonuses =
            [
                new()
                {
                    Attribute = AttributeType.Power,
                    BaseValue = 2
                }
            ],
            ActiveAbility = new AbilitySpec
            {
                Id = $"{id}.active",
                Kind = AbilitySpecKind.Active,
                Name = "Active",
                CooldownTicks = 180,
                Tags = ["Effect.Ability"],
                Effects = [new() { Id = "effect.damage.main", Operation = AbilityEffectOperation.Damage, BaseValue = 10 }]
            },
            PassiveAbility = new AbilitySpec
            {
                Id = passiveAbilityId ?? $"{id}.passive",
                Kind = AbilitySpecKind.Passive,
                Name = "Passive",
                Tags = ["Trigger.OnHit"],
                Triggers = [new() { Event = AbilityTriggerEvent.OnHit }],
                Effects = [new() { Id = "effect.attribute.main", Operation = AbilityEffectOperation.ModifyAttribute, Target = AbilityTargetSelector.Self, Attribute = AttributeType.Power, BaseValue = 1 }]
            },
            Evolution = new EssenceEvolutionDefinition
            {
                Id = $"{id}.evolution",
                Name = "Evolution",
                RequiredAscensionTier = 1,
                RequiredCatalystItemId = "item.evolution_catalyst.test",
                AddsTags = ["Mechanic.Execute"],
                ActiveAbilityModifiers = [new() { Target = "effect.damage.main", Operation = "AddMultiplier", Value = 0.5 }]
            }
        };
    }

    private sealed class StaticCreatureEssenceLootTableRepository(
        IReadOnlyList<CreatureEssenceLootTableDefinition> tables) : ICreatureEssenceLootTableRepository
    {
        public IReadOnlyList<CreatureEssenceLootTableDefinition> GetAll() => tables;

        public CreatureEssenceLootTableDefinition? GetByCreatureId(string creatureId) =>
            tables.FirstOrDefault(x => x.CreatureId.Equals(creatureId, StringComparison.OrdinalIgnoreCase));

        public CreatureEssenceLootTableDefinition? GetByEssenceDefinitionId(string essenceDefinitionId) =>
            tables.FirstOrDefault(table => table.Variants.Any(variant =>
                variant.EssenceDefinitionId.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class SingleDefinitionRepository(EssenceDefinition definition) : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [definition];
        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            definition.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase) ? definition : null;
        public AbilitySpec? GetAbilityById(string abilityId) =>
            new[] { definition.ActiveAbility, definition.PassiveAbility }
                .FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<AbilitySpec> GetAllAbilities() =>
            [definition.ActiveAbility, definition.PassiveAbility];
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
            new(characterId, equippedEssences.ToList(), [], new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private sealed class QueueRandomProvider(params double[] values) : IRandomProvider
    {
        private readonly Queue<double> _values = new(values);

        public double NextDouble() => _values.Count == 0 ? 0.99 : _values.Dequeue();
    }

    private sealed class StaticBonusService(IReadOnlyDictionary<BonusKind, double> bonuses) : IBonusService
    {
        public int GetAggregatedCallCount { get; private set; }

        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(
            Guid characterId,
            DateTimeOffset now,
            CancellationToken ct = default)
        {
            GetAggregatedCallCount++;
            return ValueTask.FromResult(bonuses);
        }
    }

    private sealed class StaticCreatureArchiveService(string focusedCreatureId) : ICreatureArchiveService
    {
        public int FocusLookupCount { get; private set; }

        public Task RecordDefeatedCreaturesAsync(
            Guid characterId,
            IReadOnlyCollection<Creature> creatures,
            DateTimeOffset defeatedAtUtc,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<CreatureArchive> GetCreatureArchiveAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new CreatureArchive([], true, null, null));

        public Task<EssenceCodex> GetEssenceCodexAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new EssenceCodex([]));

        public Task<CreatureArchive> SetEssenceFocusAsync(Guid characterId, string? creatureId, CancellationToken cancellationToken) =>
            Task.FromResult(new CreatureArchive([], true, null, null));

        public Task<bool> IsEssenceFocusAsync(Guid characterId, string creatureId, CancellationToken cancellationToken)
        {
            FocusLookupCount++;
            return Task.FromResult(creatureId.Equals(focusedCreatureId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private sealed class NoopGameEventOutbox : IGameEventOutbox
    {
        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
