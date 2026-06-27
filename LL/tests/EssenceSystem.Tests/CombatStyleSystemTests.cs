using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.CombatStyles.Dtos;
using Application.UseCases.CombatStyles.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Persistence.LL.Repositories.CombatStyles;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Essences;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.CombatStyles;
using Services.LL.Interfaces.Combat.Reward;

namespace EssenceSystem.Tests;

public sealed class CombatStyleSystemTests
{
    [Fact]
    public async Task Overview_seeds_styles_and_defaults_fighter_active()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);

        var overview = await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(8, overview.Styles.Count);
        Assert.Equal("fighter", overview.ActiveStyleId);
        Assert.Equal(8, await db.PlayerCombatStyles.CountAsync(x => x.CharacterId == characterId));
        Assert.Contains(overview.Styles, x => x.Id == "swift");
        Assert.Contains(overview.Styles, x => x.Id == "marksman");
        Assert.Contains(overview.Styles, x => x.Id == "support");
        Assert.Contains(overview.Styles, x => x.Id == "controller");
    }

    [Fact]
    public async Task Activating_style_deactivates_previous_style()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();

        var result = await service.ActivateStyleAsync(characterId, "defensive", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        Assert.True(db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "defensive").IsActive);
        Assert.False(db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "fighter").IsActive);
    }

    [Fact]
    public async Task Activating_previous_style_again_switches_back_to_it()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();

        var defensive = await service.ActivateStyleAsync(characterId, "defensive", CancellationToken.None);
        await db.SaveChangesAsync();
        var fighter = await service.ActivateStyleAsync(characterId, "fighter", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(defensive.Succeeded);
        Assert.True(fighter.Succeeded);
        Assert.True(db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "fighter").IsActive);
        Assert.False(db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "defensive").IsActive);
    }

    [Fact]
    public async Task Invalid_style_activation_fails()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);

        var result = await service.ActivateStyleAsync(characterId, "missing", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Combat Style does not exist.", result.Message);
    }

    [Fact]
    public async Task Cannot_activate_style_during_active_dungeon_run()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        db.DungeonRuns.Add(new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            DungeonDefinitionId = "test",
            DungeonDefinitionName = "Test",
            Status = DungeonRunStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ActivateStyleAsync(characterId, "defensive", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Cannot switch Combat Style during an active dungeon run.", result.Message);
    }

    [Fact]
    public async Task Focus_selection_requires_unlock_level()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();

        var locked = await service.SelectFocusAsync(characterId, "caster", "spellblade", CancellationToken.None);
        db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "caster").Level = 10;
        await db.SaveChangesAsync();
        var unlocked = await service.SelectFocusAsync(characterId, "caster", "spellblade", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(locked.Succeeded);
        Assert.True(unlocked.Succeeded);
        Assert.Equal("spellblade", unlocked.Value?.SelectedFocusId);
    }

    [Fact]
    public async Task Skill_tree_nodes_can_be_ranked_and_reset()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();
        await service.GrantExperienceAsync(characterId, 8_100, "test", CancellationToken.None);
        await db.SaveChangesAsync();

        var rootRanked = await service.RankUpNodeAsync(characterId, "fighter", "duelist-path", CancellationToken.None);
        await db.SaveChangesAsync();
        var ranked = await service.RankUpNodeAsync(characterId, "fighter", "duelist-technique", CancellationToken.None);
        await db.SaveChangesAsync();
        var snapshot = await service.GetActiveSnapshotAsync(characterId, CancellationToken.None);
        var reset = await service.ResetSkillTreeAsync(characterId, "fighter", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(rootRanked.Succeeded);
        Assert.Null(rootRanked.Value?.SelectedFocusId);
        Assert.True(ranked.Succeeded);
        Assert.Equal("duelist", ranked.Value?.SelectedFocusId);
        Assert.Equal(8, ranked.Value?.SkillPointsAvailable);
        Assert.Equal(2, ranked.Value?.SkillPointsSpent);
        Assert.Equal(1, snapshot?.NodeRanks?["duelist-path"]);
        Assert.Equal(1, snapshot?.NodeRanks?["duelist-technique"]);
        Assert.True(reset.Succeeded);
        Assert.Null(reset.Value?.SelectedFocusId);
        Assert.Equal(0, reset.Value?.SkillPointsSpent);
        Assert.Empty(db.PlayerCombatStyleNodes.Where(x => x.CharacterId == characterId && x.StyleId == "fighter"));
    }

    [Fact]
    public async Task Granting_xp_levels_active_style_and_caps_at_50()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();

        await service.GrantExperienceAsync(characterId, 10_000_000, "test", CancellationToken.None);
        await db.SaveChangesAsync();

        var fighter = db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "fighter");
        Assert.Equal(50, fighter.Level);
    }

    [Fact]
    public async Task Idle_rewards_grant_xp_to_active_combat_style()
    {
        var characterId = Guid.NewGuid();
        var experience = new CapturingExperienceRewardWriter();
        var combatStyles = new CapturingCombatStyleService();
        var applier = new IdleCombatRewardApplier(
            experience,
            new NoOpLootRewardWriter(),
            new NoOpCurrencyRewardWriter(),
            combatStyles);
        var now = DateTimeOffset.UtcNow;
        var facts = new IdleCombatRewardFacts(
            characterId,
            now,
            now,
            now,
            TimeSpan.Zero,
            null!,
            [characterId],
            null,
            []);
        var outcome = new IdleCombatCalculatedOutcome(
            characterId,
            now,
            now,
            TotalExperience: 120,
            TotalCinders: 0,
            TotalSoulstones: 0,
            TotalLoot: [],
            GatheringRewards: [],
            EncounterOutcomes: []);

        await applier.ApplyAsync(facts, outcome, CancellationToken.None);

        Assert.Equal(120, experience.TotalExperience);
        Assert.Equal(120, combatStyles.ExperienceGranted);
        Assert.Equal("idle_combat", combatStyles.Source);
    }

    [Fact]
    public void Defensive_guard_triggers_protective_shell_twice_per_encounter()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 1, 0, null, null));
        var player = CreateCombatant("player", CombatTeam.Friendly, maxHealth: 1000);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);

        for (var i = 0; i < 60; i++)
            engine.OnDamageTaken(state, DamageEffect(), enemy, player, 1, 1m);

        Assert.Equal(200, player.Barrier);
    }

    [Fact]
    public void Fighter_momentum_empowers_next_direct_damage_effect()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 1, 0, null, null));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var effect = DamageEffect(active: true);

        for (var i = 0; i < 13; i++)
            engine.OnDamageDealt(state, effect, player, enemy, 10, 1m);

        var empowered = engine.ModifyEffectAmount(state, effect, player, enemy, 100);
        var consumed = engine.ModifyEffectAmount(state, effect, player, enemy, 100);

        Assert.Equal(115, empowered);
        Assert.Equal(100, consumed);
    }

    [Fact]
    public void Hostile_side_combat_style_applies_to_hostile_player()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(
            new CombatStyleSnapshot("defensive", "Defensive", 1, 0, null, null),
            appliesToFriendlyTeam: false);
        var attacker = CreateCombatant("attacker", CombatTeam.Friendly);
        var defender = CreateCombatant("defender", CombatTeam.Hostile);

        var reduced = engine.ModifyEffectAmount(state, DamageEffect(), attacker, defender, 100);
        var unreduced = engine.ModifyEffectAmount(state, DamageEffect(), defender, attacker, 100);

        Assert.Equal(95, reduced);
        Assert.Equal(100, unreduced);
    }

    [Fact]
    public void Rule_definitions_apply_baseline_effect_modifiers()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 1, 0, null, null));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);

        var amount = engine.ModifyEffectAmount(state, DamageEffect(), enemy, player, 100);

        Assert.Equal(95, amount);
    }

    [Fact]
    public void Generic_rule_caps_limit_rule_triggers_per_encounter()
    {
        var definition = new CombatStyleDefinition(
            "capped",
            "Capped",
            "Test style",
            "test",
            100,
            50,
            [],
            [],
            [],
            [],
            [
                new CombatStyleRuleDefinition
                {
                    Id = "capped_damage_bonus",
                    EventType = CombatStyleEventType.EffectCalculation,
                    Predicate = new EffectPredicate
                    {
                        SourceMustBePlayer = true,
                        EffectOperations = [AbilityEffectOperation.Damage]
                    },
                    Operation = new ModifyEffectAmountOperation(0.10m),
                    MaxTriggersPerEncounter = 2
                }
            ],
            [],
            "Test");
        var engine = new CombatStyleRuleEngine(new SingleDefinitionProvider(definition));
        var state = engine.CreateState(new CombatStyleSnapshot("capped", "Capped", 1, 0, null, null));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var effect = DamageEffect(active: true);

        var first = engine.ModifyEffectAmount(state, effect, player, enemy, 100);
        var second = engine.ModifyEffectAmount(state, effect, player, enemy, 100);
        var third = engine.ModifyEffectAmount(state, effect, player, enemy, 100);

        Assert.Equal(110, first);
        Assert.Equal(110, second);
        Assert.Equal(100, third);
    }

    [Fact]
    public void Generic_rule_selector_applies_ranked_node_rules_and_max_style_level()
    {
        var node = new CombatStyleTreeNodeDefinition(
            "branch-technique",
            "branch",
            "Technique",
            "Test node",
            3,
            1,
            null,
            0,
            0,
            [],
            [],
            true)
        {
            Rules =
            [
                new CombatStyleRuleDefinition
                {
                    Id = "ranked_node_damage_bonus",
                    EventType = CombatStyleEventType.EffectCalculation,
                    MinStyleLevel = 1,
                    MaxStyleLevel = 10,
                    Predicate = new EffectPredicate
                    {
                        SourceMustBePlayer = true,
                        EffectOperations = [AbilityEffectOperation.Damage]
                    },
                    Operation = new ModifyEffectAmountOperation(
                        0.05m,
                        AdditivePercentModifiers:
                        [
                            new StyleValueModifier("nodeRank", 0.02m, NodeId: "branch-technique")
                        ])
                }
            ]
        };
        var definition = CreateSingleRuleDefinition("node-test", "charge", [node], [], []);
        var engine = new CombatStyleRuleEngine(new SingleDefinitionProvider(definition));
        var active = engine.CreateState(new CombatStyleSnapshot(
            "node-test",
            "Node Test",
            10,
            0,
            null,
            null,
            new Dictionary<string, int> { ["branch-technique"] = 2 }));
        var expired = engine.CreateState(new CombatStyleSnapshot(
            "node-test",
            "Node Test",
            11,
            0,
            null,
            null,
            new Dictionary<string, int> { ["branch-technique"] = 2 }));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);

        var activeAmount = engine.ModifyEffectAmount(active, DamageEffect(active: true), player, enemy, 100);
        var expiredAmount = engine.ModifyEffectAmount(expired, DamageEffect(active: true), player, enemy, 100);

        Assert.Equal(109, activeAmount);
        Assert.Equal(100, expiredAmount);
    }

    [Fact]
    public void Resource_overflow_dispatches_pending_empowerment_operation()
    {
        var definition = CreateSingleRuleDefinition(
            "overflow-test",
            "charge",
            [],
            [
                new CombatStyleRuleDefinition
                {
                    Id = "charge_on_active",
                    EventType = CombatStyleEventType.AbilityResolved,
                    Predicate = new EffectPredicate
                    {
                        SourceMustBePlayer = true,
                        ActiveAbilityOnly = true
                    },
                    Operation = new GainStyleResourceOperation("charge", 10, UsesProcCoefficient: false)
                }
            ],
            [
                new SetPendingEmpowermentOperation(
                    "charge_empowerment",
                    new EffectPredicate
                    {
                        SourceMustBePlayer = true,
                        ActiveAbilityOnly = true,
                        EffectOperations = [AbilityEffectOperation.Damage]
                    },
                    0.25m)
            ],
            resourceMaxAmount: 10);
        var engine = new CombatStyleRuleEngine(new SingleDefinitionProvider(definition));
        var state = engine.CreateState(new CombatStyleSnapshot("overflow-test", "Overflow Test", 1, 0, null, null));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var effect = DamageEffect(active: true);

        engine.OnAbilityResolved(state, ActiveAbility("active", []), player);
        var empowered = engine.ModifyEffectAmount(state, effect, player, enemy, 100);
        var consumed = engine.ModifyEffectAmount(state, effect, player, enemy, 100);

        Assert.Equal(125, empowered);
        Assert.Equal(100, consumed);
    }

    [Fact]
    public void Combat_style_balance_simulator_ranks_style_focus_candidates()
    {
        var simulator = new CombatStyleBalanceSimulator(CreateDefinitionProvider());

        var report = simulator.Run(new CombatStyleBalanceSimulationRequest(
            BattleCount: 1,
            StyleLevel: 40,
            RandomSeed: 2468,
            TopResults: 12,
            IncludeFocuses: true));

        Assert.Equal(40, report.StyleLevel);
        Assert.Equal(32, report.CandidateCount);
        Assert.Equal(496, report.BattlesRun);
        Assert.Equal(12, report.RankedStyles.Count);
        Assert.NotEmpty(report.BattleSummaries);
        Assert.Contains(report.RankedStyles, result => result.FocusId is not null);
        Assert.All(report.RankedStyles, result =>
        {
            Assert.True(result.Battles > 0);
            Assert.InRange(result.WinRate, 0, 1);
        });
    }

    [Fact]
    public void Caster_spellblade_adds_spirit_scaling_to_melee_active_damage()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("caster", "Caster", 10, 0, "spellblade", "Spellblade"));
        var player = CreateCombatant("player", CombatTeam.Friendly, spirit: 100);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);

        var amount = engine.ModifyEffectAmount(state, DamageEffect(active: true, tags: ["Melee"]), player, enemy, 50);

        Assert.Equal(65, amount);
    }

    [Fact]
    public void Defensive_level_ten_foci_have_distinct_runtime_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var counterguard = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 10, 0, "counterguard", "Counterguard"));
        var commander = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 10, 0, "commander", "Commander"));

        engine.OnDamageTaken(counterguard, DamageEffect(), enemy, player, 10, 1m);
        var retaliation = engine.ModifyEffectAmount(counterguard, DamageEffect(active: true), player, enemy, 100);
        var summonAttributes = engine.ModifySummonAttributes(
            commander,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });

        Assert.Equal(110, retaliation);
        Assert.Equal(115, summonAttributes[AttributeType.MaxHealth]);
        Assert.Equal(21, summonAttributes[AttributeType.Power]);
    }

    [Fact]
    public void Defensive_focus_milestones_scale_level_twenty_five_and_forty_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly, maxHealth: 1000);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var bulwark25 = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 25, 0, "bulwark", "Bulwark"));
        var bulwark40 = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 40, 0, "bulwark", "Bulwark"));
        var counterguard40 = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 40, 0, "counterguard", "Counterguard"));
        var commander40 = engine.CreateState(new CombatStyleSnapshot("defensive", "Defensive", 40, 0, "commander", "Commander"));

        var barrier = engine.ModifyEffectAmount(bulwark25, BarrierEffect(active: true), player, player, 100);
        for (var i = 0; i < 20; i++)
            engine.OnDamageTaken(bulwark40, DamageEffect(), enemy, player, 1, 1m);
        engine.OnDamageTaken(counterguard40, DamageEffect(), enemy, player, 10, 1m);
        var counterguardDamage = engine.ModifyEffectAmount(counterguard40, DamageEffect(active: true), player, enemy, 100);
        var commanderAttributes = engine.ModifySummonAttributes(
            commander40,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });

        Assert.Equal(115, barrier);
        Assert.Equal(170, player.Barrier);
        Assert.Equal(120, counterguardDamage);
        Assert.Equal(125, commanderAttributes[AttributeType.MaxHealth]);
        Assert.Equal(23, commanderAttributes[AttributeType.Power]);
    }

    [Fact]
    public void Fighter_level_ten_foci_have_distinct_runtime_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var duelist = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 10, 0, "duelist", "Duelist"));
        var berserker = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 10, 0, "berserker", "Berserker"));
        var vanguard = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 10, 0, "vanguard", "Vanguard"));

        player.SetHealth(50);
        var duelistDamage = engine.ModifyEffectAmount(duelist, DamageEffect(active: true), player, enemy, 100);
        var berserkerDamage = engine.ModifyEffectAmount(berserker, DamageEffect(active: true), player, enemy, 100);
        var vanguardIncoming = engine.ModifyEffectAmount(vanguard, DamageEffect(), enemy, player, 100);

        Assert.Equal(108, duelistDamage);
        Assert.Equal(112, berserkerDamage);
        Assert.Equal(95, vanguardIncoming);
    }

    [Fact]
    public void Fighter_focus_milestones_scale_level_twenty_five_and_forty_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var duelist25 = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 25, 0, "duelist", "Duelist"));
        var berserker40 = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 40, 0, "berserker", "Berserker"));
        var vanguard40 = engine.CreateState(new CombatStyleSnapshot("fighter", "Fighter", 40, 0, "vanguard", "Vanguard"));

        player.SetHealth(60);
        var duelistDamage = engine.ModifyEffectAmount(duelist25, DamageEffect(active: true), player, enemy, 100);
        var berserkerDamage = engine.ModifyEffectAmount(berserker40, DamageEffect(active: true), player, enemy, 100);
        var vanguardIncoming = engine.ModifyEffectAmount(vanguard40, DamageEffect(), enemy, player, 100);
        for (var i = 0; i < 13; i++)
            engine.OnDamageTaken(vanguard40, DamageEffect(), enemy, player, 10, 1m);

        Assert.Equal(112, duelistDamage);
        Assert.Equal(124, berserkerDamage);
        Assert.Equal(88, vanguardIncoming);
        Assert.Single(vanguard40!.PendingEmpowerments);
    }

    [Fact]
    public void Defensive_and_fighter_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var defensive = engine.CreateState(new CombatStyleSnapshot(
            "defensive",
            "Defensive",
            10,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["bulwark-path"] = 1,
                ["bulwark-technique"] = 2
            }));
        var fighter = engine.CreateState(new CombatStyleSnapshot(
            "fighter",
            "Fighter",
            10,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["duelist-technique"] = 2,
                ["vanguard-technique"] = 1
            }));

        var barrier = engine.ModifyEffectAmount(defensive, BarrierEffect(active: true), player, player, 100);
        var defensiveIncoming = engine.ModifyEffectAmount(defensive, DamageEffect(), enemy, player, 100);
        var fighterDamage = engine.ModifyEffectAmount(fighter, DamageEffect(active: true), player, enemy, 100);
        var fighterIncoming = engine.ModifyEffectAmount(fighter, DamageEffect(), enemy, player, 100);

        Assert.Equal(110, barrier);
        Assert.Equal(94, defensiveIncoming);
        Assert.Equal(104, fighterDamage);
        Assert.Equal(98, fighterIncoming);
    }

    [Fact]
    public void Caster_level_ten_foci_have_distinct_runtime_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var arcanist = engine.CreateState(new CombatStyleSnapshot("caster", "Caster", 10, 0, "arcanist", "Arcanist"));
        var occultist = engine.CreateState(new CombatStyleSnapshot("caster", "Caster", 10, 0, "occultist", "Occultist"));

        var arcanistDamage = engine.ModifyEffectAmount(arcanist, DamageEffect(active: true, tags: ["Magic"]), player, enemy, 100);
        var occultistDamage = engine.ModifyEffectAmount(occultist, DamageEffect(active: true, tags: ["Curse", "DoT"]), player, enemy, 100);
        engine.OnAbilityResolved(occultist, ActiveAbility("curse_spell", ["Curse"]), player);

        Assert.Equal(110, arcanistDamage);
        Assert.Equal(110, occultistDamage);
        Assert.Equal(2, occultist!.Resources["arcaneCharge"]);
    }

    [Fact]
    public void Caster_focus_milestones_scale_level_twenty_five_and_forty_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly, spirit: 100);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var arcanist40 = engine.CreateState(new CombatStyleSnapshot("caster", "Caster", 40, 0, "arcanist", "Arcanist"));
        var spellblade25 = engine.CreateState(new CombatStyleSnapshot("caster", "Caster", 25, 0, "spellblade", "Spellblade"));
        var occultist25 = engine.CreateState(new CombatStyleSnapshot("caster", "Caster", 25, 0, "occultist", "Occultist"));

        var arcanistDamage = engine.ModifyEffectAmount(arcanist40, DamageEffect(active: true, tags: ["Magic"]), player, enemy, 100);
        engine.OnAbilityResolved(arcanist40, ActiveAbility("magic_spell", ["Magic"]), player);
        var spellbladeDamage = engine.ModifyEffectAmount(spellblade25, DamageEffect(active: true, tags: ["Melee"]), player, enemy, 50);
        var occultistDamage = engine.ModifyEffectAmount(occultist25, DamageEffect(active: true, tags: ["Curse", "DoT"]), player, enemy, 100);

        Assert.Equal(120, arcanistDamage);
        Assert.Equal(3, arcanist40!.Resources["arcaneCharge"]);
        Assert.Equal(70, spellbladeDamage);
        Assert.Equal(115, occultistDamage);
    }

    [Fact]
    public void Caster_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly, spirit: 100);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var arcanist = engine.CreateState(new CombatStyleSnapshot(
            "caster",
            "Caster",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["arcanist-instinct"] = 2,
                ["arcanist-technique"] = 2
            }));
        var spellblade = engine.CreateState(new CombatStyleSnapshot(
            "caster",
            "Caster",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["spellblade-path"] = 1
            }));
        var occultist = engine.CreateState(new CombatStyleSnapshot(
            "caster",
            "Caster",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["occultist-technique"] = 2,
                ["occultist-discipline"] = 1
            }));

        engine.OnAbilityResolved(arcanist, ActiveAbility("magic_spell", ["Magic"]), player);
        var arcanistDamage = engine.ModifyEffectAmount(arcanist, DamageEffect(active: true, tags: ["Magic"]), player, enemy, 100);
        var spellbladeDamage = engine.ModifyEffectAmount(spellblade, DamageEffect(active: true, tags: ["Melee"]), player, enemy, 50);
        var occultistDamage = engine.ModifyEffectAmount(occultist, DamageEffect(active: true, tags: ["Curse", "DoT"]), player, enemy, 100);

        Assert.Equal(3, arcanist!.Resources["arcaneCharge"]);
        Assert.Equal(104, arcanistDamage);
        Assert.Equal(55, spellbladeDamage);
        Assert.Equal(105, occultistDamage);
    }

    [Fact]
    public void Summoner_improves_owned_summon_attributes_without_creating_summons()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("summoner", "Summoner", 1, 0, null, null));
        var player = CreateCombatant("player", CombatTeam.Friendly);

        var attributes = engine.ModifySummonAttributes(
            state,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });

        Assert.Equal(110, attributes[AttributeType.MaxHealth]);
        Assert.Equal(22, attributes[AttributeType.Power]);
    }

    [Fact]
    public void Summoner_ritualist_amplifies_curse_or_holy_summon_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("summoner", "Summoner", 10, 0, "ritualist", "Ritualist"));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);

        var amount = engine.ModifyEffectAmount(state, DamageEffect(active: true, tags: ["Summon", "Curse"]), player, enemy, 100);

        Assert.Equal(115, amount);
    }

    [Fact]
    public void Summoner_focus_milestones_scale_level_twenty_five_and_forty_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var horde40 = engine.CreateState(new CombatStyleSnapshot("summoner", "Summoner", 40, 0, "horde", "Horde"));
        var champion40 = engine.CreateState(new CombatStyleSnapshot("summoner", "Summoner", 40, 0, "champion", "Champion"));
        var ritualist40 = engine.CreateState(new CombatStyleSnapshot("summoner", "Summoner", 40, 0, "ritualist", "Ritualist"));

        var hordeAttributes = engine.ModifySummonAttributes(
            horde40,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });
        var championAttributes = engine.ModifySummonAttributes(
            champion40,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });
        var ritualistDamage = engine.ModifyEffectAmount(
            ritualist40,
            DamageEffect(active: true, tags: ["Summon", "Holy"]),
            player,
            enemy,
            100);

        Assert.Equal(25, hordeAttributes[AttributeType.Power]);
        Assert.Equal(135, championAttributes[AttributeType.MaxHealth]);
        Assert.Equal(125, ritualistDamage);
    }

    [Fact]
    public void Summoner_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var horde = engine.CreateState(new CombatStyleSnapshot(
            "summoner",
            "Summoner",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["horde-instinct"] = 2,
                ["horde-technique"] = 2
            }));
        var champion = engine.CreateState(new CombatStyleSnapshot(
            "summoner",
            "Summoner",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["champion-technique"] = 2,
                ["champion-discipline"] = 1
            }));
        var ritualist = engine.CreateState(new CombatStyleSnapshot(
            "summoner",
            "Summoner",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["ritualist-technique"] = 2
            }));
        var summon = CreateSummon("summon", CombatTeam.Friendly, player);

        engine.OnAbilityResolved(horde, ActiveAbility("summon", ["Summon"]), player);
        var hordeAttributes = engine.ModifySummonAttributes(
            horde,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });
        var championAttributes = engine.ModifySummonAttributes(
            champion,
            player,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 20
            });
        var championIncoming = engine.ModifyEffectAmount(champion, DamageEffect(), enemy, summon, 100);
        var ritualistDamage = engine.ModifyEffectAmount(
            ritualist,
            DamageEffect(active: true, tags: ["Summon", "Curse"]),
            player,
            enemy,
            100);

        Assert.Equal(19, horde!.Resources["command"]);
        Assert.Equal(23.2f, hordeAttributes[AttributeType.Power], 1);
        Assert.Equal(116, championAttributes[AttributeType.MaxHealth]);
        Assert.Equal(98, championIncoming);
        Assert.Equal(109, ritualistDamage);
    }

    [Fact]
    public void Swift_style_builds_flow_and_empowers_active_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("swift", "Swift", 40, 0, "tempo", "Tempo"));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var effect = DamageEffect(active: true, tags: ["Ranged"], attackType: AttackType.Ranged);

        for (var i = 0; i < 6; i++)
            engine.OnAbilityResolved(state, ActiveAbility($"active_{i}", ["Ranged"]), player);
        var empowered = engine.ModifyEffectAmount(state, effect, player, enemy, 100);

        Assert.Equal(125, empowered);
        Assert.Empty(state!.PendingEmpowerments);
    }

    [Fact]
    public void Swift_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var flurry = engine.CreateState(new CombatStyleSnapshot(
            "swift",
            "Swift",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["flurry-instinct"] = 2,
                ["flurry-technique"] = 2
            }));
        var evasion = engine.CreateState(new CombatStyleSnapshot(
            "swift",
            "Swift",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["evasion-technique"] = 1,
                ["evasion-discipline"] = 1
            }));
        var tempo = engine.CreateState(new CombatStyleSnapshot(
            "swift",
            "Swift",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["tempo-technique"] = 2,
                ["tempo-discipline"] = 1
            }));

        engine.OnAbilityResolved(flurry, ActiveAbility("quick_shot", ["Ranged"]), player);
        var flurryDamage = engine.ModifyEffectAmount(flurry, DamageEffect(active: true, tags: ["Ranged"], attackType: AttackType.Ranged), player, enemy, 100);
        var evasionIncoming = engine.ModifyEffectAmount(evasion, DamageEffect(), enemy, player, 100);
        var tempoDamage = engine.ModifyEffectAmount(tempo, DamageEffect(active: true), player, enemy, 100);
        var tempoStatus = engine.ModifyEffectAmount(tempo, StatusEffect(active: true, tags: ["Debuff"]), player, enemy, 100);

        Assert.Equal(14, flurry!.Resources["flow"]);
        Assert.Equal(107, flurryDamage);
        Assert.Equal(97, evasionIncoming);
        Assert.Equal(105, tempoDamage);
        Assert.Equal(104, tempoStatus);
    }

    [Fact]
    public void Marksman_style_builds_aim_from_ranged_damage()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("marksman", "Marksman", 40, 0, "sniper", "Sniper"));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var effect = DamageEffect(active: true, tags: ["Ranged"], attackType: AttackType.Ranged);

        for (var i = 0; i < 10; i++)
            engine.OnDamageDealt(state, effect, player, enemy, 10, 1m);
        var empowered = engine.ModifyEffectAmount(state, effect, player, enemy, 100);

        Assert.Equal(146, empowered);
        Assert.Empty(state!.PendingEmpowerments);
    }

    [Fact]
    public void Marksman_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var sniper = engine.CreateState(new CombatStyleSnapshot(
            "marksman",
            "Marksman",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["sniper-instinct"] = 2,
                ["sniper-technique"] = 2,
                ["sniper-discipline"] = 1
            }));
        var volley = engine.CreateState(new CombatStyleSnapshot(
            "marksman",
            "Marksman",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["volley-technique"] = 2,
                ["volley-discipline"] = 1
            }));
        var trapper = engine.CreateState(new CombatStyleSnapshot(
            "marksman",
            "Marksman",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["trapper-technique"] = 2,
                ["trapper-discipline"] = 1
            }));
        var rangedDamage = DamageEffect(active: true, tags: ["Ranged"], attackType: AttackType.Ranged);
        var rangedDebuffDamage = DamageEffect(active: true, tags: ["Ranged", "Debuff"], attackType: AttackType.Ranged);

        engine.OnDamageDealt(sniper, rangedDamage, player, enemy, 10, 1m);
        var sniperDamage = engine.ModifyEffectAmount(sniper, rangedDamage, player, enemy, 100);
        var volleyDamage = engine.ModifyEffectAmount(
            volley,
            DamageEffect(active: true, tags: ["Ranged"], target: AbilityTargetSelector.AllEnemies, attackType: AttackType.Ranged),
            player,
            enemy,
            100);
        var trapperDamage = engine.ModifyEffectAmount(trapper, rangedDebuffDamage, player, enemy, 100);

        Assert.Equal(14, sniper!.Resources["aim"]);
        Assert.Equal(110, sniperDamage);
        Assert.Equal(110, volleyDamage);
        Assert.Equal(110, trapperDamage);
    }

    [Fact]
    public void Support_style_builds_resolve_from_healing_and_barriers()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("support", "Support", 40, 0, "healer", "Healer"));
        var player = CreateCombatant("player", CombatTeam.Friendly);

        for (var i = 0; i < 9; i++)
            engine.ModifyEffectAmount(state, HealEffect(active: true), player, player, 10);
        var empowered = engine.ModifyEffectAmount(state, HealEffect(active: true), player, player, 100);

        Assert.Equal(153, empowered);
        Assert.Empty(state!.PendingEmpowerments);
    }

    [Fact]
    public void Support_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var healer = engine.CreateState(new CombatStyleSnapshot(
            "support",
            "Support",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["healer-path"] = 1,
                ["healer-instinct"] = 2,
                ["healer-technique"] = 2,
                ["healer-discipline"] = 1,
                ["healer-specialization"] = 1,
                ["healer-mastery"] = 1,
                ["healer-amplifier"] = 1,
                ["healer-focus"] = 1
            }));
        var warden = engine.CreateState(new CombatStyleSnapshot(
            "support",
            "Support",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["warden-path"] = 1,
                ["warden-instinct"] = 2,
                ["warden-technique"] = 2,
                ["warden-discipline"] = 1,
                ["warden-specialization"] = 1,
                ["warden-mastery"] = 1,
                ["warden-amplifier"] = 1,
                ["warden-focus"] = 1
            }));
        var chaplain = engine.CreateState(new CombatStyleSnapshot(
            "support",
            "Support",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["chaplain-path"] = 1,
                ["chaplain-instinct"] = 2,
                ["chaplain-technique"] = 2,
                ["chaplain-discipline"] = 1,
                ["chaplain-specialization"] = 1,
                ["chaplain-mastery"] = 1,
                ["chaplain-amplifier"] = 1,
                ["chaplain-focus"] = 1
            }));

        healer!.Resources["resolve"] = 95;
        warden!.Resources["resolve"] = 95;
        chaplain!.Resources["resolve"] = 95;
        var healerHeal = engine.ModifyEffectAmount(healer, HealEffect(active: true), player, player, 100);
        var healerEmpoweredHeal = engine.ModifyEffectAmount(healer, HealEffect(), player, player, 100);
        var wardenBarrier = engine.ModifyEffectAmount(warden, BarrierEffect(active: true), player, player, 100);
        var wardenIncoming = engine.ModifyEffectAmount(warden, DamageEffect(), enemy, player, 100);
        var wardenEmpoweredBarrier = engine.ModifyEffectAmount(warden, BarrierEffect(), player, player, 100);
        var chaplainHolyHeal = engine.ModifyEffectAmount(chaplain, HealEffect(active: true, tags: ["Holy"]), player, player, 100);
        var chaplainEmpoweredHolyHeal = engine.ModifyEffectAmount(chaplain, HealEffect(tags: ["Holy"]), player, player, 100);

        Assert.Equal(119, healerHeal);
        Assert.Equal(147, healerEmpoweredHeal);
        Assert.Equal(118, wardenBarrier);
        Assert.Equal(94, wardenIncoming);
        Assert.Equal(146, wardenEmpoweredBarrier);
        Assert.Equal(119, chaplainHolyHeal);
        Assert.Equal(147, chaplainEmpoweredHolyHeal);
    }

    [Fact]
    public void Controller_style_builds_control_from_active_status_and_debuff_effects()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var state = engine.CreateState(new CombatStyleSnapshot("controller", "Controller", 40, 0, "hexer", "Hexer"));
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var effect = StatusEffect(active: true, tags: ["Curse", "Control"]);

        for (var i = 0; i < 7; i++)
            engine.ModifyEffectAmount(state, effect, player, enemy, 10);
        var empowered = engine.ModifyEffectAmount(state, effect, player, enemy, 100);

        Assert.Equal(153, empowered);
        Assert.Empty(state!.PendingEmpowerments);
    }

    [Fact]
    public void Controller_skill_tree_nodes_have_runtime_effects_without_selected_focus()
    {
        var engine = new CombatStyleRuleEngine(CreateDefinitionProvider());
        var player = CreateCombatant("player", CombatTeam.Friendly);
        var enemy = CreateCombatant("enemy", CombatTeam.Hostile);
        var hexer = engine.CreateState(new CombatStyleSnapshot(
            "controller",
            "Controller",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["hexer-path"] = 1,
                ["hexer-instinct"] = 2,
                ["hexer-technique"] = 2,
                ["hexer-discipline"] = 1,
                ["hexer-specialization"] = 1,
                ["hexer-mastery"] = 1,
                ["hexer-amplifier"] = 1,
                ["hexer-focus"] = 1
            }));
        var tactician = engine.CreateState(new CombatStyleSnapshot(
            "controller",
            "Controller",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["tactician-path"] = 1,
                ["tactician-instinct"] = 2,
                ["tactician-technique"] = 2,
                ["tactician-discipline"] = 1,
                ["tactician-specialization"] = 1,
                ["tactician-mastery"] = 1,
                ["tactician-amplifier"] = 1,
                ["tactician-focus"] = 1
            }));
        var frostbinder = engine.CreateState(new CombatStyleSnapshot(
            "controller",
            "Controller",
            1,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["frostbinder-path"] = 1,
                ["frostbinder-instinct"] = 2,
                ["frostbinder-technique"] = 2,
                ["frostbinder-discipline"] = 1,
                ["frostbinder-specialization"] = 1,
                ["frostbinder-mastery"] = 1,
                ["frostbinder-amplifier"] = 1,
                ["frostbinder-focus"] = 1
            }));
        var curseEffect = StatusEffect(active: true, tags: ["Curse"]);
        var tacticalEffect = StatusEffect(active: true, tags: ["Debuff", "Physical"]);
        var controlEffect = StatusEffect(active: true, tags: ["Control"]);

        hexer!.Resources["control"] = 95;
        tactician!.Resources["control"] = 95;
        frostbinder!.Resources["control"] = 95;
        var hexerStatus = engine.ModifyEffectAmount(hexer, curseEffect, player, enemy, 100);
        var hexerEmpoweredStatus = engine.ModifyEffectAmount(hexer, StatusEffect(tags: ["Curse"]), player, enemy, 100);
        var tacticianStatus = engine.ModifyEffectAmount(tactician, tacticalEffect, player, enemy, 100);
        var tacticianIncoming = engine.ModifyEffectAmount(tactician, DamageEffect(), enemy, player, 100);
        var tacticianEmpoweredStatus = engine.ModifyEffectAmount(tactician, StatusEffect(tags: ["Debuff", "Physical"]), player, enemy, 100);
        var frostbinderStatus = engine.ModifyEffectAmount(frostbinder, controlEffect, player, enemy, 100);
        var frostbinderIncoming = engine.ModifyEffectAmount(frostbinder, DamageEffect(), enemy, player, 100);
        var frostbinderEmpoweredStatus = engine.ModifyEffectAmount(frostbinder, StatusEffect(tags: ["Control"]), player, enemy, 100);

        Assert.Equal(119, hexerStatus);
        Assert.Equal(147, hexerEmpoweredStatus);
        Assert.Equal(118, tacticianStatus);
        Assert.Equal(99, tacticianIncoming);
        Assert.Equal(146, tacticianEmpoweredStatus);
        Assert.Equal(118, frostbinderStatus);
        Assert.Equal(99, frostbinderIncoming);
        Assert.Equal(146, frostbinderEmpoweredStatus);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static async Task<Guid> SeedCharacterAsync(LLDbContext db)
    {
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Test Character",
            UserId = Guid.NewGuid(),
            Level = 10
        });
        await db.SaveChangesAsync();
        return characterId;
    }

    private static CombatStyleService CreateService(LLDbContext db)
    {
        var definitions = CreateDefinitionProvider();
        return new CombatStyleService(
            new PlayerCombatStyleRepository(db),
            definitions,
            new CombatStyleSwitchValidator(new DungeonRunRepository(db)),
            new EmptyEssenceDefinitionRepository(),
            new EssenceRepository(db),
            NullLogger<CombatStyleService>.Instance);
    }

    private static ICombatStyleDefinitionProvider CreateDefinitionProvider()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        return new JsonCombatStyleDefinitionProvider(
            new ConfigurationBuilder().Build(),
            FindApiContentRoot(),
            options);
    }

    private static CombatStyleDefinition CreateSingleRuleDefinition(
        string styleId,
        string resourceId,
        IReadOnlyList<CombatStyleTreeNodeDefinition> nodes,
        IReadOnlyList<CombatStyleRuleDefinition> rules,
        IReadOnlyList<StyleRuleOperation> overflowOperations,
        decimal resourceMaxAmount = 100) =>
        new(
            styleId,
            styleId,
            "Test style",
            resourceId,
            resourceMaxAmount,
            50,
            [],
            [],
            [],
            nodes,
            rules,
            overflowOperations,
            "Test");

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "LL", "src", "API", "API.LL");
            if (File.Exists(Path.Combine(candidate, "Data", "combat-styles.json")))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL content root for combat style definitions.");
    }

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        float maxHealth = 100,
        float spirit = 0) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = 10,
                [AttributeType.Spirit] = spirit
            },
            []);

    private static RuntimeCombatant CreateSummon(
        string id,
        CombatTeam team,
        RuntimeCombatant owner,
        float maxHealth = 100) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = 10
            },
            [],
            isSummoned: true,
            summonDurationTicks: 10,
            summonOwner: owner);

    private static CompiledAbility ActiveAbility(string id, IReadOnlyList<string> tags) =>
        new()
        {
            Id = id,
            Name = id,
            Kind = AbilitySpecKind.Active,
            CooldownTicks = 0,
            Costs = [],
            TriggersByEvent = new Dictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTrigger>>(),
            Tags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase)
        };

    private static CompiledEffect DamageEffect(
        bool active = false,
        IReadOnlyList<string>? tags = null,
        AbilityTargetSelector target = AbilityTargetSelector.CurrentTarget,
        AttackType attackType = AttackType.Melee) =>
        new()
        {
            Id = "damage",
            StatsSource = "Test",
            Operation = AbilityEffectOperation.Damage,
            Target = target,
            DamageType = DamageType.Physical,
            AttackType = attackType,
            ProcCoefficient = 1m,
            AbilityKind = active ? AbilitySpecKind.Active : AbilitySpecKind.Passive,
            AbilityTags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Conditions = []
        };

    private static CompiledEffect HealEffect(bool active = false, IReadOnlyList<string>? tags = null) =>
        new()
        {
            Id = "heal",
            StatsSource = "Test",
            Operation = AbilityEffectOperation.Heal,
            Target = AbilityTargetSelector.Self,
            ProcCoefficient = 1m,
            AbilityKind = active ? AbilitySpecKind.Active : AbilitySpecKind.Passive,
            AbilityTags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Tags = new HashSet<string>(tags is null ? new[] { "Healing" } : tags.Concat(["Healing"]).ToArray(), StringComparer.OrdinalIgnoreCase),
            Conditions = []
        };

    private static CompiledEffect BarrierEffect(bool active = false, IReadOnlyList<string>? tags = null) =>
        new()
        {
            Id = "barrier",
            StatsSource = "Test",
            Operation = AbilityEffectOperation.GrantBarrier,
            Target = AbilityTargetSelector.Self,
            ProcCoefficient = 1m,
            AbilityKind = active ? AbilitySpecKind.Active : AbilitySpecKind.Passive,
            AbilityTags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Conditions = []
        };

    private static CompiledEffect StatusEffect(bool active = false, IReadOnlyList<string>? tags = null) =>
        new()
        {
            Id = "status",
            StatsSource = "Test",
            Operation = AbilityEffectOperation.ApplyStatus,
            Target = AbilityTargetSelector.CurrentTarget,
            ProcCoefficient = 1m,
            AbilityKind = active ? AbilitySpecKind.Active : AbilitySpecKind.Passive,
            AbilityTags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase),
            Conditions = []
        };

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

    private sealed class NoOpLootRewardWriter : ILootRewardWriter
    {
        public Task AddLootAsync(
            Guid characterId,
            IReadOnlyCollection<InventoryItem> items,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class NoOpCurrencyRewardWriter : ICurrencyRewardWriter
    {
        public Task AddAsync(Guid characterId, int cinders, int soulstones, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class CapturingCombatStyleService : ICombatStyleService
    {
        public long ExperienceGranted { get; private set; }
        public string Source { get; private set; } = string.Empty;

        public Task<CombatStylesOverviewModel> GetOverviewAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatStyleOperationResult> ActivateStyleAsync(Guid characterId, string styleId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatStyleOperationResult<CombatStyleModel>> SelectFocusAsync(
            Guid characterId,
            string styleId,
            string focusId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatStyleOperationResult<CombatStyleModel>> RankUpNodeAsync(
            Guid characterId,
            string styleId,
            string nodeId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatStyleOperationResult<CombatStyleModel>> ResetSkillTreeAsync(
            Guid characterId,
            string styleId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatBuildPreviewModel> GetBuildPreviewAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CombatStyleSnapshot?> GetActiveSnapshotAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task GrantExperienceAsync(Guid characterId, long amount, string source, CancellationToken cancellationToken)
        {
            ExperienceGranted += amount;
            Source = source;
            return Task.CompletedTask;
        }
    }

    private sealed class SingleDefinitionProvider : ICombatStyleDefinitionProvider
    {
        private readonly CombatStyleDefinition _definition;

        public SingleDefinitionProvider(CombatStyleDefinition definition)
        {
            _definition = definition;
        }

        public IReadOnlyCollection<CombatStyleDefinition> GetAll() => [_definition];

        public CombatStyleDefinition? GetById(string styleId) =>
            _definition.Id.Equals(styleId, StringComparison.OrdinalIgnoreCase) ? _definition : null;

        public CombatStyleFocusDefinition? GetFocus(string styleId, string focusId) =>
            GetById(styleId)?.Focuses.FirstOrDefault(x => x.Id.Equals(focusId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class EmptyEssenceDefinitionRepository : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [];
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];
        public EssenceDefinition? GetById(string essenceDefinitionId) => null;
        public EssenceDefinition? GetByMonsterId(string monsterId) => null;
        public AbilitySpec? GetAbilityById(string abilityId) => null;
    }
}
