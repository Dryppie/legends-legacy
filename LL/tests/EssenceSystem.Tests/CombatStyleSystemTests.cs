using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.CombatStyles.Dtos;
using Application.UseCases.CombatStyles.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Persistence.LL.Repositories.CombatStyles;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Essences;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Combat.Layers.Resolution.Models;
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
    public void Combat_style_json_definitions_have_valid_skill_tree_rule_references()
    {
        var definitions = CreateDefinitionProvider().GetAll();

        foreach (var style in definitions)
        {
            Assert.NotEmpty(style.SkillTreeNodes);

            var nodeIds = style.SkillTreeNodes.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var focusIds = style.Focuses.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in style.Rules)
                AssertValidRuleReferences(style, rule, nodeIds, focusIds, currentNodeId: null, allowNodeRankModifiers: false);

            foreach (var focus in style.Focuses)
            {
                Assert.Contains(focus.Id, focusIds);
                foreach (var rule in focus.Rules)
                    AssertValidRuleReferences(style, rule, nodeIds, focusIds, currentNodeId: null, allowNodeRankModifiers: false);
            }

            foreach (var node in style.SkillTreeNodes)
            {
                if (style.SkillTreeNodes.Any(x => x.Row > 0))
                    Assert.True(
                        new[] { "left", "middle", "right" }.Contains(node.BranchId, StringComparer.OrdinalIgnoreCase),
                        $"Style '{style.Id}' node '{node.Id}' has invalid lane branch '{node.BranchId}'.");
                else
                    Assert.Contains(node.BranchId, focusIds);
                if (node.RequiredNodeId is not null)
                    Assert.Contains(node.RequiredNodeId, nodeIds);

                foreach (var rule in node.Rules)
                    AssertValidRuleReferences(style, rule, nodeIds, focusIds, node.Id, allowNodeRankModifiers: true);
            }

            foreach (var operation in style.ResourceOverflowOperations)
                AssertValidOperationReferences(style, operation, nodeIds, focusIds, currentNodeId: null, allowNodeRankModifiers: true);
        }
    }

    [Fact]
    public void Combat_style_json_definitions_use_redesigned_skill_tree_shape()
    {
        var definitions = CreateDefinitionProvider().GetAll();

        foreach (var style in definitions)
        {
            var majorNodes = style.SkillTreeNodes
                .Where(node => node.NodeType.Equals(CombatStyleNodeTypes.Major, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var minorNodes = style.SkillTreeNodes
                .Where(node => node.NodeType.Equals(CombatStyleNodeTypes.Minor, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Equal(9, majorNodes.Count);
            Assert.Equal(12, minorNodes.Count);

            foreach (var row in Enumerable.Range(1, 3))
            {
                var rowMajorNodes = majorNodes.Where(node => node.Row == row).ToList();
                Assert.Equal(3, rowMajorNodes.Count);
                Assert.Contains(rowMajorNodes, node => node.Lane == CombatStyleNodeLanes.Left);
                Assert.Contains(rowMajorNodes, node => node.Lane == CombatStyleNodeLanes.Middle);
                Assert.Contains(rowMajorNodes, node => node.Lane == CombatStyleNodeLanes.Right);
            }

            Assert.All(majorNodes.Where(node => node.Row == 2), node =>
            {
                Assert.NotNull(node.Mutator);
                Assert.NotEmpty(node.MutatorGroups);
                Assert.NotEmpty(node.Tooltip.Changes);
            });
        }
    }

    [Fact]
    public void Caster_arcane_armament_mutator_converts_eligible_melee_damage()
    {
        var provider = CreateDefinitionProvider();
        var resolver = new CombatStyleAbilityMutatorResolver(provider);
        var spec = new AbilitySpec
        {
            Id = "test_melee",
            Kind = AbilitySpecKind.Active,
            Tags = ["Melee"],
            DeliveryTags = ["Melee"],
            EffectTags = ["Damage"],
            Costs = [new AbilityCostSpec { Resource = AbilityResourceType.Mana, BaseValue = 100 }],
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "hit",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100,
                    DamageType = DamageType.Physical,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 1f,
                    Tags = ["Damage"]
                }
            ]
        };
        var snapshot = new CombatStyleSnapshot(
            "caster",
            "Caster",
            10,
            0,
            null,
            null,
            new Dictionary<string, int> { ["caster_arcane_armament"] = 1 });

        var modified = resolver.ApplyMutators(spec, snapshot);

        Assert.NotSame(spec, modified);
        Assert.Contains("ConvertedDamage", modified.Tags);
        Assert.Equal(DamageType.Magical, modified.Effects.Single().DamageType);
        Assert.Equal(AttributeType.Spirit, modified.Effects.Single().ScalingAttribute);
        Assert.Equal(105, modified.Effects.Single().BaseValue);
        Assert.Equal(105, modified.Costs.Single().BaseValue);
    }

    [Fact]
    public void Caster_arcane_armament_respects_damage_conversion_flags()
    {
        var provider = CreateDefinitionProvider();
        var resolver = new CombatStyleAbilityMutatorResolver(provider);
        var spec = new AbilitySpec
        {
            Id = "test_melee",
            Kind = AbilitySpecKind.Active,
            Tags = ["Melee"],
            DeliveryTags = ["Melee"],
            EffectTags = ["Damage"],
            ConversionFlags = new AbilityConversionFlags { AllowDamageTypeConversion = false },
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "hit",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 100,
                    DamageType = DamageType.Physical,
                    Tags = ["Damage"]
                }
            ]
        };
        var snapshot = new CombatStyleSnapshot(
            "caster",
            "Caster",
            10,
            0,
            null,
            null,
            new Dictionary<string, int> { ["caster_arcane_armament"] = 1 });

        var modified = resolver.ApplyMutators(spec, snapshot);

        Assert.Same(spec, modified);
        Assert.Equal(DamageType.Physical, modified.Effects.Single().DamageType);
    }

    [Fact]
    public void Mutator_resolver_applies_only_one_mutator_per_group()
    {
        var definition = CreateMutatorTestDefinition(
            CreateMutatorNode(
                "first",
                DamageType.Magical,
                1.10m,
                CombatStyleMutatorGroups.DamageTypeConversion),
            CreateMutatorNode(
                "second",
                DamageType.Burn,
                2m,
                CombatStyleMutatorGroups.DamageTypeConversion));
        var resolver = new CombatStyleAbilityMutatorResolver(new SingleDefinitionProvider(definition));
        var spec = CreatePhysicalDamageSpec();
        var snapshot = new CombatStyleSnapshot(
            "mutator-test",
            "Mutator Test",
            10,
            0,
            null,
            null,
            new Dictionary<string, int>
            {
                ["first"] = 1,
                ["second"] = 1
            });

        var modified = resolver.ApplyMutators(spec, snapshot);

        Assert.Equal(DamageType.Magical, modified.Effects.Single().DamageType);
        Assert.Equal(110, modified.Effects.Single().BaseValue);
    }

    [Fact]
    public void Mutator_resolver_returns_original_spec_when_no_effect_matches()
    {
        var definition = CreateMutatorTestDefinition(
            CreateMutatorNode(
                "first",
                DamageType.Magical,
                1.10m,
                CombatStyleMutatorGroups.DamageTypeConversion));
        var resolver = new CombatStyleAbilityMutatorResolver(new SingleDefinitionProvider(definition));
        var spec = CreatePhysicalDamageSpec();
        spec.Effects.Single().DamageType = DamageType.Poison;
        var snapshot = new CombatStyleSnapshot(
            "mutator-test",
            "Mutator Test",
            10,
            0,
            null,
            null,
            new Dictionary<string, int> { ["first"] = 1 });

        var modified = resolver.ApplyMutators(spec, snapshot);

        Assert.Same(spec, modified);
        Assert.Equal(DamageType.Poison, modified.Effects.Single().DamageType);
    }

    [Fact]
    public async Task Combat_engine_executor_compiles_abilities_after_combat_style_mutators()
    {
        var ability = CreatePhysicalDamageSpec();
        ability.Id = "ability.test.style_mutated_strike";
        ability.Name = "Style Mutated Strike";
        ability.OwningEssenceId = "essence.test.style_mutated";
        ability.Tags = ["Melee"];
        ability.DeliveryTags = ["Melee"];
        ability.EffectTags = ["Damage"];
        ability.CooldownTicks = 999;
        ability.Triggers =
        [
            new AbilityTriggerSpec
            {
                Event = AbilityTriggerEvent.OnAbilityUsed,
                EffectIds = ["hit"]
            }
        ];
        ability.Effects.Single().Tags = ["Damage"];
        ability.Effects.Single().ScalingAttribute = null;
        ability.Effects.Single().ScalingCoefficient = 0f;
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = "essence.test.style_mutated"
            });
        var friendlyCharacter = CreateTestCharacter("Style Mutated Friendly");
        var hostileCharacter = CreateTestCharacter("Style Mutated Hostile");
        var friendlyCombatant = CreateTestCombatEntity(
            "friendly-slot",
            friendlyCharacter,
            "essence.test.style_mutated");
        var hostileCombatant = CreateTestCombatEntity("hostile-slot", hostileCharacter);
        var combatStyle = new CombatStyleSnapshot(
            "caster",
            "Caster",
            10,
            0,
            null,
            null,
            new Dictionary<string, int> { ["caster_arcane_armament"] = 1 });
        var runtime = CreateTestRuntime(
            friendlyCharacter,
            friendlyCombatant,
            hostileCharacter,
            hostileCombatant,
            combatStyle);
        var executor = new CombatEngineExecutor(
            new FakeAbilityCatalogProvider(catalog),
            combatStyleDefinitions: CreateDefinitionProvider());

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(
            result.EventLog,
            item => item.Source == "hit"
                && item.EventType == EventType.Damage
                && item.Magnitude == 105
                && item.Details.Contains("Magical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Redesigned_skill_tree_enforces_major_row_exclusivity()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();
        await service.GrantExperienceAsync(characterId, 60_000, "test", CancellationToken.None);
        await db.SaveChangesAsync();

        var first = await service.RankUpNodeAsync(
            characterId,
            "fighter",
            "fighter_duelists_focus",
            CancellationToken.None);
        await db.SaveChangesAsync();
        var second = await service.RankUpNodeAsync(
            characterId,
            "fighter",
            "fighter_bruisers_grit",
            CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("A major node is already selected in row 1.", second.Message);
    }

    [Fact]
    public async Task Redesigned_skill_tree_uses_lane_branching_between_major_rows()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();
        await service.GrantExperienceAsync(characterId, 60_000, "test", CancellationToken.None);
        await db.SaveChangesAsync();

        var rowOne = await service.RankUpNodeAsync(
            characterId,
            "fighter",
            "fighter_bruisers_grit",
            CancellationToken.None);
        await db.SaveChangesAsync();
        var lockedRight = await service.RankUpNodeAsync(
            characterId,
            "fighter",
            "fighter_bloodied_tempo",
            CancellationToken.None);
        var unlockedMiddle = await service.RankUpNodeAsync(
            characterId,
            "fighter",
            "fighter_martial_imprint",
            CancellationToken.None);

        Assert.True(rowOne.Succeeded);
        Assert.False(lockedRight.Succeeded);
        Assert.Equal("Required node is not unlocked.", lockedRight.Message);
        Assert.True(unlockedMiddle.Succeeded);
    }

    [Fact]
    public async Task Redesigned_skill_tree_row_three_major_sets_build_identity()
    {
        await using var db = CreateDb();
        var characterId = await SeedCharacterAsync(db);
        var service = CreateService(db);
        await service.GetOverviewAsync(characterId, CancellationToken.None);
        await db.SaveChangesAsync();
        await service.GrantExperienceAsync(characterId, 60_000, "test", CancellationToken.None);
        await db.SaveChangesAsync();

        await service.RankUpNodeAsync(characterId, "fighter", "fighter_duelists_focus", CancellationToken.None);
        await db.SaveChangesAsync();
        await service.RankUpNodeAsync(characterId, "fighter", "fighter_martial_imprint", CancellationToken.None);
        await db.SaveChangesAsync();
        var result = await service.RankUpNodeAsync(
            characterId,
            "fighter",
            "fighter_duelists_claim",
            CancellationToken.None);
        await db.SaveChangesAsync();
        var snapshot = await service.GetActiveSnapshotAsync(characterId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("fighter_duelists_claim", result.Value?.SelectedFocusId);
        Assert.Equal("fighter_duelists_claim", snapshot?.SelectedFocusId);
        Assert.Equal("Duelists Claim", snapshot?.SelectedFocusName);
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

        var locked = await service.SelectFocusAsync(characterId, "caster", "caster_grand_channel", CancellationToken.None);
        db.PlayerCombatStyles.Single(x => x.CharacterId == characterId && x.StyleId == "caster").Level = 25;
        await db.SaveChangesAsync();
        var unlocked = await service.SelectFocusAsync(characterId, "caster", "caster_grand_channel", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(locked.Succeeded);
        Assert.True(unlocked.Succeeded);
        Assert.Equal("caster_grand_channel", unlocked.Value?.SelectedFocusId);
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

        var rootRanked = await service.RankUpNodeAsync(characterId, "fighter", "fighter_duelists_focus", CancellationToken.None);
        await db.SaveChangesAsync();
        var ranked = await service.RankUpNodeAsync(characterId, "fighter", "fighter_martial_imprint", CancellationToken.None);
        await db.SaveChangesAsync();
        var snapshot = await service.GetActiveSnapshotAsync(characterId, CancellationToken.None);
        var reset = await service.ResetSkillTreeAsync(characterId, "fighter", CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(rootRanked.Succeeded);
        Assert.Null(rootRanked.Value?.SelectedFocusId);
        Assert.True(ranked.Succeeded);
        Assert.Null(ranked.Value?.SelectedFocusId);
        Assert.Equal(8, ranked.Value?.SkillPointsAvailable);
        Assert.Equal(2, ranked.Value?.SkillPointsSpent);
        Assert.Equal(1, snapshot?.NodeRanks?["fighter_duelists_focus"]);
        Assert.Equal(1, snapshot?.NodeRanks?["fighter_martial_imprint"]);
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

        Assert.Equal(103, empowered);
        Assert.Empty(state!.PendingEmpowerments);
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

        Assert.Equal(125, empowered);
        Assert.Empty(state!.PendingEmpowerments);
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

        Assert.Equal(125, empowered);
        Assert.Empty(state!.PendingEmpowerments);
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

        Assert.Equal(125, empowered);
        Assert.Empty(state!.PendingEmpowerments);
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

    private static CombatStyleDefinition CreateMutatorTestDefinition(params CombatStyleTreeNodeDefinition[] nodes) =>
        new(
            "mutator-test",
            "Mutator Test",
            "Test style",
            "test",
            100,
            50,
            [],
            [],
            [],
            nodes,
            [],
            [],
            "Test");

    private static CombatStyleTreeNodeDefinition CreateMutatorNode(
        string nodeId,
        DamageType damageType,
        decimal potencyMultiplier,
        params string[] groups) =>
        new(
            nodeId,
            CombatStyleNodeLanes.Middle.ToLowerInvariant(),
            nodeId,
            nodeId,
            1,
            1,
            null,
            0,
            0,
            [],
            [],
            false)
        {
            Row = 2,
            Lane = CombatStyleNodeLanes.Middle,
            NodeType = CombatStyleNodeTypes.Major,
            MutatorGroups = groups,
            Mutator = new CombatStyleAbilityMutatorDefinition
            {
                Kind = CombatStyleMutatorKinds.Converter,
                Groups = groups,
                Conditions = new CombatStyleMutatorConditionDefinition
                {
                    EffectOperations = [AbilityEffectOperation.Damage],
                    AllowedDamageTypes = [DamageType.Physical],
                    AllowDamageTypeConversionRequired = true
                },
                Transform = new CombatStyleMutatorTransformDefinition
                {
                    DamageType = damageType,
                    EffectPotencyMultiplier = potencyMultiplier
                }
            }
        };

    private static AbilitySpec CreatePhysicalDamageSpec() =>
        new()
        {
            Id = "physical_damage",
            Kind = AbilitySpecKind.Active,
            Effects =
            [
                new AbilityEffectSpec
                {
                    Id = "hit",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 100,
                    DamageType = DamageType.Physical
                }
            ]
        };

    private static Character CreateTestCharacter(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = Guid.NewGuid(),
            Level = 10,
            BaseCombatAttributes =
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Power] = 10,
                [AttributeType.CritDamage] = 100
            },
            CombatAttributes =
            {
                [AttributeType.MaxHealth] = 1_000,
                [AttributeType.Power] = 10,
                [AttributeType.CritDamage] = 100
            }
        };

    private static CombatEntity CreateTestCombatEntity(
        string runtimeId,
        Character source,
        string? equippedEssenceId = null)
    {
        var combatant = new CombatEntity(source)
        {
            Id = runtimeId,
            Name = source.Name,
            Level = source.Level
        };
        combatant.SyncCurrentHealthToMax();

        if (!string.IsNullOrWhiteSpace(equippedEssenceId))
        {
            combatant.EquippedEssences.Add(new()
            {
                Id = Guid.NewGuid(),
                CharacterId = source.Id,
                EssenceDefinitionId = equippedEssenceId,
                Level = 1
            });
        }

        return combatant;
    }

    private static CombatEncounterRuntime CreateTestRuntime(
        Character friendlyCharacter,
        CombatEntity friendlyCombatant,
        Character hostileCharacter,
        CombatEntity hostileCombatant,
        CombatStyleSnapshot? playerCombatStyle = null)
    {
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)),
            PlayerCombatStyle: playerCombatStyle);

        return new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
    }

    private static void AssertValidRuleReferences(
        CombatStyleDefinition style,
        CombatStyleRuleDefinition rule,
        IReadOnlySet<string> nodeIds,
        IReadOnlySet<string> focusIds,
        string? currentNodeId,
        bool allowNodeRankModifiers) =>
        AssertValidOperationReferences(style, rule.Operation, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);

    private static void AssertValidOperationReferences(
        CombatStyleDefinition style,
        StyleRuleOperation operation,
        IReadOnlySet<string> nodeIds,
        IReadOnlySet<string> focusIds,
        string? currentNodeId,
        bool allowNodeRankModifiers)
    {
        switch (operation)
        {
            case ModifyEffectAmountOperation op:
                AssertValidModifiers(style, op.AdditivePercentModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
            case AddDamageReductionOperation op:
                AssertValidModifiers(style, op.PercentModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
            case GainStyleResourceOperation op:
                Assert.Equal(style.ResourceId, op.ResourceId);
                AssertValidModifiers(style, op.AmountModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
            case AddBonusDamageFromStatOperation op:
                AssertValidModifiers(style, op.CoefficientModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
            case SetPendingEmpowermentOperation op:
                AssertValidModifiers(style, op.AdditivePercentModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
            case ModifySummonStatsOperation op:
                AssertValidModifiers(style, op.MaxHealthPercentModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                AssertValidModifiers(style, op.DamagePercentModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
            case GrantBarrierFromMaxHealthOperation op:
                AssertValidModifiers(style, op.PercentModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                AssertValidModifiers(style, op.MaxTriggerModifiers, nodeIds, focusIds, currentNodeId, allowNodeRankModifiers);
                break;
        }
    }

    private static void AssertValidModifiers(
        CombatStyleDefinition style,
        IReadOnlyList<StyleValueModifier>? modifiers,
        IReadOnlySet<string> nodeIds,
        IReadOnlySet<string> focusIds,
        string? currentNodeId,
        bool allowNodeRankModifiers)
    {
        foreach (var modifier in modifiers ?? [])
        {
            if (modifier.NodeId is not null)
            {
                Assert.True(
                    allowNodeRankModifiers,
                    $"Style '{style.Id}' has nodeRank modifier '{modifier.NodeId}' outside a node rule or resource overflow operation.");
                Assert.Contains(modifier.NodeId, nodeIds);

                if (currentNodeId is not null)
                    Assert.Equal(currentNodeId, modifier.NodeId);
            }

            if (modifier.FocusId is not null)
                Assert.Contains(modifier.FocusId, focusIds);
        }
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "LL", "src", "API", "API.LL");
            if (Directory.Exists(Path.Combine(candidate, "Data", "combat-styles")))
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

    private sealed class FakeAbilityCatalogProvider(AbilityCatalog catalog) : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => catalog;
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


