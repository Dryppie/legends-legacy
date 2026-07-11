using Application.Interfaces.Services.AdminDashboard;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Boons;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Routes;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences.Definitions;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Orchestration.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Dungeons;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Reader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class DungeonRogueliteStateTests
{
    [Fact]
    public void Pressure_delta_clamps_and_updates_reward_multiplier()
    {
        var run = CreateRun();
        var service = new DungeonPressureService(new SingleDungeonDefinitions(new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Name = "Test Dungeon"
        }));

        var high = service.ApplyPressureDelta(run, 200);

        Assert.Equal(100, high.Pressure);
        Assert.Equal(175, high.RewardMultiplierPercent);
        Assert.Contains("maximum", high.ActiveThresholdIds);

        var low = service.ApplyPressureDelta(run, -300);

        Assert.Equal(0, low.Pressure);
        Assert.Equal(100, low.RewardMultiplierPercent);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(24, 100)]
    [InlineData(25, 110)]
    [InlineData(50, 125)]
    [InlineData(75, 145)]
    [InlineData(100, 175)]
    public void Pressure_thresholds_calculate_reward_multiplier(int pressure, int expectedMultiplier)
    {
        var service = new DungeonPressureService(new SingleDungeonDefinitions(new DungeonDefinition()));

        Assert.Equal(expectedMultiplier, service.CalculateRewardMultiplierPercent(pressure));
    }

    [Fact]
    public void Custom_mechanic_thresholds_update_reward_multiplier_and_state()
    {
        var run = CreateRun();
        var service = new DungeonPressureService(new SingleDungeonDefinitions(new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Name = "Test Dungeon",
            Mechanic = new DungeonMechanicDefinition
            {
                Id = "heat",
                DisplayName = "Heat",
                MaxValue = 80,
                Thresholds =
                [
                    new()
                    {
                        Id = "heated",
                        Value = 40,
                        Description = "The room is heating up.",
                        RewardMultiplierBonusPercent = 30
                    }
                ]
            }
        }));

        var result = service.ApplyPressureDelta(run, 45);

        Assert.Equal(45, result.Pressure);
        Assert.Equal(130, result.RewardMultiplierPercent);
        Assert.Equal("heat", run.State.MechanicId);
        Assert.Equal("Heat", run.State.MechanicDisplayName);
        Assert.Equal(80, run.State.MechanicMaxValue);
        var threshold = Assert.Single(run.State.CurrentMechanicThresholds);
        Assert.Equal("heated", threshold.Id);
        Assert.Equal("The room is heating up.", threshold.Description);
    }

    [Fact]
    public void Route_generation_creates_choices_after_current_room_is_completed_and_selection_moves_forward()
    {
        var run = CreateRun();
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        run.Rooms.Add(new RoomInstance
        {
            Id = Guid.NewGuid(),
            RoomIndex = 1,
            Type = RoomType.Event
        });

        var service = CreateRouteService();

        var routes = service.GenerateRouteOptions(run);
        Assert.NotEmpty(routes);

        var selected = service.ChooseRoute(run, routes[0].Id);

        Assert.Equal(1, selected.RoomIndex);
        Assert.Empty(run.State.CurrentRouteOptions);
        Assert.Equal(1, run.CurrentRoomIndex);
    }

    [Fact]
    public void Route_generation_waits_until_current_room_is_completed()
    {
        var run = CreateRun();
        run.Rooms.Add(new RoomInstance
        {
            Id = Guid.NewGuid(),
            RoomIndex = 1,
            Type = RoomType.Event
        });
        var service = CreateRouteService();

        var routes = service.GenerateRouteOptions(run);

        Assert.Empty(routes);
        Assert.Empty(run.State.CurrentRouteOptions);
        Assert.Equal(0, run.CurrentRoomIndex);
    }

    [Fact]
    public void Hidden_route_selection_marks_route_taken_and_consumes_reveal()
    {
        var run = CreateRun();
        run.State.Flags["hidden_route_revealed"] = 1;
        run.State.CurrentRouteOptions.Add(new DungeonRouteOption
        {
            Id = "hidden:1",
            RoomIndex = 1,
            DisplayName = "Hidden Passage",
            RoomType = RoomType.Event,
            PressureDelta = -8
        });
        var service = CreateRouteService();

        var route = service.ChooseRoute(run, "hidden:1");

        Assert.Equal("hidden:1", route.Id);
        Assert.Equal(1, run.CurrentRoomIndex);
        Assert.Equal(1, run.State.Flags["hidden_route_taken"]);
        Assert.False(run.State.Flags.ContainsKey("hidden_route_revealed"));
        Assert.Empty(run.State.CurrentRouteOptions);
    }

    [Fact]
    public void Unique_boon_selection_cannot_exceed_stack_limit()
    {
        var run = CreateRun();
        var service = CreateBoonService();
        var choice = new DungeonBoonChoiceOption
        {
            Id = "hunter_focus",
            Name = "Hunter's Focus",
            Description = "Gain an edge against elites and bosses.",
            Rarity = DungeonBoonRarity.Common.ToString()
        };

        run.State.CurrentBoonChoices.Add(choice);
        service.ChooseBoon(run, choice.Id);
        run.State.CurrentBoonChoices.Add(choice);
        var exception = Assert.Throws<InvalidOperationException>(() => service.ChooseBoon(run, choice.Id));

        Assert.Single(run.State.ActiveBoonIds);
        Assert.Equal("The selected boon has already reached its stack limit.", exception.Message);
    }

    [Fact]
    public void Stackable_boon_selection_adds_multiple_stacks_until_limit()
    {
        var run = CreateRun();
        var choice = new DungeonBoonChoiceOption
        {
            Id = "stacking_edge",
            Name = "Stacking Edge",
            Description = "A test boon that can stack.",
            Rarity = DungeonBoonRarity.Common.ToString()
        };
        var service = new DungeonBoonService(new StaticBoonDefinitions(
        [
            new()
            {
                Id = "stacking_edge",
                Name = "Stacking Edge",
                Description = "A test boon that can stack.",
                Rarity = DungeonBoonRarity.Common,
                MaxStacks = 2,
                AttributeModifiers =
                [
                    new EssenceAttributeModifier(AttributeType.Power, 5, ModifierType.Additive)
                ]
            }
        ]));

        run.State.CurrentBoonChoices.Add(choice);
        service.ChooseBoon(run, choice.Id);
        run.State.CurrentBoonChoices.Add(choice);
        service.ChooseBoon(run, choice.Id);
        run.State.CurrentBoonChoices.Add(choice);
        var exception = Assert.Throws<InvalidOperationException>(() => service.ChooseBoon(run, choice.Id));

        Assert.Equal(2, run.State.ActiveBoonIds.Count);
        Assert.Equal("The selected boon has already reached its stack limit.", exception.Message);
    }

    [Fact]
    public void Active_boons_expose_combat_attribute_modifiers()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("hunter_focus");
        var service = CreateBoonService();

        var modifiers = service.GetActiveAttributeModifiers(run);

        Assert.Contains(modifiers, x => x.AttributeType == AttributeType.CritChance && x.Amount == 5);
        Assert.Contains(modifiers, x => x.AttributeType == AttributeType.Precision && x.Amount == 8);
    }

    [Fact]
    public void Active_boons_expose_combat_ability_modifiers()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("mana_spiral");
        var service = CreateBoonService();

        var modifiers = service.GetActiveAbilityModifiers(run);

        var modifier = Assert.Single(modifiers);
        Assert.Equal("effect.damage.main", modifier.Target);
        Assert.Equal("AddMultiplier", modifier.Operation);
        Assert.Equal(0.08, modifier.Value);
    }

    [Fact]
    public void Boon_generation_excludes_active_boons_and_uses_definitions()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("hunter_focus");
        var service = CreateBoonService();

        var choices = service.GenerateBoonChoices(run, 3);

        Assert.Equal(3, choices.Count);
        Assert.DoesNotContain(choices, x => x.Id == "hunter_focus");
        Assert.All(choices, x => Assert.False(string.IsNullOrWhiteSpace(x.Rarity)));
        Assert.All(choices, x => Assert.NotEmpty(x.EffectSummaries));
    }

    [Fact]
    public void Boon_generation_includes_stackable_active_boons_below_stack_limit()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("stacking_edge");
        run.State.ActiveBoonIds.Add("one_time_guard");
        var service = new DungeonBoonService(new StaticBoonDefinitions(
        [
            new()
            {
                Id = "stacking_edge",
                Name = "Stacking Edge",
                Description = "A test boon that can stack.",
                Rarity = DungeonBoonRarity.Common,
                MaxStacks = 2
            },
            new()
            {
                Id = "one_time_guard",
                Name = "One Time Guard",
                Description = "A test boon that cannot stack.",
                Rarity = DungeonBoonRarity.Common
            }
        ]));

        var choice = Assert.Single(service.GenerateBoonChoices(run, 2));

        Assert.Equal("stacking_edge", choice.Id);
    }

    [Fact]
    public void Boon_generation_respects_family_stack_limits_and_deduplicates_choice_families()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("focus_common");
        var service = new DungeonBoonService(new StaticBoonDefinitions(
        [
            new()
            {
                Id = "focus_common",
                FamilyId = "focus",
                FamilyName = "Focus",
                Name = "Focus",
                Description = "Common focus.",
                Rarity = DungeonBoonRarity.Common,
                Tier = 1,
                MaxStacks = 2,
                MaxFamilyStacks = 2
            },
            new()
            {
                Id = "focus_rare",
                FamilyId = "focus",
                FamilyName = "Focus",
                Name = "Focus",
                Description = "Rare focus.",
                Rarity = DungeonBoonRarity.Rare,
                Tier = 3,
                MaxStacks = 2,
                MaxFamilyStacks = 2
            },
            new()
            {
                Id = "guard_common",
                FamilyId = "guard",
                FamilyName = "Guard",
                Name = "Guard",
                Description = "Common guard.",
                Rarity = DungeonBoonRarity.Common,
                Tier = 1,
                MaxStacks = 2,
                MaxFamilyStacks = 2
            }
        ]));

        var choices = service.GenerateBoonChoices(run, 3);

        Assert.Equal(2, choices.Count);
        var focusChoice = Assert.Single(choices, x => x.FamilyId == "focus");
        Assert.Single(choices, x => x.FamilyId == "guard");

        run.State.CurrentBoonChoices.Clear();
        run.State.CurrentBoonChoices.Add(focusChoice);
        service.ChooseBoon(run, run.State.CurrentBoonChoices[0].Id);

        Assert.DoesNotContain(service.GenerateBoonChoices(run, 3), x => x.FamilyId == "focus");
    }

    [Fact]
    public void Boon_generation_includes_readable_effect_summaries()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("bulwark_echo");
        run.State.ActiveBoonIds.Add("hunter_focus");
        run.State.ActiveBoonIds.Add("guardian_root");
        var service = CreateBoonService();

        var choice = Assert.Single(service.GenerateBoonChoices(run));

        Assert.Equal("mana_spiral", choice.Id);
        Assert.Contains("+12% Spirit", choice.EffectSummaries);
        Assert.Contains("+5 Magic Penetration", choice.EffectSummaries);
        Assert.Contains("+8% main damage effects", choice.EffectSummaries);
    }

    [Fact]
    public void Active_boon_state_summarizes_active_boons_and_combines_matching_effects()
    {
        var run = CreateRun();
        run.State.ActiveBoonIds.Add("bulwark_echo");
        run.State.ActiveBoonIds.Add("bulwark_echo");
        run.State.ActiveBoonIds.Add("hunter_focus");
        var service = CreateBoonService();

        service.SyncActiveBoonState(run);

        var bulwark = Assert.Single(run.State.ActiveBoonSummaries, x => x.Id == "bulwark_echo");
        Assert.Equal(2, bulwark.Count);
        Assert.Equal("bulwark_echo", bulwark.FamilyId);
        Assert.Equal("Bulwark Echo", bulwark.FamilyName);
        Assert.Contains(bulwark.EffectSummaries, x => x == "+30% Armor");

        Assert.Contains(run.State.ActiveBoonEffectSummaries, x =>
            x.Label == "Armor" && x.Value == "+30%" && x.Category == "Stats");
        Assert.Contains(run.State.ActiveBoonEffectSummaries, x =>
            x.Label == "Resistance" && x.Value == "+30%" && x.Category == "Stats");
        Assert.Contains(run.State.ActiveBoonEffectSummaries, x =>
            x.Label == "Crit Chance" && x.Value == "+5" && x.Category == "Stats");
    }

    [Theory]
    [InlineData(RoomType.Combat, 4)]
    [InlineData(RoomType.MiniBoss, 8)]
    public void Combat_victory_completion_generates_boon_choices_for_non_boss_battles(
        RoomType roomType,
        int expectedPressure)
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        room.Type = roomType;

        InvokeApplyRoomCompletionPressure(run, room);

        Assert.Equal(expectedPressure, run.State.Pressure);
        Assert.Equal(1, run.State.Flags.GetValueOrDefault("pending_boon_advances_room"));
        Assert.NotEmpty(run.State.CurrentBoonChoices);
    }

    [Fact]
    public void Boss_victory_completion_does_not_generate_boon_choices()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        room.Type = RoomType.Boss;

        InvokeApplyRoomCompletionPressure(run, room);

        Assert.Equal(0, run.State.Pressure);
        Assert.False(run.State.Flags.ContainsKey("pending_boon_advances_room"));
        Assert.Empty(run.State.CurrentBoonChoices);
    }

    [Fact]
    public void Json_boon_provider_loads_authored_boon_definitions()
    {
        var provider = new JsonDungeonBoonDefinitionProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());

        var manaSpiral = provider.GetById("mana_spiral");
        var hunterFocus = provider.GetById("hunter_focus");
        var fatebreakerSeal = provider.GetById("fatebreaker_seal");

        Assert.NotNull(manaSpiral);
        Assert.NotNull(hunterFocus);
        Assert.NotNull(fatebreakerSeal);
        Assert.True(provider.GetAll().Count >= 43);
        Assert.Contains(provider.GetAll(), x => x.Rarity == DungeonBoonRarity.Rare);
        Assert.Contains(provider.GetAll(), x => x.Rarity == DungeonBoonRarity.Legacy);
        Assert.Contains(provider.GetAll(), x => x.MaxStacks > 1);
        Assert.Equal(
            Enum.GetValues<DungeonBoonRarity>(),
            provider.GetAll()
                .Where(x => x.FamilyId == "hunter_focus")
                .Select(x => x.Rarity)
                .OrderBy(x => x)
                .ToArray());
        Assert.Contains(hunterFocus!.AttributeModifiers, x => x.AttributeType == AttributeType.CritChance && x.Amount == 5);
        Assert.Contains(manaSpiral!.AbilityModifiers, x => x.Target == "effect.damage.main" && x.Operation == "AddMultiplier");
        Assert.Equal(5, hunterFocus.MaxStacks);
        Assert.Equal(5, hunterFocus.MaxFamilyStacks);
        Assert.Equal(2, manaSpiral.Tier);
        Assert.Equal(1, fatebreakerSeal!.MaxStacks);
    }

    [Fact]
    public void Json_mastery_bonus_provider_loads_authored_bonus_definitions()
    {
        var provider = new JsonDungeonMasteryBonusDefinitionProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());

        var bonuses = provider.GetAll();

        Assert.Equal(10, bonuses.Count);
        Assert.Equal(Enumerable.Range(1, 10), bonuses.Select(x => x.RequiredLevel));
        Assert.All(bonuses, bonus => Assert.Equal(5, bonus.RewardMultiplierBonusPercent));
        Assert.Contains(bonuses, bonus => bonus.Id == "mastery_reward_multiplier_level_1");
        Assert.Contains(bonuses, bonus => bonus.Id == "mastery_reward_multiplier_level_10");
    }

    [Fact]
    public void Json_event_provider_loads_dungeon_specific_event_definitions()
    {
        var provider = new JsonDungeonEventDefinitionProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());

        var goblinTreasure = provider.GetDefinition("goblin_mines_ii", EventOutcomeType.TreasureRoom);
        var genericTrap = provider.GetDefinition("test_dungeon", EventOutcomeType.Trap);

        Assert.Equal("goblin_explosive_storage", goblinTreasure.Id);
        Assert.Contains(goblinTreasure.Choices, x => x.RevealsHiddenRoute);
        Assert.Contains(genericTrap.Choices, x => x.RevealsHiddenRoute);
    }

    [Fact]
    public void Json_route_provider_loads_goblin_and_catacombs_route_tables()
    {
        var provider = new JsonDungeonRouteDefinitionProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());

        var goblinRoutes = provider.GetDefinitions("goblin_mines_ii", RoomType.Event);
        var catacombRoutes = provider.GetDefinitions("forgotten_catacombs_iii", RoomType.Combat);

        Assert.Contains(goblinRoutes, x => x.Id == "goblin_mines_event_blasted_store_room");
        Assert.Contains(goblinRoutes, x => x.Tags.Contains("explosives"));
        Assert.Contains(catacombRoutes, x => x.Id == "catacombs_combat_ossuary_passage");
        Assert.Contains(catacombRoutes, x => x.Tags.Contains("undead"));
    }

    [Fact]
    public void Route_generation_prefers_authored_dungeon_route_tables()
    {
        var run = CreateRun();
        run.DungeonDefinitionId = "forgotten_catacombs";
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        run.Rooms.Add(new RoomInstance
        {
            Id = Guid.NewGuid(),
            RoomIndex = 1,
            Type = RoomType.Event
        });
        var service = CreateRouteService(
        [
            new()
            {
                Id = "catacombs_event_ossuary_test",
                DungeonDefinitionIds = ["forgotten_catacombs"],
                RoomType = RoomType.Event,
                DisplayName = "Ossuary Test Route",
                PressureDelta = 6,
                Tags = ["event", "catacombs"]
            }
        ]);

        var routes = service.GenerateRouteOptions(run);

        var route = Assert.Single(routes);
        Assert.Equal("Ossuary Test Route", route.DisplayName);
        Assert.Equal("route:1:catacombs_event_ossuary_test", route.Id);
        Assert.Equal(1, route.RoomIndex);
        Assert.Contains("catacombs", route.Tags);
    }

    [Fact]
    public void Json_dungeon_definitions_load_authored_mechanics_and_thresholds()
    {
        var contentRoot = FindApiContentRoot();
        var reader = new JsonDefinitionReader<DungeonDefinition>(
            contentRoot,
            Path.Combine("Data", "dungeons.json"),
            CreateJsonOptions());
        var definitions = new JsonDungeonDefinitions(reader, new DungeonDefinitionValidator());

        var goblinMines = definitions.GetByKey("goblin_mines");
        var catacombs = definitions.GetByKey("forgotten_catacombs");

        Assert.Equal("alarm", goblinMines.Mechanic.Id);
        Assert.Equal("Alarm Level", goblinMines.Mechanic.DisplayName);
        var alarmFull = Assert.Single(goblinMines.Mechanic.Thresholds, x => x.Id == "alarm_full");
        Assert.Equal(75, alarmFull.Value);
        Assert.Equal(45, alarmFull.RewardMultiplierBonusPercent);
        Assert.Contains("alarm_enemy_reinforced", alarmFull.EnemyModifierIds);
        Assert.Contains("alarm_boss_enraged", alarmFull.BossModifierIds);

        Assert.Equal("curse", catacombs.Mechanic.Id);
        Assert.Equal("Curse", catacombs.Mechanic.DisplayName);
        var curseEmpowered = Assert.Single(catacombs.Mechanic.Thresholds, x => x.Id == "curse_empowered");
        Assert.Equal(75, curseEmpowered.Value);
        Assert.Equal(45, curseEmpowered.RewardMultiplierBonusPercent);
        Assert.Contains("curse_enemy_empowered", curseEmpowered.EnemyModifierIds);
        Assert.Contains("curse_boss_empowered", curseEmpowered.BossModifierIds);
    }

    [Fact]
    public void Boss_modifier_service_uses_pressure_thresholds_and_run_flags()
    {
        var run = CreateRun();
        run.State.Pressure = 80;
        run.State.Flags["checkpoint_pushes"] = 2;
        run.State.Flags["saved_explosives"] = 1;
        run.State.Flags["hidden_route_taken"] = 1;
        var room = run.Rooms[0];
        room.Type = RoomType.Boss;
        var dungeon = new DungeonDefinition
        {
            Id = "goblin_mines",
            Mechanic = new DungeonMechanicDefinition
            {
                Thresholds =
                [
                    new()
                    {
                        Id = "alarm_high",
                        Value = 75,
                        BossModifierIds = ["alarm_boss_enraged"]
                    }
                ]
            }
        };
        var service = new DungeonBossModifierService();

        var modifiers = service.GetActiveBossModifiers(run, dungeon, room);
        var attributeModifiers = service.GetActiveBossAttributeModifiers(run, dungeon, room);

        Assert.Contains(modifiers, x => x.Id == "alarm_boss_enraged" && x.AttributeType == AttributeType.Power);
        Assert.Contains(modifiers, x => x.Id == "checkpoint_push_boss_fury" && x.Amount == 10);
        Assert.Contains(modifiers, x => x.Id == "goblin_saved_explosives" && x.IsHelpfulToPlayer);
        Assert.Contains(modifiers, x => x.Id == "boss_surprised" && x.IsHelpfulToPlayer);
        Assert.Equal(modifiers.Count, attributeModifiers.Count);
    }

    [Fact]
    public void Boss_modifier_service_ignores_non_boss_rooms()
    {
        var run = CreateRun();
        run.State.Pressure = 100;
        run.State.Flags["checkpoint_pushes"] = 1;
        var dungeon = new DungeonDefinition { Id = run.DungeonDefinitionId };
        var service = new DungeonBossModifierService();

        var modifiers = service.GetActiveBossModifiers(run, dungeon, run.Rooms[0]);

        Assert.Empty(modifiers);
    }

    [Fact]
    public void Encounter_modifier_service_uses_enemy_threshold_modifier_ids()
    {
        var run = CreateRun();
        run.State.Pressure = 75;
        var dungeon = new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Mechanic = new DungeonMechanicDefinition
            {
                Thresholds =
                [
                    new()
                    {
                        Id = "curse_high",
                        Value = 75,
                        EnemyModifierIds = ["curse_enemy_empowered"]
                    }
                ]
            }
        };
        var service = new DungeonEncounterModifierService();

        var modifiers = service.GetActiveEnemyAttributeModifiers(run, dungeon, run.Rooms[0]);

        var modifier = Assert.Single(modifiers);
        Assert.Equal(AttributeType.Spirit, modifier.AttributeType);
        Assert.Equal(8, modifier.Amount);
        Assert.Equal(ModifierType.Additive, modifier.ModifierType);
    }

    [Fact]
    public void Dungeon_combat_plan_carries_enemy_attribute_modifiers()
    {
        var characterId = Guid.NewGuid();
        var enemyId = Guid.NewGuid();
        var enemyModifier = new DungeonAttributeModifier(
            AttributeType.Power,
            12,
            ModifierType.Additive);
        var planner = new DungeonCombatPlanner();

        var plan = planner.CreatePlan(
            dungeonRunId: Guid.NewGuid(),
            characterId: characterId,
            characterSnapshot: new() { CharacterId = characterId },
            playerEntityIds: [characterId],
            enemySourceEntityIds: [enemyId],
            enemyAttributeModifiers: [enemyModifier]);

        var modifier = Assert.Single(plan.EnemyAttributeModifiers);
        Assert.Equal(AttributeType.Power, modifier.AttributeType);
        Assert.Equal(12, modifier.Amount);
        Assert.Equal(ModifierType.Additive, modifier.ModifierType);

        var encounterPlan = planner.CreateEncounterPlan(plan, 1, DateTimeOffset.UtcNow);

        Assert.Equal(CombatMode.Dungeon, encounterPlan.Mode);
        Assert.Single(encounterPlan.FriendlyParticipants);
        Assert.Single(encounterPlan.HostileParticipants);
    }

    [Fact]
    public async Task Dungeon_encounter_participant_resolver_maps_legacy_content_keys()
    {
        var vampireBatId = Guid.NewGuid();
        var service = new DungeonEncounterParticipantResolver(
            new StaticCreatureService(new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            {
                ["vampire_bat"] = vampireBatId
            }));

        var resolved = await service.ResolveAsync(["giant_bat"], CancellationToken.None);

        Assert.Equal(vampireBatId, Assert.Single(resolved));
    }

    [Fact]
    public void Checkpoint_focus_generates_boon_choices()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        room.Type = RoomType.Checkpoint;
        var service = CreateCheckpointService(run);

        var result = service.ApplyChoice(run, room, "focus");

        Assert.Equal(DungeonCheckpointChoiceOutcome.Focus, result.Outcome);
        Assert.Empty(run.State.CurrentCheckpointChoices);
        Assert.NotEmpty(run.State.CurrentBoonChoices);
        Assert.Equal(1, run.State.Flags.GetValueOrDefault("pending_boon_completes_room"));
    }

    [Fact]
    public void Checkpoint_push_deeper_adds_pressure_and_multiplier_flag()
    {
        var run = CreateRun();
        var room = run.Rooms[0];
        room.Type = RoomType.Checkpoint;
        var service = CreateCheckpointService(run);

        var result = service.ApplyChoice(run, room, "push_deeper");

        Assert.Equal(DungeonCheckpointChoiceOutcome.PushDeeper, result.Outcome);
        Assert.Equal(15, run.State.Pressure);
        Assert.Equal(1, run.State.Flags["checkpoint_pushes"]);
        Assert.Equal(20, run.State.Flags["reward_multiplier_bonus_pct"]);
    }

    [Fact]
    public void Checkpoint_rest_reduces_pressure_and_unsecured_loot()
    {
        var run = CreateRun();
        run.State.Pressure = 30;
        run.PendingExperience = 100;
        run.PendingCinders = 50;
        run.PendingSoulstones = 10;
        run.PendingRewards.Add(new RunReward
        {
            ItemId = "item.test",
            Name = "Test Item",
            ItemType = ItemType.Resource,
            Quantity = 10,
            Source = "room:1"
        });
        var room = run.Rooms[0];
        room.Type = RoomType.Checkpoint;
        var service = CreateCheckpointService(run);

        var result = service.ApplyChoice(run, room, "rest");

        Assert.Equal(DungeonCheckpointChoiceOutcome.Rest, result.Outcome);
        Assert.Equal(20, run.State.Pressure);
        Assert.Equal(90, run.PendingExperience);
        Assert.Equal(45, run.PendingCinders);
        Assert.Equal(9, run.PendingSoulstones);
        Assert.Equal(9, run.PendingRewards.Single().Quantity);
        Assert.Equal(45, run.State.UnsecuredLoot.Cinders);
        Assert.Equal(9, run.State.UnsecuredLoot.Items["item.test"]);
    }

    [Fact]
    public void Checkpoint_withdraw_secures_current_pending_loot()
    {
        var run = CreateRun();
        run.PendingExperience = 120;
        run.PendingCinders = 34;
        run.PendingSoulstones = 5;
        run.PendingRewards.Add(new RunReward
        {
            ItemId = "item.test",
            Name = "Test Item",
            ItemType = ItemType.Resource,
            Quantity = 3,
            Source = "room:1"
        });
        var room = run.Rooms[0];
        room.Type = RoomType.Checkpoint;
        var service = CreateCheckpointService(run);

        var result = service.ApplyChoice(run, room, "withdraw");

        Assert.Equal(DungeonCheckpointChoiceOutcome.Withdraw, result.Outcome);
        Assert.Equal(DungeonRunStatus.Withdrawn, run.Status);
        Assert.Equal(RoomInstanceStatus.Completed, room.Status);
        Assert.Equal(120, run.State.SecuredLoot.Experience);
        Assert.Equal(34, run.State.SecuredLoot.Cinders);
        Assert.Equal(5, run.State.SecuredLoot.Soulstones);
        Assert.Equal(3, run.State.SecuredLoot.Items["item.test"]);
        Assert.Equal(0, run.State.UnsecuredLoot.Cinders);
        Assert.Empty(run.State.UnsecuredLoot.Items);
        Assert.Empty(run.State.CurrentCheckpointChoices);
    }

    [Fact]
    public async Task Withdrawn_reward_claim_uses_secured_loot_only()
    {
        var characterId = Guid.NewGuid();
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            DungeonDefinitionId = "test_dungeon",
            Status = DungeonRunStatus.Withdrawn,
            PendingExperience = 999,
            PendingCinders = 999,
            PendingSoulstones = 999,
            State = new DungeonRunState
            {
                RunId = Guid.NewGuid(),
                SecuredLoot = new DungeonLootBag
                {
                    Experience = 50,
                    Cinders = 20,
                    Soulstones = 2,
                    Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["item.secured"] = 4
                    }
                }
            },
            PendingRewards =
            [
                new()
                {
                    ItemId = "item.unsecured",
                    Name = "Unsecured",
                    ItemType = ItemType.Resource,
                    Quantity = 99,
                    Source = "room:1"
                }
            ]
        };
        var experience = new CapturingExperienceRewardWriter();
        var currency = new CapturingCurrencyRewardWriter();
        var inventory = new CapturingInventoryService();
        var claimer = new DungeonRunRewardClaimer(
            experience,
            currency,
            new StaticItemBaseRepository(
            [
                new() { Id = "item.secured", Name = "Secured", ItemType = ItemType.Resource, Stackable = true },
                new() { Id = "item.unsecured", Name = "Unsecured", ItemType = ItemType.Resource, Stackable = true }
            ]),
            new InventoryItemFactory(),
            inventory);

        var claimed = await claimer.ClaimAsync(run, CancellationToken.None);

        Assert.Equal(50, experience.TotalExperience);
        Assert.Equal(20, currency.Cinders);
        Assert.Equal(2, currency.Soulstones);
        var claimedItem = Assert.Single(claimed);
        Assert.Equal("item.secured", claimedItem.ItemInstance.ItemBaseId);
        Assert.Equal(4, claimedItem.Quantity);
        Assert.DoesNotContain(inventory.Items, item => item.ItemInstance.ItemBaseId == "item.unsecured");
    }

    [Fact]
    public void Treasure_event_search_deeper_adds_pressure_and_state_flag()
    {
        var run = CreateRun();
        var service = CreateEventChoiceService(run);
        service.EnsureChoices(run, EventOutcomeType.TreasureRoom);

        var choice = service.ApplyChoiceState(run, "search_deeper");

        Assert.Equal("search_deeper", choice.Id);
        Assert.Equal(12, run.State.Pressure);
        Assert.Equal(1, run.State.Flags["searched_deep_treasure"]);
    }

    [Fact]
    public void Shrine_event_cleanse_reduces_pressure()
    {
        var run = CreateRun();
        run.State.Pressure = 30;
        var service = CreateEventChoiceService(run);
        service.EnsureChoices(run, EventOutcomeType.Shrine);

        var choice = service.ApplyChoiceState(run, "cleanse_corruption");

        Assert.Equal("cleanse_corruption", choice.Id);
        Assert.Equal(15, run.State.Pressure);
        Assert.Equal(1, run.State.Flags["cleansed_shrine"]);
    }

    [Fact]
    public void Shrine_event_receive_blessing_exposes_boon_choice_flag()
    {
        var run = CreateRun();
        var service = CreateEventChoiceService(run);

        var choices = service.EnsureChoices(run, EventOutcomeType.Shrine);

        var blessing = Assert.Single(choices, x => x.Id == "receive_blessing");
        Assert.True(blessing.GrantsBoonChoice);
    }

    [Fact]
    public void Trap_event_can_reveal_hidden_route_at_pressure_cost()
    {
        var run = CreateRun();
        var service = CreateEventChoiceService(run);
        service.EnsureChoices(run, EventOutcomeType.Trap);

        var choice = service.ApplyChoiceState(run, "trigger_intentionally");

        Assert.Equal("trigger_intentionally", choice.Id);
        Assert.Equal(10, run.State.Pressure);
        Assert.Equal(1, run.State.Flags["revealed_hidden_route"]);
        Assert.Equal(1, run.State.Flags["hidden_route_revealed"]);
    }

    [Fact]
    public void Event_choices_expose_missing_flag_requirements()
    {
        var run = CreateRun();
        var service = CreateEventChoiceService(run);

        var choices = service.EnsureChoices(run, EventOutcomeType.Shrine);

        var hiddenRoute = Assert.Single(choices, x => x.Id == "use_miner_route");
        Assert.Contains("Requires: Save Miner", hiddenRoute.MissingRequirements);
        Assert.DoesNotContain(hiddenRoute.MissingRequirements, x => x.Contains("saved_miner", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Dungeon_mastery_award_completion_grants_xp_levels_and_is_idempotent()
    {
        var run = CreateRun();
        run.Status = DungeonRunStatus.Completed;
        run.State.Pressure = 100;
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        var repository = new InMemoryMasteryRepository();
        var service = new DungeonMasteryService(repository, CreateMasteryBonusProvider());

        var firstAward = await service.AwardCompletionAsync(run, CancellationToken.None);
        var secondAward = await service.AwardCompletionAsync(run, CancellationToken.None);

        Assert.False(firstAward.AlreadyAwarded);
        Assert.Equal(155, firstAward.ExperienceAwarded);
        Assert.Equal(1, firstAward.Level);
        Assert.Equal(1, firstAward.LevelsGained);
        Assert.Equal(1, firstAward.CompletionCount);
        Assert.Contains(firstAward.Reasons, x => x.Id == "high_pressure_completion" && x.Experience == 50);
        Assert.Contains(run.State.MasteryAwardReasons, x => x.Id == "completion");
        Assert.True(secondAward.AlreadyAwarded);
        Assert.Equal(0, secondAward.ExperienceAwarded);
        Assert.Equal(firstAward.TotalExperience, secondAward.TotalExperience);
    }

    [Fact]
    public async Task Dungeon_mastery_award_includes_boss_optional_and_high_pressure_xp()
    {
        var run = CreateRun();
        run.Status = DungeonRunStatus.Completed;
        run.State.Pressure = 80;
        run.State.Flags["searched_deep_treasure"] = 1;
        run.Rooms[0].Status = RoomInstanceStatus.Completed;
        run.Rooms.Add(new RoomInstance
        {
            Id = Guid.NewGuid(),
            RoomIndex = 1,
            Type = RoomType.Boss,
            Status = RoomInstanceStatus.Completed
        });
        var service = new DungeonMasteryService(new InMemoryMasteryRepository(), CreateMasteryBonusProvider());

        var award = await service.AwardCompletionAsync(run, CancellationToken.None);

        Assert.Equal(210, award.ExperienceAwarded);
        Assert.Equal(1, award.Level);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(99, 0)]
    [InlineData(100, 1)]
    [InlineData(250, 2)]
    [InlineData(7500, 10)]
    public void Dungeon_mastery_level_calculation_uses_cumulative_thresholds(long experience, int expectedLevel)
    {
        var service = new DungeonMasteryService(new InMemoryMasteryRepository(), CreateMasteryBonusProvider());

        Assert.Equal(expectedLevel, service.CalculateLevel(experience));
    }

    [Fact]
    public async Task Dungeon_mastery_start_bonus_adds_reward_multiplier_for_level_two_mastery()
    {
        var run = CreateRun();
        var repository = new InMemoryMasteryRepository();
        await repository.AddAsync(new CharacterDungeonMastery
        {
            CharacterId = run.CharacterId,
            DungeonDefinitionId = run.DungeonDefinitionId,
            Experience = 250,
            Level = 2,
            CompletionCount = 2
        }, CancellationToken.None);
        var service = new DungeonMasteryService(repository, CreateMasteryBonusProvider());

        await service.ApplyStartBonusesAsync(run, CancellationToken.None);
        await service.ApplyStartBonusesAsync(run, CancellationToken.None);

        Assert.Equal(2, run.State.Flags[DungeonMasteryService.MasteryLevelFlag]);
        Assert.Equal(5, run.State.Flags[DungeonMasteryService.MasteryRewardBonusFlag]);
        Assert.Equal(5, run.State.Flags[DungeonMasteryService.RewardMultiplierBonusFlag]);
        Assert.Equal(105, run.State.RewardMultiplierPercent);
    }

    private static DungeonRun CreateRun() => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = Guid.NewGuid(),
        DungeonDefinitionId = "test_dungeon",
        DungeonDefinitionName = "Test Dungeon",
        Seed = 123,
        Status = DungeonRunStatus.Active,
        State = new DungeonRunState
        {
            RunId = Guid.NewGuid()
        },
        Rooms =
        [
            new()
            {
                Id = Guid.NewGuid(),
                RoomIndex = 0,
                Type = RoomType.Combat
            }
        ]
    };

    private static DungeonRouteService CreateRouteService(
        IReadOnlyList<DungeonRouteDefinition>? definitions = null) =>
        new(new StaticRouteDefinitions(definitions ?? []));

    private static DungeonCheckpointService CreateCheckpointService(DungeonRun run)
    {
        var definitions = new SingleDungeonDefinitions(new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Name = run.DungeonDefinitionName
        });

        return new DungeonCheckpointService(
            new DungeonPressureService(definitions),
            CreateBoonService());
    }

    private static DungeonEventChoiceService CreateEventChoiceService(DungeonRun run)
    {
        var definitions = new SingleDungeonDefinitions(new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Name = run.DungeonDefinitionName
        });

        return new DungeonEventChoiceService(
            new DungeonPressureService(definitions),
            new StaticEventDefinitions(CreateEventDefinitions()));
    }

    private static IDungeonMasteryBonusDefinitionProvider CreateMasteryBonusProvider() =>
        new StaticMasteryBonusDefinitions(
        [
            new()
            {
                Id = "mastery_reward_multiplier_1",
                Description = "+5% dungeon reward multiplier",
                RequiredLevel = 2,
                RewardMultiplierBonusPercent = 5
            }
        ]);

    private static IReadOnlyList<DungeonEventDefinition> CreateEventDefinitions() =>
    [
        new()
        {
            Id = "treasure",
            Name = "Treasure",
            OutcomeType = EventOutcomeType.TreasureRoom,
            Choices =
            [
                new()
                {
                    Id = "search_deeper",
                    Label = "Search Deeper",
                    Description = "Gain better unsecured loot at higher risk.",
                    PressureDelta = 12,
                    GrantsLoot = true,
                    AddFlags = ["searched_deep_treasure"]
                }
            ]
        },
        new()
        {
            Id = "shrine",
            Name = "Shrine",
            OutcomeType = EventOutcomeType.Shrine,
            Choices =
            [
                new()
                {
                    Id = "cleanse_corruption",
                    Label = "Cleanse Corruption",
                    Description = "Reduce pressure.",
                    PressureDelta = -15,
                    AddFlags = ["cleansed_shrine"]
                },
                new()
                {
                    Id = "receive_blessing",
                    Label = "Receive Blessing",
                    Description = "Choose one temporary boon.",
                    GrantsBoonChoice = true
                },
                new()
                {
                    Id = "use_miner_route",
                    Label = "Use Miner Route",
                    Description = "Take the miner's hidden route.",
                    RequiredFlags = ["saved_miner"],
                    PressureDelta = -10,
                    AddFlags = ["revealed_hidden_route"],
                    RevealsHiddenRoute = true
                }
            ]
        },
        new()
        {
            Id = "trap",
            Name = "Trap",
            OutcomeType = EventOutcomeType.Trap,
            Choices =
            [
                new()
                {
                    Id = "trigger_intentionally",
                    Label = "Trigger Intentionally",
                    Description = "Raise pressure to reveal a hidden opportunity.",
                    PressureDelta = 10,
                    AddFlags = ["revealed_hidden_route"],
                    RevealsHiddenRoute = true
                }
            ]
        }
    ];

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidates = new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            };

            foreach (var candidate in candidates)
            {
                var boonCandidate = Path.Combine(candidate, "Data", "dungeon-boons.json");
                if (File.Exists(boonCandidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/dungeon-boons.json from test output directory.");
    }

    private static void InvokeApplyRoomCompletionPressure(DungeonRun run, RoomInstance room)
    {
        var definitions = new SingleDungeonDefinitions(new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Name = "Test Dungeon"
        });

        var service = new DungeonRunService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            definitions,
            null!,
            null!,
            new DungeonPressureService(definitions),
            null!,
            CreateBoonService(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var method = typeof(DungeonRunService).GetMethod(
            "ApplyRoomCompletionPressure",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(method);
        method.Invoke(service, [run, room]);
    }

    private static DungeonBoonService CreateBoonService() => new(new StaticBoonDefinitions(
    [
        new()
        {
            Id = "bulwark_echo",
            Name = "Bulwark Echo",
            Description = "Defensive effects are stronger for this dungeon.",
            Rarity = DungeonBoonRarity.Common,
            AttributeModifiers =
            [
                new EssenceAttributeModifier(AttributeType.Armor, 15, ModifierType.Additive),
                new EssenceAttributeModifier(AttributeType.Resistance, 15, ModifierType.Additive)
            ]
        },
        new()
        {
            Id = "hunter_focus",
            Name = "Hunter's Focus",
            Description = "Gain an edge against elites and bosses.",
            Rarity = DungeonBoonRarity.Common,
            AttributeModifiers =
            [
                new EssenceAttributeModifier(AttributeType.CritChance, 5, ModifierType.Flat),
                new EssenceAttributeModifier(AttributeType.Precision, 8, ModifierType.Additive)
            ]
        },
        new()
        {
            Id = "mana_spiral",
            Name = "Mana Spiral",
            Description = "Damage abilities gain momentum during rooms.",
            Rarity = DungeonBoonRarity.Uncommon,
            AttributeModifiers =
            [
                new EssenceAttributeModifier(AttributeType.Spirit, 12, ModifierType.Additive),
                new EssenceAttributeModifier(AttributeType.MagicPenetration, 5, ModifierType.Flat)
            ],
            AbilityModifiers =
            [
                new()
                {
                    Target = "effect.damage.main",
                    Operation = "AddMultiplier",
                    Value = 0.08
                }
            ]
        },
        new()
        {
            Id = "guardian_root",
            Name = "Guardian Root",
            Description = "Protective reactions become more reliable.",
            Rarity = DungeonBoonRarity.Uncommon
        }
    ]));

    private sealed class SingleDungeonDefinitions(DungeonDefinition definition) : IDungeonDefinitions
    {
        public DungeonDefinition GetByKey(string key) => definition;
        public IReadOnlyList<DungeonDefinition> GetAll() => [definition];
    }

    private sealed class StaticBoonDefinitions(IReadOnlyList<DungeonBoonDefinition> definitions)
        : IDungeonBoonDefinitionProvider
    {
        public IReadOnlyList<DungeonBoonDefinition> GetAll() => definitions;

        public DungeonBoonDefinition? GetById(string boonId) =>
            definitions.FirstOrDefault(x => x.Id.Equals(boonId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StaticRouteDefinitions(IReadOnlyList<DungeonRouteDefinition> definitions)
        : IDungeonRouteDefinitionProvider
    {
        public IReadOnlyList<DungeonRouteDefinition> GetAll() => definitions;

        public IReadOnlyList<DungeonRouteDefinition> GetDefinitions(string dungeonDefinitionId, RoomType roomType) =>
            definitions
                .Where(x => x.RoomType == roomType &&
                    x.DungeonDefinitionIds.Any(id =>
                        dungeonDefinitionId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                        dungeonDefinitionId.StartsWith(id + "_", StringComparison.OrdinalIgnoreCase)))
                .ToList();
    }

    private sealed class StaticMasteryBonusDefinitions(IReadOnlyList<DungeonMasteryBonusDefinition> definitions)
        : IDungeonMasteryBonusDefinitionProvider
    {
        public IReadOnlyList<DungeonMasteryBonusDefinition> GetAll() => definitions;
    }

    private sealed class StaticEventDefinitions(IReadOnlyList<DungeonEventDefinition> definitions)
        : IDungeonEventDefinitionProvider
    {
        public IReadOnlyList<DungeonEventDefinition> GetAll() => definitions;

        public DungeonEventDefinition GetDefinition(string dungeonDefinitionId, EventOutcomeType outcomeType) =>
            definitions.FirstOrDefault(x => x.OutcomeType == outcomeType)
            ?? definitions.First(x => x.OutcomeType == EventOutcomeType.TreasureRoom);
    }

    private sealed class InMemoryMasteryRepository : ICharacterDungeonMasteryRepository
    {
        private readonly List<CharacterDungeonMastery> _masteries = [];

        public Task AddAsync(CharacterDungeonMastery mastery, CancellationToken cancellationToken)
        {
            _masteries.Add(mastery);
            return Task.CompletedTask;
        }

        public Task<CharacterDungeonMastery?> GetAsync(
            Guid characterId,
            string dungeonDefinitionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_masteries.FirstOrDefault(
                x => x.CharacterId == characterId &&
                    x.DungeonDefinitionId.Equals(dungeonDefinitionId, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyList<CharacterDungeonMastery>> GetForCharacterAsync(
            Guid characterId,
            IReadOnlyCollection<string> dungeonDefinitionIds,
            CancellationToken cancellationToken)
        {
            var selected = _masteries
                .Where(x => x.CharacterId == characterId && dungeonDefinitionIds.Contains(x.DungeonDefinitionId))
                .ToList();

            return Task.FromResult<IReadOnlyList<CharacterDungeonMastery>>(selected);
        }
    }

    private sealed class CapturingExperienceRewardWriter : IExperienceRewardWriter
    {
        public int TotalExperience { get; private set; }

        public Task AddSplitExperienceAsync(
            IReadOnlyCollection<Guid> recipientCharacterIds,
            int totalExperience,
            CancellationToken cancellationToken)
        {
            TotalExperience += totalExperience;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingCurrencyRewardWriter : ICurrencyRewardWriter
    {
        public int Cinders { get; private set; }
        public int Soulstones { get; private set; }

        public Task AddAsync(Guid characterId, int cinders, int soulstones, CancellationToken cancellationToken)
        {
            Cinders += cinders;
            Soulstones += soulstones;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingInventoryService : IInventoryService
    {
        public List<InventoryItem> Items { get; } = [];

        public Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<Inventory?>(null);

        public Task AddItemsToInventory(
            Guid characterId,
            List<InventoryItem> loot,
            CancellationToken cancellationToken)
        {
            Items.AddRange(loot);
            return Task.CompletedTask;
        }

        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult<InventoryItem?>(null);

        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(
            Guid characterId,
            MarketPlaceListing marketplaceListing,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> AddItemInstanceBackToInventory(
            Guid characterId,
            ItemInstance itemInstance,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddItemToInventoryFromMarketPlace(
            Guid characterId,
            InventoryItem inventoryItem,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<InventoryItem?> ScrapEquipments(
            Guid characterId,
            List<Guid> parsedGuids,
            CancellationToken cancellationToken) =>
            Task.FromResult<InventoryItem?>(null);
    }

    private sealed class StaticItemBaseRepository(IReadOnlyList<ItemBase> itemBases) : IItemBaseRepository
    {
        private readonly Dictionary<string, ItemBase> _itemBases = itemBases
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken)
        {
            var result = _itemBases.Values
                .Where(x => itemIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            return Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(result);
        }

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken) =>
            Task.FromResult(itemBases.OfType<EquipmentBase>().FirstOrDefault(x => x.Id == itemBaseId));

        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken)
        {
            foreach (var itemBase in itemBases)
            {
                _itemBases.TryAdd(itemBase.Id, itemBase);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StaticCreatureService(IReadOnlyDictionary<string, Guid> idsByKey) : ICreatureService
    {
        public Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new List<Creature>());

        public Task<List<Guid>> GetCreaturesByKey(IReadOnlyList<string> enemyCreatureKeys, CancellationToken cancellationToken) =>
            Task.FromResult(enemyCreatureKeys
                .Where(idsByKey.ContainsKey)
                .Select(key => idsByKey[key])
                .ToList());

        public Task UpdateCreatureAsync(CreatureDto creatureToUpdate, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
