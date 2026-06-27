using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class AbilitySystemTests
{
    [Fact]
    public void Catalog_indexes_500_authored_abilities_without_scanning_runtime_combat()
    {
        var abilities = Enumerable.Range(0, 500)
            .Select(index => CreateDamageAbility($"ability.scale.{index}", index % 2 == 0 ? "Family.Fire" : "Family.Ice"))
            .ToList();
        var owners = abilities.ToDictionary(x => x.Id, x => $"essence.{x.Id}", StringComparer.OrdinalIgnoreCase);

        var catalog = AbilityCatalogValidator.CreateCatalog(abilities, [CreateBurnStatus()], owners);

        Assert.Equal(500, catalog.AbilitiesById.Count);
        Assert.Equal(500, catalog.AbilityIdsByKind[AbilitySpecKind.Active].Count);
        Assert.Equal(250, catalog.AbilityIdsByTag["Family.Fire"].Count);
        Assert.Equal(500, catalog.AbilityIdsByTrigger[AbilityTriggerEvent.OnAbilityUsed].Count);
        Assert.Equal("essence.ability.scale.42", catalog.OwningEssenceByAbilityId["ability.scale.42"]);
        Assert.Equal(["ability.scale.42"], catalog.AbilityIdsByOwningEssence["essence.ability.scale.42"]);
    }

    [Fact]
    public void Catalog_reports_grouped_validation_failures()
    {
        var invalid = CreateDamageAbility("ability.invalid", "Family.Test");
        invalid.Effects[0].Operation = AbilityEffectOperation.ApplyStatus;
        invalid.Effects[0].StatusId = "missing.status";

        var validation = AbilityCatalogValidator.Validate([invalid], []);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, x => x.Contains("ability.invalid/effect.damage", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, x => x.Contains("missing.status", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Catalog_validates_summon_effect_references()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.missing",
            Kind = AbilitySpecKind.Active,
            Name = "Missing Summon",
            Effects =
            [
                new()
                {
                    Id = "effect.summon",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "missing.summon"
                }
            ]
        };

        var validation = AbilityCatalogValidator.Validate([summonAbility], [], summons: []);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, x => x.Contains("ability.summon.missing/effect.summon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, x => x.Contains("missing.summon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Engine_executes_direct_damage_and_barrier()
    {
        var strike = CreateDamageAbility("ability.strike", "Family.Test");
        var barrier = new AbilitySpec
        {
            Id = "ability.barrier",
            Kind = AbilitySpecKind.Active,
            Name = "Barrier",
            Effects =
            [
                new()
                {
                    Id = "effect.barrier",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 15
                }
            ]
        };

        var result = RunBattle([strike, barrier], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.True(hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(15, friendly.Barrier);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.Damage && x.Source == "effect.damage");
        Assert.Contains(result.EventLog, x => x.EventType == EventType.RestoreBarrier && x.Source == "effect.barrier");
    }

    [Fact]
    public void Engine_pays_health_cost_before_using_active_ability()
    {
        var ability = CreateDamageAbility("ability.health.cost", "Family.Test");
        ability.Costs.Add(new AbilityCostSpec
        {
            Resource = AbilityResourceType.Health,
            BaseValue = 25
        });

        var result = RunBattle([ability], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.Equal(175, friendly.Health);
        Assert.Equal(180, hostile.Health);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name);
    }

    [Fact]
    public void Engine_does_not_use_active_ability_when_health_cost_cannot_be_paid()
    {
        var ability = CreateDamageAbility("ability.health.cost.unpaid", "Family.Test");
        ability.Costs.Add(new AbilityCostSpec
        {
            Resource = AbilityResourceType.Health,
            BaseValue = 200
        });

        var result = RunBattle([ability], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.Equal(200, friendly.Health);
        Assert.Equal(200, hostile.Health);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name);
    }

    [Fact]
    public void Engine_recognizes_mana_costs_as_unpayable_until_mana_runtime_exists()
    {
        var ability = CreateDamageAbility("ability.mana.cost", "Family.Test");
        ability.Costs.Add(new AbilityCostSpec
        {
            Resource = AbilityResourceType.Mana,
            BaseValue = 5
        });

        var result = RunBattle([ability], [], maxTicks: 1, out var friendly, out var hostile);

        Assert.Equal(200, friendly.Health);
        Assert.Equal(200, hostile.Health);
        Assert.DoesNotContain(result.EventLog, x => x.EventType == EventType.AbilityUse && x.Source == ability.Name);
    }

    [Fact]
    public void Engine_stats_record_actual_health_damage_not_overkill()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.overkill",
            Kind = AbilitySpecKind.Active,
            Name = "Overkill",
            Effects =
            [
                new()
                {
                    Id = "effect.overkill.damage",
                    Operation = AbilityEffectOperation.Damage,
                    BaseValue = 20
                }
            ]
        };
        var abilities = AbilityCompiler.CompileAbilities([ability]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 5);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        var damageEvent = Assert.Single(result.EventLog, x => x.Source == "effect.overkill.damage" && x.EventType == EventType.Damage);
        Assert.Equal(5, damageEvent.Magnitude);
        Assert.Equal(5, result.EntityStats.Single(x => x.EntityId == "friendly").DamageDone);
        Assert.Equal(5, result.EntityStats.Single(x => x.EntityId == "hostile").DamageTaken);
    }

    [Fact]
    public void Engine_prefers_taunting_targets_for_basic_attacks()
    {
        var taunt = new StatusSpec
        {
            Id = "status.taunt",
            Name = "Taunt",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 30,
            Tags = ["Control.Taunt"]
        };
        var statuses = AbilityCompiler.CompileStatuses([taunt]);
        var front = CreateCombatant("front", CombatTeam.Friendly, []);
        var taunter = CreateCombatant("taunter", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        taunter.Statuses.Add(new RuntimeStatus(statuses["status.taunt"], taunter, taunter, 1));
        var engine = new FastCombatEngine(
            statuses,
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([front, taunter], [hostile]);

        Assert.Contains(result.EventLog, x =>
            x.ActorId == "hostile"
            && x.Source == "Basic Attack"
            && x.EventType == EventType.Damage
            && x.TargetId == "taunter");
        Assert.DoesNotContain(result.EventLog, x =>
            x.ActorId == "hostile"
            && x.Source == "Basic Attack"
            && x.EventType == EventType.Damage
            && x.TargetId == "front");
    }

    [Fact]
    public void Engine_supports_real_catalog_selectors()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.cleave",
                    Kind = AbilitySpecKind.Active,
                    Name = "Cleave",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.two.enemies",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.TwoEnemies,
                            BaseValue = 10
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.group.guard",
                    Kind = AbilitySpecKind.Active,
                    Name = "Group Guard",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.two.allies",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.TwoAllies,
                            BaseValue = 5
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.protect.large",
                    Kind = AbilitySpecKind.Active,
                    Name = "Protect Large",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.highest.max.health",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.HighestMaxHealthAlly,
                            BaseValue = 9
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var highHealthAlly = CreateCombatant("high-health-ally", CombatTeam.Friendly, [], maxHealth: 300);
        var firstHostile = CreateCombatant("hostile-1", CombatTeam.Hostile, []);
        var secondHostile = CreateCombatant("hostile-2", CombatTeam.Hostile, []);
        var thirdHostile = CreateCombatant("hostile-3", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly, ally, highHealthAlly], [firstHostile, secondHostile, thirdHostile]);

        Assert.Equal(2, new[] { firstHostile, secondHostile, thirdHostile }.Count(x => x.Health < x.GetAttribute(AttributeType.MaxHealth)));
        Assert.Equal(
            new[] { "hostile-1", "hostile-2" },
            result.EventLog
                .Where(x => x.Source == "effect.two.enemies" && x.EventType == EventType.Damage)
                .Select(x => x.TargetId)
                .ToArray());
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "effect.two.allies" && x.EventType == EventType.RestoreBarrier));
        Assert.Single(result.EventLog, x => x.Source == "effect.highest.max.health" && x.TargetId == "high-health-ally");
        Assert.Equal(9, highHealthAlly.Barrier);
    }

    [Fact]
    public void Engine_supports_restore_resource_lifesteal_and_real_catalog_events()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.self.wound",
                    Kind = AbilitySpecKind.Active,
                    Name = "Self Wound",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.self.wound",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.Self,
                            BaseValue = 50
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.restore.barrier",
                    Kind = AbilitySpecKind.Active,
                    Name = "Restore Barrier",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.restore.barrier",
                            Operation = AbilityEffectOperation.RestoreResource,
                            Target = AbilityTargetSelector.Self,
                            Resource = AbilityResourceType.Barrier,
                            BaseValue = 12
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.life.drain",
                    Kind = AbilitySpecKind.Active,
                    Name = "Life Drain",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.life.drain",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 40,
                            AttackType = AttackType.Melee,
                            LifeStealPercentage = 50
                        }
                    ]
                },
                CreatePassiveBarrier("ability.on.melee", "effect.on.melee", AbilityTriggerEvent.OnMeleeAttack, 3),
                CreatePassiveBarrier("ability.on.health.changed", "effect.on.health.changed", AbilityTriggerEvent.OnHealthChanged, 4),
                CreatePassiveBarrier("ability.on.heal", "effect.on.heal", AbilityTriggerEvent.OnHeal, 5),
                CreatePassiveBarrier("ability.on.lifesteal", "effect.on.lifesteal", AbilityTriggerEvent.OnLifestealHeal, 6)
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Contains(result.EventLog, x => x.Source == "effect.restore.barrier" && x.EventType == EventType.RestoreBarrier && x.Magnitude == 12);
        Assert.Contains(result.EventLog, x => x.Source == "effect.life.drain" && x.EventType == EventType.Heal && x.Magnitude == 20);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.melee" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.health.changed" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.heal" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.lifesteal" && x.EventType == EventType.RestoreBarrier);
    }

    [Fact]
    public void Engine_limits_melee_attacked_passives_to_the_attacked_owner()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.melee.tap",
                    Kind = AbilitySpecKind.Active,
                    Name = "Melee Tap",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.melee.tap",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 1,
                            AttackType = AttackType.Melee
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.hot.aura",
                    Kind = AbilitySpecKind.Passive,
                    Name = "Hot Aura",
                    Triggers = [new() { Event = AbilityTriggerEvent.OnMeleeAttacked }],
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.hot_aura.damage",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.EventTarget,
                            BaseValue = 4,
                            AttackType = AttackType.None,
                            DamageType = DamageType.Burn
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 200);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.DoesNotContain(result.EventLog, x =>
            x.Source == "effect.hot_aura.damage"
            && x.TargetId == "friendly");
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.hot_aura.damage"
            && x.TargetId == "hostile"
            && x.Magnitude == 4);

        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly");
        var hotAura = friendlyStats.Abilities.Single(x => x.Name == "Hot Aura");
        Assert.Equal(4, hotAura.TotalDamage);
    }

    [Fact]
    public void Engine_supports_cooldown_restore_resource()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.execute",
                    Kind = AbilitySpecKind.Active,
                    Name = "Execute",
                    CooldownTicks = 20,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.execute",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 50,
                            AttackType = AttackType.Melee
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.on.kill.cooldown",
                    Kind = AbilitySpecKind.Passive,
                    Name = "On Kill Cooldown",
                    Triggers =
                    [
                        new()
                        {
                            Event = AbilityTriggerEvent.OnKill,
                            EffectIds = [ "effect.restore.cooldown" ]
                        }
                    ],
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.restore.cooldown",
                            Operation = AbilityEffectOperation.RestoreResource,
                            Target = AbilityTargetSelector.Self,
                            Resource = AbilityResourceType.Cooldown,
                            BaseValue = 5
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 20);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        var execute = friendly.Abilities.Single(x => x.Definition.Id == "ability.execute");
        Assert.True(execute.RemainingCooldownTicks < 19);
        Assert.Contains(result.EventLog, x => x.Source == "effect.restore.cooldown" && x.EventType == EventType.Buff);
    }

    [Fact]
    public void Engine_does_not_spend_active_cooldown_when_no_effect_can_resolve()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.requires.status",
                    Kind = AbilitySpecKind.Active,
                    Name = "Requires Status",
                    CooldownTicks = 100,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.requires.status",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 10,
                            Conditions =
                            [
                                new()
                                {
                                    Type = AbilityConditionType.HasStatus,
                                    Subject = AbilityConditionSubject.Target,
                                    StatusId = "status.missing"
                                }
                            ]
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        var ability = friendly.Abilities.Single(x => x.Definition.Id == "ability.requires.status");
        Assert.Equal(0, ability.RemainingCooldownTicks);
        Assert.DoesNotContain(result.EventLog, x => x.ActorId == "friendly" && x.EventType == EventType.AbilityUse);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.requires.status");
    }

    [Fact]
    public void Engine_stops_using_active_abilities_after_last_opponent_dies()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.killing.blow",
                    Kind = AbilitySpecKind.Active,
                    Name = "Killing Blow",
                    CooldownTicks = 100,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.killing.blow",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 50
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.after.kill",
                    Kind = AbilitySpecKind.Active,
                    Name = "After Kill",
                    CooldownTicks = 100,
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.after.kill",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 10
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [], maxHealth: 20);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        var killingBlow = friendly.Abilities.Single(x => x.Definition.Id == "ability.killing.blow");
        var afterKill = friendly.Abilities.Single(x => x.Definition.Id == "ability.after.kill");
        Assert.True(killingBlow.RemainingCooldownTicks > 0);
        Assert.Equal(0, afterKill.RemainingCooldownTicks);
        Assert.Contains(result.EventLog, x => x.Source == "effect.killing.blow" && x.EventType == EventType.Damage);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.after.kill");
        Assert.DoesNotContain(result.EventLog, x => x.Source == "After Kill" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public void Engine_honors_limited_uses_for_immediate_trigger_effects()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.self.wound",
                    Kind = AbilitySpecKind.Active,
                    Name = "Self Wound",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.self.wound",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.Self,
                            BaseValue = 5
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.one.use.guard",
                    Kind = AbilitySpecKind.Passive,
                    Name = "One Use Guard",
                    Triggers = [new() { Event = AbilityTriggerEvent.OnHealthChanged }],
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.one.use.guard",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.Self,
                            BaseValue = 3,
                            Uses = 1
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.one.use.guard" && x.EventType == EventType.RestoreBarrier);
        Assert.Equal(3, friendly.Barrier);
    }

    [Fact]
    public void Engine_honors_limited_uses_across_multi_target_effects()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.limited.cleave",
                    Kind = AbilitySpecKind.Active,
                    Name = "Limited Cleave",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.limited.cleave",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.TwoEnemies,
                            BaseValue = 10,
                            Uses = 1
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var firstHostile = CreateCombatant("hostile-1", CombatTeam.Hostile, []);
        var secondHostile = CreateCombatant("hostile-2", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [firstHostile, secondHostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.limited.cleave" && x.EventType == EventType.Damage);
        Assert.True(firstHostile.Health < firstHostile.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(secondHostile.GetAttribute(AttributeType.MaxHealth), secondHostile.Health);
    }

    [Fact]
    public void Engine_supports_dodge_triggers()
    {
        var friendlyAbilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.melee.strike",
                    Kind = AbilitySpecKind.Active,
                    Name = "Melee Strike",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.melee.strike",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 40,
                            AttackType = AttackType.Melee
                        }
                    ]
                }
            ]);
        var hostileAbilities = AbilityCompiler.CompileAbilities(
            [
                CreatePassiveBarrier("ability.on.dodge", "effect.on.dodge", AbilityTriggerEvent.OnDodge, 7)
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, friendlyAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, hostileAbilities.Values, dodgeChance: 100);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, RandomSeed: 7));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(hostile.GetAttribute(AttributeType.MaxHealth), hostile.Health);
        Assert.Contains(result.EventLog, x => x.Source == "effect.melee.strike" && x.EventType == EventType.Miss);
        Assert.Contains(result.EventLog, x => x.Source == "effect.on.dodge" && x.EventType == EventType.RestoreBarrier && x.TargetId == "hostile");
    }

    [Fact]
    public void Engine_applies_status_and_runs_damage_over_time()
    {
        var ignite = new AbilitySpec
        {
            Id = "ability.ignite",
            Kind = AbilitySpecKind.Active,
            Name = "Ignite",
            Effects =
            [
                new()
                {
                    Id = "effect.apply.burn",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.burn",
                    BaseValue = 1
                }
            ]
        };

        var result = RunBattle([ignite], [CreateBurnStatus()], maxTicks: 8, out _, out var hostile);

        Assert.Contains(hostile.Statuses, x => x.Definition.Id == "status.burn");
        Assert.True(result.EventLog.Count(x => x.Source == "effect.burn.dot" && x.EventType == EventType.Damage) >= 2);
    }

    [Fact]
    public void Engine_expires_timed_attribute_buffs()
    {
        var frenzy = new AbilitySpec
        {
            Id = "ability.frenzy",
            Kind = AbilitySpecKind.Active,
            Name = "Frenzy",
            CooldownTicks = 100,
            Effects =
            [
                new()
                {
                    Id = "effect.power.buff",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.Self,
                    Attribute = AttributeType.Power,
                    BaseValue = 20,
                    DurationTicks = 3
                }
            ]
        };

        var result = RunBattle([frenzy], [], maxTicks: 4, out var friendly, out _);

        Assert.Equal(50, friendly.GetAttribute(AttributeType.Power));
        Assert.Contains(result.EventLog, x => x.EventType == EventType.Buff);
        Assert.Contains(result.EventLog, x => x.EventType == EventType.BuffExpired);
    }

    [Fact]
    public void Json_catalog_large_rat_big_increases_current_health_with_max_health()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.essence.legacy.large_rat.big"]]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        engine.Run([friendly], [hostile]);

        Assert.Equal(250, friendly.GetAttribute(AttributeType.MaxHealth));
        Assert.Equal(250, friendly.Health);
    }

    [Fact]
    public void Json_catalog_authors_proc_coefficients_for_all_effects()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var abilityEffects = catalog.AbilitiesById.Values.SelectMany(x => x.Effects);
        var statusEffects = catalog.Statuses.SelectMany(x => x.Effects);
        var effects = abilityEffects.Concat(statusEffects).ToArray();

        Assert.NotEmpty(effects);
        Assert.All(effects, effect => Assert.InRange(effect.ProcCoefficient, 0.01m, 2m));
        Assert.Contains(effects, effect => effect.ProcCoefficient < 1m);
        Assert.Contains(effects, effect => effect.Operation == AbilityEffectOperation.Damage && effect.ProcCoefficient < 1m);
    }

    [Fact]
    public void Balance_simulator_ranks_random_essence_combinations()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var simulator = new AbilityBalanceSimulator(provider, essenceRepository);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 20,
            TeamSize: 2,
            EssencesPerParticipant: 2,
            RandomSeed: 123,
            TopResults: 10,
            CandidatePoolSize: 5,
            CandidateTeams: null));

        Assert.Equal("RandomPool", report.Mode);
        Assert.Equal(20, report.BattlesRun);
        Assert.Equal(2, report.TeamSize);
        Assert.Equal(2, report.EssencesPerParticipant);
        Assert.Equal(5, report.CandidatePoolSize);
        Assert.Equal(5, report.CandidateTeamCount);
        Assert.NotEmpty(report.RankedCombinations);
        Assert.True(report.RankedCombinations.Count <= 5);
        Assert.Equal(40, report.RankedCombinations.Sum(x => x.Battles));
        Assert.All(report.RankedCombinations, combination =>
        {
            Assert.DoesNotContain("essence.", combination.DisplayName, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(combination.Signature, combination.DisplayName);
        });
        Assert.All(report.RankedCombinations, combination =>
        {
            Assert.True(combination.Battles > 0);
            Assert.InRange(combination.WinRate, 0, 1);
            Assert.Equal(2, combination.Participants.Count);
            Assert.All(combination.Participants, participant => Assert.Equal(2, participant.EssenceIds.Count));
        });
    }

    [Fact]
    public void Balance_simulator_runs_saved_combinations_as_round_robin()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var simulator = new AbilityBalanceSimulator(provider, essenceRepository);
        var first = new AbilityBalanceTeamLoadout(
            [new AbilityBalanceParticipantLoadout(["essence.legacy.large_rat"])]);
        var second = new AbilityBalanceTeamLoadout(
            [new AbilityBalanceParticipantLoadout(["essence.legacy.flame_imp"])]);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 6,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: 456,
            TopResults: 10,
            CandidatePoolSize: 10,
            CandidateTeams: [first, second]));

        Assert.Equal("SavedRoundRobin", report.Mode);
        Assert.Equal(6, report.BattlesRun);
        Assert.Equal(2, report.CandidateTeamCount);
        Assert.Equal(10, report.CandidatePoolSize);
        Assert.Equal(2, report.RankedCombinations.Count);
        Assert.Contains(report.RankedCombinations, combination => combination.DisplayName == "Large Rat's Essence");
        Assert.Contains(report.RankedCombinations, combination => combination.DisplayName == "Flame Imp's Essence");
        Assert.All(report.RankedCombinations, combination => Assert.Equal(6, combination.Battles));
    }

    [Fact]
    public void Engine_supports_status_stacks_and_reflect_triggers()
    {
        var thorns = new AbilitySpec
        {
            Id = "ability.thorns",
            Kind = AbilitySpecKind.Active,
            Name = "Thorns",
            Effects =
            [
                new()
                {
                    Id = "effect.apply.thorns",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.Self,
                    StatusId = "status.thorns",
                    BaseValue = 1
                }
            ]
        };

        var result = RunBattle([thorns], [CreateThornsStatus()], maxTicks: 35, out _, out var hostile);

        Assert.Contains(result.EventLog, x => x.Source == "effect.thorns.reflect" && x.EventType == EventType.Damage);
        Assert.True(hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
    }

    [Fact]
    public void Engine_applies_damage_reduction_before_health_damage()
    {
        var hostileAbility = CreateDamageAbility("ability.reduced_hit", "Family.Test");
        var compiledAbilities = AbilityCompiler.CompileAbilities([hostileAbility]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, compiledAbilities.Values);
        friendly.AdjustAttribute(AttributeType.DamageReduction, 25);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(185, friendly.Health);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage" && x.EventType == EventType.Damage && x.Magnitude == 15);
    }

    [Fact]
    public void Json_catalog_skeleton_warrior_spiked_defense_reflects_melee_attackers()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.essence.legacy.skeleton_warrior.spiked_defense"]]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.spiked_defense.reflect"
            && x.ActorId == "friendly"
            && x.TargetId == "hostile"
            && x.EventType == EventType.Damage
            && x.Magnitude == 6);
    }

    [Fact]
    public void Json_catalog_plague_swipe_applies_two_target_dot()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.essence.legacy.plague_ghoul.plague_swipe"]]);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var firstHostile = CreateCombatant("hostile-1", CombatTeam.Hostile, []);
        var secondHostile = CreateCombatant("hostile-2", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 12, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [firstHostile, secondHostile]);

        Assert.Contains(result.EventLog, x => x.Source == "status.plague_swipe_poison" && x.TargetId == "hostile-1");
        Assert.Contains(result.EventLog, x => x.Source == "status.plague_swipe_poison" && x.TargetId == "hostile-2");
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "effect.plague_swipe.damage" && x.EventType == EventType.Damage));
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "effect.plague_swipe.dot" && x.EventType == EventType.Damage));
    }

    [Fact]
    public void Json_catalog_protective_bone_barrier_grants_periodic_barrier()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.essence.legacy.skeleton_mage.protective_bone_barrier"]]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 101, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(25, friendly.Barrier);
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.protective_bone_barrier.periodic"
            && x.TargetId == "friendly"
            && x.EventType == EventType.RestoreBarrier
            && x.Magnitude == 25);
    }

    [Fact]
    public void Json_catalog_vile_feast_heals_once_on_death()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.essence.legacy.ravenous_ghoul.vile_feast"]]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        friendly.AdjustHealth(-120);
        var firstHostile = CreateCombatant("hostile-1", CombatTeam.Hostile, [], maxHealth: 1);
        var secondHostile = CreateCombatant("hostile-2", CombatTeam.Hostile, [], maxHealth: 1);
        var engine = new FastCombatEngine(
            AbilityCompiler.CompileStatuses(catalog.Statuses),
            new FastCombatEngineOptions(MaxTicks: 3, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [firstHostile, secondHostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.vile_feast.heal" && x.EventType == EventType.Heal);
        Assert.Contains(result.EventLog, x => x.Source == "effect.vile_feast.heal" && x.Magnitude == 120);
    }

    [Fact]
    public void Json_catalog_illusion_fox_foxfire_only_retaliates_when_owner_is_attacked()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [catalog.AbilitiesById["ability.essence.legacy.illusion_fox.foxfire_wisp"]]);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);

        var foxOwner = CreateCombatant("fox-owner", CombatTeam.Friendly, compiledAbilities.Values);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var attacker = CreateCombatant("attacker", CombatTeam.Hostile, []);
        var allyTargetedEngine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 60));

        var allyTargetedResult = allyTargetedEngine.Run([ally, foxOwner], [attacker]);

        Assert.Contains(allyTargetedResult.EventLog, x =>
            x.Source == "status.foxfire_stack"
            && x.ActorId == "fox-owner"
            && x.TargetId == "fox-owner"
            && x.EventType == EventType.StatusEffect);
        Assert.DoesNotContain(allyTargetedResult.EventLog, x =>
            x.Source == "status.foxfire_stack"
            && x.TargetId == "ally");
        Assert.DoesNotContain(allyTargetedResult.EventLog, x => x.Source == "effect.foxfire.damage");

        foxOwner = CreateCombatant("fox-owner", CombatTeam.Friendly, compiledAbilities.Values);
        ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        attacker = CreateCombatant("attacker", CombatTeam.Hostile, []);
        var ownerTargetedEngine = new FastCombatEngine(
            compiledStatuses,
            new FastCombatEngineOptions(MaxTicks: 61, BasicAttackIntervalTicks: 60));

        var ownerTargetedResult = ownerTargetedEngine.Run([foxOwner, ally], [attacker]);

        Assert.Contains(ownerTargetedResult.EventLog, x =>
            x.Source == "effect.foxfire.damage"
            && x.ActorId == "fox-owner"
            && x.TargetId == "attacker"
            && x.EventType == EventType.Damage
            && x.Magnitude == 8);
        Assert.Equal(0, foxOwner.GetStatusStacks("status.foxfire_stack"));
    }

    [Fact]
    public void Engine_stunned_combatants_skip_active_abilities_and_basic_attacks()
    {
        var hostileAbility = CreateDamageAbility("ability.stunned.hit", "Family.Test");
        var statuses = AbilityCompiler.CompileStatuses([CreateStunStatus()]);
        var hostileAbilities = AbilityCompiler.CompileAbilities([hostileAbility]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, hostileAbilities.Values);
        hostile.Statuses.Add(new RuntimeStatus(statuses["status.stunned"], hostile, hostile, 1));
        var engine = new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(200, friendly.Health);
        Assert.DoesNotContain(result.EventLog, x => x.ActorId == "hostile" && x.EventType is EventType.AbilityUse or EventType.Damage);
    }

    [Fact]
    public void Json_catalog_feral_pounce_stun_blocks_actions()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var hostileAbilities = AbilityCompiler.CompileAbilities([CreateDamageAbility("ability.hostile.hit", "Family.Test")]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, []);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, hostileAbilities.Values);
        hostile.Statuses.Add(new RuntimeStatus(statuses["status.feral_pounce_stunned"], hostile, hostile, 1));
        var engine = new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Equal(200, friendly.Health);
        Assert.DoesNotContain(result.EventLog, x => x.ActorId == "hostile" && x.EventType is EventType.AbilityUse or EventType.Damage);
    }

    [Fact]
    public void Engine_refresh_status_reapplies_duration_without_stacking()
    {
        var refreshStatus = CreateEmptyStatus("status.refresh", AbilityStatusStackingPolicy.Refresh, maxStacks: 5, durationTicks: 10);
        var applyTwice = new AbilitySpec
        {
            Id = "ability.apply.refresh.twice",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Refresh Twice",
            Effects =
            [
                CreateApplyStatusEffect("effect.apply.refresh.one", "status.refresh"),
                CreateApplyStatusEffect("effect.apply.refresh.two", "status.refresh")
            ]
        };

        var result = RunBattle([applyTwice], [refreshStatus], maxTicks: 1, out _, out var hostile);

        Assert.Equal(1, hostile.GetStatusStacks("status.refresh"));
        Assert.Equal(2, result.EventLog.Count(x => x.Source == "status.refresh" && x.EventType == EventType.StatusEffect));
    }

    [Fact]
    public void Engine_stack_status_accumulates_to_max_stacks()
    {
        var stackStatus = CreateEmptyStatus("status.stack", AbilityStatusStackingPolicy.Stack, maxStacks: 3, durationTicks: 10);
        var applyFourTimes = new AbilitySpec
        {
            Id = "ability.apply.stack.four",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Stack Four",
            Effects =
            [
                CreateApplyStatusEffect("effect.apply.stack.one", "status.stack"),
                CreateApplyStatusEffect("effect.apply.stack.two", "status.stack"),
                CreateApplyStatusEffect("effect.apply.stack.three", "status.stack"),
                CreateApplyStatusEffect("effect.apply.stack.four", "status.stack")
            ]
        };

        RunBattle([applyFourTimes], [stackStatus], maxTicks: 1, out _, out var hostile);

        Assert.Equal(3, hostile.GetStatusStacks("status.stack"));
    }

    [Fact]
    public void Engine_modify_status_stacks_to_zero_removes_status()
    {
        var stackStatus = CreateEmptyStatus("status.stack.consume", AbilityStatusStackingPolicy.Stack, maxStacks: 3, durationTicks: 10);
        var consume = new AbilitySpec
        {
            Id = "ability.consume.stack",
            Kind = AbilitySpecKind.Active,
            Name = "Consume Stack",
            Effects =
            [
                new()
                {
                    Id = "effect.consume.stack",
                    Operation = AbilityEffectOperation.ModifyStatusStacks,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.stack.consume",
                    BaseValue = -1
                }
            ]
        };
        var compiledStatuses = AbilityCompiler.CompileStatuses([stackStatus]);
        var compiledAbilities = AbilityCompiler.CompileAbilities([consume]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        hostile.Statuses.Add(new RuntimeStatus(compiledStatuses["status.stack.consume"], hostile, hostile, 1));
        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Empty(hostile.Statuses);
        Assert.Contains(result.EventLog, x => x.Source == "status.stack.consume" && x.EventType == EventType.StatusEffectExpired);
    }

    [Fact]
    public void Engine_remove_status_and_cleanse_clear_statuses()
    {
        var removable = CreateEmptyStatus("status.removable", AbilityStatusStackingPolicy.Refresh, maxStacks: 1, durationTicks: 100);
        var lingering = CreateEmptyStatus("status.lingering", AbilityStatusStackingPolicy.Refresh, maxStacks: 1, durationTicks: 100);
        var removeAndCleanse = new AbilitySpec
        {
            Id = "ability.remove.cleanse",
            Kind = AbilitySpecKind.Active,
            Name = "Remove And Cleanse",
            Effects =
            [
                new()
                {
                    Id = "effect.remove.status",
                    Operation = AbilityEffectOperation.RemoveStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    StatusId = "status.removable"
                },
                new()
                {
                    Id = "effect.cleanse",
                    Operation = AbilityEffectOperation.Cleanse,
                    Target = AbilityTargetSelector.CurrentTarget
                }
            ]
        };
        var compiledStatuses = AbilityCompiler.CompileStatuses([removable, lingering]);
        var compiledAbilities = AbilityCompiler.CompileAbilities([removeAndCleanse]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        hostile.Statuses.Add(new RuntimeStatus(compiledStatuses["status.removable"], hostile, hostile, 1));
        hostile.Statuses.Add(new RuntimeStatus(compiledStatuses["status.lingering"], hostile, hostile, 1));
        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(MaxTicks: 1));

        var result = engine.Run([friendly], [hostile]);

        Assert.Empty(hostile.Statuses);
        Assert.Contains(result.EventLog, x => x.Source == "effect.remove.status" && x.EventType == EventType.StatusEffectExpired);
        Assert.Contains(result.EventLog, x => x.Source == "effect.cleanse" && x.EventType == EventType.StatusEffectExpired);
    }

    [Fact]
    public void Engine_status_applied_timed_attribute_buff_expires_cleanly()
    {
        var buffStatus = CreateTimedPowerBuffStatus();
        var applyBuff = new AbilitySpec
        {
            Id = "ability.apply.power.status",
            Kind = AbilitySpecKind.Active,
            Name = "Apply Power Status",
            CooldownTicks = 100,
            Effects = [CreateApplyStatusEffect("effect.apply.power.status", buffStatus.Id, AbilityTargetSelector.Self)]
        };

        var result = RunBattle([applyBuff], [buffStatus], maxTicks: 4, out var friendly, out _);

        Assert.Equal(50, friendly.GetAttribute(AttributeType.Power));
        Assert.Contains(result.EventLog, x => x.Source == "effect.status.power.buff" && x.EventType == EventType.Buff);
        Assert.Contains(result.EventLog, x => x.Source == "effect.status.power.buff" && x.EventType == EventType.BuffExpired);
    }

    [Fact]
    public void Engine_is_seed_deterministic()
    {
        var ability = CreateDamageAbility("ability.seeded", "Family.Test");

        var first = RunBattle([ability], [], maxTicks: 5, out _, out _, seed: 99);
        var second = RunBattle([ability], [], maxTicks: 5, out _, out _, seed: 99);

        Assert.Equal(
            first.EventLog.Select(x => (x.Timestamp, x.Source, x.EventType, x.Magnitude)).ToList(),
            second.EventLog.Select(x => (x.Timestamp, x.Source, x.EventType, x.Magnitude)).ToList());
    }

    [Fact]
    public void Json_catalog_loads_compiles_and_runs_seeded_battle()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();

        Assert.Contains("ability.training.strike", catalog.AbilitiesById.Keys);
        Assert.Contains("status.training.burn", catalog.StatusesById.Keys);
        Assert.Equal("essence.training", catalog.OwningEssenceByAbilityId["ability.training.strike"]);

        var compiledAbilities = AbilityCompiler.CompileAbilities(
            [
                catalog.AbilitiesById["ability.training.strike"],
                catalog.AbilitiesById["ability.training.guard"],
                catalog.AbilitiesById["ability.training.burn"]
            ]);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var friendly = CreateCombatant("json-friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("json-hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(MaxTicks: 40, RandomSeed: 7));

        var result = engine.Run([friendly], [hostile]);

        Assert.True(hostile.Health < hostile.GetAttribute(AttributeType.MaxHealth));
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x => x.Source == "effect.barrier.main" && x.EventType == EventType.RestoreBarrier);
        Assert.Contains(result.EventLog, x => x.Source == "effect.burn.dot" && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x => x.Source == "effect.reflect.damage" && x.EventType == EventType.Damage);
    }

    [Fact]
    public void Json_catalog_fixed_seed_golden_1v1_covers_buff_damage_reflect_and_defense()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var friendlyAbilities = CompileCatalogAbilities(
            catalog,
            "ability.essence.legacy.glade_panther.ambush_strike",
            "ability.essence.legacy.glade_panther.razor_claws");
        var hostileAbilities = CompileCatalogAbilities(
            catalog,
            "ability.essence.legacy.skeleton_warrior.bone_shield",
            "ability.essence.legacy.skeleton_warrior.spiked_defense");
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var panther = CreateCombatant("panther", CombatTeam.Friendly, friendlyAbilities.Values);
        var skeleton = CreateCombatant("skeleton", CombatTeam.Hostile, hostileAbilities.Values);
        var engine = new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 41));

        var result = engine.Run([panther], [skeleton]);

        Assert.Equal(BattleOutcome.Draw, result.Outcome);
        Assert.Equal(1, result.Duration);
        Assert.Equal(194, panther.Health);
        Assert.Equal(175, skeleton.Health);
        Assert.Equal(115, panther.GetAttribute(AttributeType.CritDamage));
        Assert.Equal(10, skeleton.GetAttribute(AttributeType.DamageReduction));
        Assert.Equal(
            new (int Timestamp, string Source, EventType EventType, string? TargetId, int Magnitude)[]
            {
                (0, "effect.razor_claws.crit_damage", EventType.Buff, "panther", 15),
                (0, "Ambush Strike", EventType.AbilityUse, null, 0),
                (0, "effect.ambush.damage", EventType.Damage, "skeleton", 25),
                (0, "effect.spiked_defense.reflect", EventType.Damage, "panther", 6),
                (0, "Bone Shield", EventType.AbilityUse, null, 0),
                (0, "effect.bone_shield.damage_reduction", EventType.Buff, "skeleton", 10)
            },
            result.EventLog.Select(x => (x.Timestamp, x.Source, x.EventType, (string?)x.TargetId, x.Magnitude)).ToArray());
    }

    [Fact]
    public void Json_catalog_fixed_seed_golden_team_fight_covers_death_and_one_time_heal()
    {
        var catalog = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();
        var mageAbilities = CompileCatalogAbilities(
            catalog,
            "ability.essence.legacy.skeleton_mage.siphon");
        var pantherAbilities = CompileCatalogAbilities(
            catalog,
            "ability.essence.legacy.glade_panther.ambush_strike");
        var ghoulAbilities = CompileCatalogAbilities(
            catalog,
            "ability.essence.legacy.ravenous_ghoul.vile_feast");
        var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var mage = CreateCombatant("mage", CombatTeam.Friendly, mageAbilities.Values);
        var panther = CreateCombatant("panther", CombatTeam.Friendly, pantherAbilities.Values);
        var ghoul = CreateCombatant("ghoul", CombatTeam.Friendly, ghoulAbilities.Values);
        ghoul.AdjustHealth(-50);
        var frontTarget = CreateCombatant("front-target", CombatTeam.Hostile, [], maxHealth: 40);
        var backTarget = CreateCombatant("back-target", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(statuses, new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000, RandomSeed: 41));

        var result = engine.Run([mage, panther, ghoul], [frontTarget, backTarget]);

        Assert.Equal(BattleOutcome.Draw, result.Outcome);
        Assert.Equal(0, frontTarget.Health);
        Assert.Equal(200, backTarget.Health);
        Assert.Equal(200, ghoul.Health);
        Assert.Single(result.EventLog, x => x.Source == "effect.vile_feast.heal" && x.EventType == EventType.Heal);
        Assert.Equal(
            new (int Timestamp, string Source, EventType EventType, string? TargetId, int Magnitude)[]
            {
                (0, "Siphon", EventType.AbilityUse, null, 0),
                (0, "effect.siphon.damage", EventType.Damage, "front-target", 16),
                (0, "Ambush Strike", EventType.AbilityUse, null, 0),
                (0, "effect.ambush.damage", EventType.Damage, "front-target", 24),
                (0, "effect.ambush.damage", EventType.Death, "front-target", 0),
                (0, "effect.vile_feast.heal", EventType.Heal, "ghoul", 50)
            },
            result.EventLog.Select(x => (x.Timestamp, x.Source, x.EventType, (string?)x.TargetId, x.Magnitude)).ToArray());
    }

    [Fact]
    public void Json_catalog_compiles_all_authored_specs()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var catalog = provider.GetCatalog();

        var compiledAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);

        Assert.Equal(catalog.Abilities.Count, compiledAbilities.Count);
        Assert.Equal(catalog.Statuses.Count, compiledStatuses.Count);
    }

    [Fact]
    public void Json_catalog_behavior_manifest_observations_pass()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
        var diagnostics = new AbilityCatalogBehaviorDiagnostics(
            provider,
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions(),
            essenceRepository);

        var report = diagnostics.Analyze();
        var failures = report.Scenarios
            .Where(x => !x.Passed)
            .SelectMany(x => x.Failures.Select(failure => $"{x.BehaviorId}/{x.AbilityId}: {failure}"))
            .ToList();

        Assert.True(report.ScenarioCount > 0);
        Assert.True(report.IsComplete, string.Join(Environment.NewLine, failures));
        Assert.Equal(report.ScenarioCount, report.PassedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.True(report.HasFullAbilityCoverage, string.Join(Environment.NewLine, report.MissingAbilityIds));
        Assert.Equal(report.AbilityCount, report.CoveredAbilityCount);
        Assert.Empty(report.MissingAbilityIds);
    }

    [Fact]
    public void Json_catalog_covers_authored_essence_slots()
    {
        var contentRoot = FindApiContentRoot();
        var options = CreateJsonOptions();
        var essenceRepository = new JsonEssenceDefinitionRepository(
            CreateConfig(),
            contentRoot,
            options,
            new EssenceDefinitionValidator());
        var provider = new JsonAbilityCatalogProvider(CreateConfig(), contentRoot, options);
        var analyzer = new AbilityCatalogCoverageAnalyzer(essenceRepository, provider);

        var report = analyzer.Analyze();

        Assert.True(report.IsComplete, string.Join(Environment.NewLine, report.Gaps.Select(x => $"{x.EssenceId} {x.Slot}: {x.Reason}")));
        Assert.Equal(report.RequiredSlotCount, report.CoveredSlotCount);
        Assert.Equal(120, report.RequiredSlotCount);
        Assert.Equal(report.EssenceCount, report.RuntimeLoadoutChecks.Count);
        Assert.All(report.RuntimeLoadoutChecks, check =>
        {
            Assert.True(check.IsReady, $"{check.EssenceId}: {check.Failure}");
            Assert.Null(check.Failure);
            Assert.Equal(2, check.AbilityIds.Count);
        });
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.goblin_ambusher");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.skeleton_guardian");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.fire_ant");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.ant_worker");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.forest_spirit");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.wood_nymph");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.giant_spider");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.venomous_spiderling");
        foreach (var essenceId in new[]
        {
            "essence.blackjaw_spider",
            "essence.raven",
            "essence.widow_stalker",
            "essence.scarecrow",
            "essence.lost_soul",
            "essence.apparition",
            "essence.specter",
            "essence.zombie",
            "essence.half_zombie",
            "essence.undead",
            "essence.blood_zombie",
            "essence.giant_worm",
            "essence.burrowed_horror",
            "essence.cave_leech",
            "essence.stonejaw_grub",
            "essence.deep_burrower"
        })
        {
            Assert.DoesNotContain(report.Gaps, x => x.EssenceId == essenceId);
        }
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.cave_bat");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.necroshade_wraith");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.goblin");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.goblin_warrior");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.goblin_archer");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.large_rat");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.flame_imp");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.frost_imp");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.shadow_imp");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.vampire_bat");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.blue_slime");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.brown_slime");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.green_slime");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.rainbow_slime");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.red_slime");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.transparent_slime");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.enchanted_fairy");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.glade_panther");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.illusion_fox");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.nightshade_blossom");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.pixie");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.hobgoblin");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.feral_ghoul");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.plague_ghoul");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.ravenous_ghoul");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.skeleton_archer");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.skeleton_mage");
        Assert.DoesNotContain(report.Gaps, x => x.EssenceId == "essence.legacy.skeleton_warrior");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.goblin_ambusher" && x.Slot == "Active" && x.AbilityId == "ability.essence.goblin_ambusher.cheap_shot");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.goblin_ambusher" && x.Slot == "Passive" && x.AbilityId == "ability.essence.goblin_ambusher.cowards_opening");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.skeleton_guardian" && x.Slot == "Active" && x.AbilityId == "ability.essence.skeleton_guardian.bone_bulwark");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.skeleton_guardian" && x.Slot == "Passive" && x.AbilityId == "ability.essence.skeleton_guardian.rattle_guard");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.fire_ant" && x.Slot == "Active" && x.AbilityId == "ability.essence.fire_ant.burning_mandibles");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.fire_ant" && x.Slot == "Passive" && x.AbilityId == "ability.essence.fire_ant.colony_heat");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.cave_bat" && x.Slot == "Active" && x.AbilityId == "ability.essence.cave_bat.screech");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.cave_bat" && x.Slot == "Passive" && x.AbilityId == "ability.essence.cave_bat.skittering_wings");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.necroshade_wraith" && x.Slot == "Active" && x.AbilityId == "ability.essence.necroshade_wraith.wraith_hex");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.necroshade_wraith" && x.Slot == "Passive" && x.AbilityId == "ability.essence.necroshade_wraith.grave_whisper");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.goblin" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.goblin.sneak_attack");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.goblin" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.goblin.pocket_dirt");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.goblin_warrior" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.goblin_warrior.raging_cleave");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.goblin_warrior" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.goblin_warrior.reckless_assault");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.goblin_archer" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.goblin_archer.snipers_strike");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.goblin_archer" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.goblin_archer.poisoned_arrows");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.large_rat" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.large_rat.tail_wrap");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.large_rat" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.large_rat.big");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.flame_imp" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.flame_imp.firebomb_toss");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.flame_imp" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.flame_imp.hot_aura");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.frost_imp" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.frost_imp.ice_touch");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.frost_imp" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.frost_imp.cold_aura");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.shadow_imp" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.shadow_imp.shadow_image");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.shadow_imp" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.shadow_imp.shadowy_presence");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.vampire_bat" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.vampire_bat.bloodthirsty_fangs");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.vampire_bat" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.vampire_bat.dark_vitality");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.blue_slime" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.blue_slime.sweet_water");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.blue_slime" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.blue_slime.absorptive_shell");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.brown_slime" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.brown_slime.mud_armor");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.brown_slime" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.brown_slime.earthly_fortitude");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.green_slime" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.green_slime.acid_splash");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.green_slime" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.green_slime.corrosive_ooze");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.rainbow_slime" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.rainbow_slime.unstable_colors");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.rainbow_slime" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.rainbow_slime.colorful_shield");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.red_slime" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.red_slime.ignite_core");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.red_slime" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.red_slime.fire_body");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.transparent_slime" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.transparent_slime.transparent_engulf");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.transparent_slime" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.transparent_slime.transparent_shift");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.enchanted_fairy" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.enchanted_fairy.faes_embrace");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.enchanted_fairy" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.enchanted_fairy.enchanted_charm");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.glade_panther" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.glade_panther.ambush_strike");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.glade_panther" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.glade_panther.razor_claws");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.illusion_fox" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.illusion_fox.distracting_illusion");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.illusion_fox" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.illusion_fox.foxfire_wisp");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.nightshade_blossom" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.nightshade_blossom.necrotic_spores");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.nightshade_blossom" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.nightshade_blossom.twilight_bloom");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.pixie" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.pixie.pixie_burst");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.pixie" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.pixie.resonant_chime");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.hobgoblin" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.hobgoblin.frenzy");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.hobgoblin" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.hobgoblin.savage_onslaught");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.feral_ghoul" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.feral_ghoul.feral_pounce");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.feral_ghoul" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.feral_ghoul.shredding_claws");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.plague_ghoul" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.plague_ghoul.plague_swipe");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.plague_ghoul" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.plague_ghoul.pestilent_touch");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.ravenous_ghoul" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.ravenous_ghoul.draining_claws");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.ravenous_ghoul" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.ravenous_ghoul.vile_feast");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.skeleton_archer" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.skeleton_archer.bone_arrow");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.skeleton_archer" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.skeleton_archer.piercing_arrows");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.skeleton_mage" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.skeleton_mage.siphon");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.skeleton_mage" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.skeleton_mage.protective_bone_barrier");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.skeleton_warrior" && x.Slot == "Active" && x.AbilityId == "ability.essence.legacy.skeleton_warrior.bone_shield");
        Assert.Contains(report.Slots, x => x.EssenceId == "essence.legacy.skeleton_warrior" && x.Slot == "Passive" && x.AbilityId == "ability.essence.legacy.skeleton_warrior.spiked_defense");
    }

    [Fact]
    public async Task Combat_engine_executor_runs_real_encounter_runtime()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var friendlyCharacter = CreateSourceCharacter("Executor Friendly");
        var hostileCharacter = CreateSourceCharacter("Executor Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.training");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Equal(plan.StartsAt, result.StartedAt);
        Assert.True(result.Duration > 0);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x => x.Source == "effect.burn.dot" && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x => x.Source == "effect.reflect.damage" && x.EventType == EventType.Damage);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_evolved_conditional_multiplier_modifiers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.evolved_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Evolved Strike",
            OwningEssenceId = "essence.test.evolved",
            CooldownTicks = 999,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.status.bleed", "effect.damage.main"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.status.bleed",
                    Operation = AbilityEffectOperation.ApplyStatus,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 1,
                    StatusId = "status.bleed"
                },
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10
                }
            ]
        };
        var status = new StatusSpec
        {
            Id = "status.bleed",
            Name = "Bleed",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            DurationTicks = 100
        };
        var essence = new EssenceDefinition
        {
            Id = "essence.test.evolved",
            ActiveAbilityId = ability.Id,
            Evolution = new EssenceEvolutionDefinition
            {
                ActiveAbilityModifiers =
                [
                    new()
                    {
                        Target = "effect.damage.main",
                        Operation = "AddMultiplier",
                        Value = 0.5,
                        Condition = "TargetHasStatus.Bleed"
                    }
                ]
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [status],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = essence.Id
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var repository = new FakeLegacyDefinitionRepository([ability], [essence]);
        var friendlyCharacter = CreateSourceCharacter("Evolved Friendly");
        var hostileCharacter = CreateSourceCharacter("Evolved Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, essence.Id);
        friendlyCombatant.EquippedEssences.Single().IsEvolved = true;
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider, repository);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 10);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main.evolved_bonus" && x.EventType == EventType.Damage && x.Magnitude == 5);
        Assert.Single(result.EventLog, x => x.Source == "Evolved Strike" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_evolved_add_effect_modifiers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.add_effect_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Add Effect Strike",
            OwningEssenceId = "essence.test.add_effect",
            CooldownTicks = 999,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.damage.main"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10
                }
            ]
        };
        var essence = new EssenceDefinition
        {
            Id = "essence.test.add_effect",
            ActiveAbilityId = ability.Id,
            Evolution = new EssenceEvolutionDefinition
            {
                ActiveAbilityModifiers =
                [
                    new()
                    {
                        Target = "effect.damage.main",
                        Operation = "AddEffect",
                        Value = 1,
                        Effect = new AbilityEffectSpec
                        {
                            Id = "effect.damage.evolved",
                            Operation = AbilityEffectOperation.Damage,
                            Target = AbilityTargetSelector.CurrentTarget,
                            BaseValue = 4
                        }
                    }
                ]
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = essence.Id
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var repository = new FakeLegacyDefinitionRepository([ability], [essence]);
        var friendlyCharacter = CreateSourceCharacter("Add Effect Friendly");
        var hostileCharacter = CreateSourceCharacter("Add Effect Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, essence.Id);
        friendlyCombatant.EquippedEssences.Single().IsEvolved = true;
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider, repository);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 10);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.evolved" && x.EventType == EventType.Damage && x.Magnitude == 4);
        Assert.Single(result.EventLog, x => x.Source == "Add Effect Strike" && x.EventType == EventType.AbilityUse);
    }

    [Fact]
    public async Task Combat_engine_executor_applies_temporary_ability_modifiers()
    {
        var ability = new AbilitySpec
        {
            Id = "ability.test.temporary_modifier_strike",
            Kind = AbilitySpecKind.Active,
            Name = "Temporary Modifier Strike",
            OwningEssenceId = "essence.test.temporary_modifier",
            CooldownTicks = 999,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnAbilityUsed,
                    EffectIds = ["effect.damage.main"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.damage.main",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10
                }
            ]
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [ability],
            [],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ability.Id] = "essence.test.temporary_modifier"
            });
        var provider = new FakeAbilityCatalogProvider(catalog);
        var friendlyCharacter = CreateSourceCharacter("Temporary Modifier Friendly");
        var hostileCharacter = CreateSourceCharacter("Temporary Modifier Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.test.temporary_modifier");
        friendlyCombatant.TemporaryAbilityModifiers.Add(new EssenceAbilityModifierDefinition
        {
            Target = "effect.damage.main",
            Operation = "AddMultiplier",
            Value = 0.5
        });
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage && x.Magnitude == 15);
        Assert.Single(result.EventLog, x => x.Source == "Temporary Modifier Strike" && x.EventType == EventType.AbilityUse);
    }

    [Theory]
    [InlineData(CombatMode.Idle)]
    [InlineData(CombatMode.Dungeon)]
    [InlineData(CombatMode.Pvp)]
    [InlineData(CombatMode.Raid)]
    public async Task Combat_engine_executor_runs_real_loadout_golden_contracts(CombatMode mode)
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            mode,
            ["essence.legacy.green_slime"],
            ["essence.legacy.skeleton_warrior"],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Equal(runtime.Plan.StartsAt, result.StartedAt);
        Assert.True(result.Duration > 0);
        Assert.NotEmpty(result.EventLog);
        Assert.NotEmpty(result.EntityStats);
        Assert.Contains(result.EventLog, x => x.Source == "Acid Splash" && x.EventType == EventType.AbilityUse && x.ActorId == "friendly-slot");
        Assert.Contains(result.EventLog, x => x.Source == "status.green_slime.poison" && x.EventType == EventType.StatusEffect && x.TargetId == "hostile-slot");
        Assert.Contains(result.EventLog, x => x.Source == "effect.green_slime_poison.dot" && x.EventType == EventType.Damage && x.Magnitude == 4);
        Assert.Contains(result.EventLog, x => x.Source == "effect.spiked_defense.reflect" && x.EventType == EventType.Damage && x.TargetId == "friendly-slot" && x.Magnitude == 6);
    }

    [Fact]
    public async Task Combat_engine_executor_illusion_fox_passive_retaliates_when_holder_is_attacked()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var allyCharacter = CreateSourceCharacter("Fox Ally");
        var foxCharacter = CreateSourceCharacter("Illusion Fox Holder");
        var hostileCharacter = CreateSourceCharacter("Hostile Attacker");
        var allyCombatant = CreateCombatEntity("ally-slot", allyCharacter);
        var foxCombatant = CreateCombatEntity("fox-slot", foxCharacter, "essence.legacy.illusion_fox");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("fox-slot", foxCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("ally-slot", allyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            new IdleEncounterSourceContext(allyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)));
        var runtime = new CombatEncounterRuntime(
            plan,
            [
                new CombatRuntimeParticipant(plan.FriendlyParticipants[0], foxCharacter, foxCombatant),
                new CombatRuntimeParticipant(plan.FriendlyParticipants[1], allyCharacter, allyCombatant)
            ],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var foxfire = result.EventLog.First(x =>
            x.Source == "effect.foxfire.damage"
            && x.ActorId == "fox-slot"
            && x.TargetId == "hostile-slot"
            && x.EventType == EventType.Damage);

        Assert.Contains(result.EventLog, x =>
            x.Source == "status.foxfire_stack"
            && x.ActorId == "fox-slot"
            && x.TargetId == "fox-slot"
            && x.EventType == EventType.StatusEffect);
        Assert.Equal(8, foxfire.Magnitude);
        Assert.Contains(result.EventLog, x =>
            x.Source == "Basic Attack"
            && x.ActorId == "hostile-slot"
            && x.EventType == EventType.AbilityUse
            && x.Timestamp == foxfire.Timestamp);
        Assert.Contains(result.EventLog, x =>
            x.Source == "Basic Attack"
            && x.ActorId == "hostile-slot"
            && x.TargetId == "fox-slot"
            && x.EventType == EventType.Damage
            && x.Timestamp == foxfire.Timestamp);
    }

    [Fact]
    public async Task Combat_engine_executor_summons_temporary_combatant_that_can_act_and_expire()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.legacy.shadow_imp"],
            [],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var summonLog = result.EventLog.First(x =>
            x.Source == "effect.shadow_image.summon"
            && x.ActorId == "friendly-slot"
            && x.EventType == EventType.Summon);
        var summonId = summonLog.TargetId;

        Assert.NotNull(summonLog.CombatEntity);
        Assert.Equal("Shadow Image", summonLog.CombatEntity!.Name);
        Assert.Equal("shadow_image", summonLog.CombatEntity.ImagePath);
        Assert.True(provider.GetCatalog().SummonsById.ContainsKey("shadowImage"));
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.Source == "Shadow Strike"
            && x.EventType == EventType.AbilityUse);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.Source == "effect.shadow_image.shadow_strike.damage"
            && x.TargetId == "hostile-slot"
            && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.Source == "Basic Attack"
            && x.EventType == EventType.AbilityUse);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == summonId
            && x.TargetId == "hostile-slot"
            && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == "friendly-slot"
            && x.TargetId == summonId
            && x.EventType == EventType.SummonExpired);
    }

    [Fact]
    public void Engine_supports_summoned_and_non_summoned_ally_target_selectors()
    {
        var abilities = AbilityCompiler.CompileAbilities(
            [
                new AbilitySpec
                {
                    Id = "ability.buff.summons",
                    Kind = AbilitySpecKind.Active,
                    Name = "Buff Summons",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.buff.summons",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.SummonedAllies,
                            BaseValue = 11
                        }
                    ]
                },
                new AbilitySpec
                {
                    Id = "ability.buff.non.summons",
                    Kind = AbilitySpecKind.Active,
                    Name = "Buff Non-Summons",
                    Effects =
                    [
                        new()
                        {
                            Id = "effect.buff.non.summons",
                            Operation = AbilityEffectOperation.GrantBarrier,
                            Target = AbilityTargetSelector.NonSummonedAllies,
                            BaseValue = 7
                        }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, abilities.Values);
        var ally = CreateCombatant("ally", CombatTeam.Friendly, []);
        var summon = new RuntimeCombatant(
            "summon",
            "Summon",
            CombatTeam.Friendly,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 50,
                [AttributeType.Power] = 0
            },
            [],
            ["Summoned"],
            isSummoned: true,
            summonDurationTicks: 100,
            summonOwner: friendly);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(new Dictionary<string, CompiledStatus>(), new FastCombatEngineOptions(MaxTicks: 1, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly, ally, summon], [hostile]);

        Assert.Equal(11, summon.Barrier);
        Assert.Equal(7, friendly.Barrier);
        Assert.Equal(7, ally.Barrier);
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.buff.summons" && x.TargetId == "friendly");
        Assert.DoesNotContain(result.EventLog, x => x.Source == "effect.buff.non.summons" && x.TargetId == "summon");
    }

    [Fact]
    public void Engine_enforces_summon_template_active_cap()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.cap",
            Kind = AbilitySpecKind.Active,
            Name = "Summon Cap",
            Effects =
            [
                new()
                {
                    Id = "effect.summon.cap",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "cappedSummon",
                    DurationTicks = 100
                }
            ]
        };
        var compiledAbilities = AbilityCompiler.CompileAbilities([summonAbility]);
        var compiledSummons = AbilityCompiler.CompileSummons(
            [
                new SummonSpec
                {
                    Id = "cappedSummon",
                    Name = "Capped Summon",
                    MaxActive = 1,
                    Attributes =
                    [
                        new() { Attribute = AttributeType.MaxHealth, BaseValue = 20, MinimumValue = 1 },
                        new() { Attribute = AttributeType.Power, BaseValue = 0 }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(MaxTicks: 3, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);

        Assert.Single(result.EventLog, x => x.Source == "effect.summon.cap" && x.EventType == EventType.Summon);
    }

    [Fact]
    public void Engine_expires_owned_summons_when_summoner_dies()
    {
        var summonAbility = new AbilitySpec
        {
            Id = "ability.summon.owner.cleanup",
            Kind = AbilitySpecKind.Active,
            Name = "Summon Cleanup",
            Effects =
            [
                new()
                {
                    Id = "effect.summon.cleanup",
                    Operation = AbilityEffectOperation.Summon,
                    Target = AbilityTargetSelector.Self,
                    SummonId = "cleanupSummon",
                    DurationTicks = 100
                }
            ]
        };
        var killAbility = new AbilitySpec
        {
            Id = "ability.kill.owner",
            Kind = AbilitySpecKind.Active,
            Name = "Kill Owner",
            Effects =
            [
                new()
                {
                    Id = "effect.kill.owner",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 500
                }
            ]
        };
        var compiledAbilities = AbilityCompiler.CompileAbilities([summonAbility, killAbility]);
        var compiledSummons = AbilityCompiler.CompileSummons(
            [
                new SummonSpec
                {
                    Id = "cleanupSummon",
                    Name = "Cleanup Summon",
                    MaxActive = 1,
                    Attributes =
                    [
                        new() { Attribute = AttributeType.MaxHealth, BaseValue = 20, MinimumValue = 1 },
                        new() { Attribute = AttributeType.Power, BaseValue = 0 }
                    ]
                }
            ]);
        var friendly = CreateCombatant("friendly", CombatTeam.Friendly, [compiledAbilities["ability.summon.owner.cleanup"]], maxHealth: 50);
        var hostile = CreateCombatant("hostile", CombatTeam.Hostile, [compiledAbilities["ability.kill.owner"]]);
        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(MaxTicks: 3, BasicAttackIntervalTicks: 1000));

        var result = engine.Run([friendly], [hostile]);
        var summonLog = Assert.Single(result.EventLog, x => x.Source == "effect.summon.cleanup" && x.EventType == EventType.Summon);
        var summonId = summonLog.TargetId;

        Assert.Contains(result.EventLog, x => x.Source == "effect.kill.owner" && x.TargetId == "friendly" && x.EventType == EventType.Death);
        Assert.Contains(result.EventLog, x =>
            x.ActorId == "friendly"
            && x.TargetId == summonId
            && x.EventType == EventType.SummonExpired
            && x.Details.Contains("owner death", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Combat_engine_executor_attributes_applied_status_damage_to_parent_ability()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.legacy.goblin"],
            [],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly-slot");
        var sneakAttack = Assert.Single(friendlyStats.Abilities, x => x.Name == "Sneak Attack");

        Assert.True(sneakAttack.Uses > 0);
        Assert.True(sneakAttack.TotalDamage > 0);
        Assert.DoesNotContain(friendlyStats.Abilities, x => x.Name == "Sneak Attack Bleed");
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.initial.damage"
            && x.StatsSource == "Sneak Attack"
            && x.EventType == EventType.Damage);
        Assert.Contains(result.EventLog, x =>
            x.Source == "effect.bleed.dot"
            && x.StatsSource == "Sneak Attack"
            && x.EventType == EventType.Damage);
    }

    [Fact]
    public async Task Combat_engine_executor_counts_multi_effect_passive_trigger_as_one_proc()
    {
        var runtime = CreateRealEssenceEncounterRuntime(
            CombatMode.Idle,
            ["essence.legacy.goblin_warrior"],
            [],
            out _,
            out _);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var friendlyStats = result.EntityStats.Single(x => x.EntityId == "friendly-slot");
        var recklessAssault = Assert.Single(friendlyStats.Abilities, x => x.Name == "Reckless Assault");

        Assert.Equal(1, recklessAssault.Uses);
        Assert.Equal(2, result.EventLog.Count(x =>
            x.ActorId == "friendly-slot"
            && x.StatsSource == "Reckless Assault"
            && x.EventType is EventType.Buff or EventType.Debuff));
    }

    [Fact]
    public async Task Combat_engine_executor_syncs_final_runtime_state_to_combat_entities()
    {
        var runtime = CreateTrainingEncounterRuntime(out _, out _, CombatMode.Pvp);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);
        var lastHostileSnapshot = result.EventLog
            .Where(x => x.TargetId == "hostile-slot" && x.CombatEntity is not null)
            .Select(x => x.CombatEntity!)
            .Last();

        Assert.Equal(lastHostileSnapshot.Health, runtime.HostileParticipants.Single().Combatant.GetCurrentHealthValue());
        Assert.Equal(lastHostileSnapshot.Barrier, runtime.HostileParticipants.Single().Combatant.GetCurrentBarrierValue());
    }

    [Theory]
    [InlineData(CombatMode.Idle)]
    [InlineData(CombatMode.Dungeon)]
    [InlineData(CombatMode.Pvp)]
    [InlineData(CombatMode.Raid)]
    public async Task Combat_engine_executor_runs_outer_runtime_shapes(CombatMode mode)
    {
        var runtime = CreateTrainingEncounterRuntime(out _, out _, mode);
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var executor = new CombatEngineExecutor(provider);

        var result = await executor.ExecuteAsync(runtime, CancellationToken.None);

        Assert.Equal(mode, runtime.Plan.Mode);
        Assert.Equal(mode, runtime.Plan.SourceContext.Mode);
        Assert.Equal(runtime.Plan.StartsAt, result.StartedAt);
        Assert.True(result.Duration > 0);
        Assert.NotEmpty(result.EventLog);
        Assert.NotEmpty(result.EntityStats);
        Assert.Contains(result.EventLog, x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage);
    }

    [Fact]
    public void Ability_catalog_diagnostics_runs_training_encounter()
    {
        var provider = new JsonAbilityCatalogProvider(
            CreateConfig(),
            FindApiContentRoot(),
            CreateJsonOptions());
        var diagnostics = new AbilityCatalogDiagnostics(provider);

        var report = diagnostics.RunTrainingEncounter();

        Assert.True(report.AbilityCount >= 3);
        Assert.True(report.StatusCount >= 2);
        Assert.True(report.SummonCount >= 1);
        Assert.True(report.IndexedSummonTags >= 1);
        Assert.True(report.TimedSummonCount >= 1);
        Assert.True(report.SummonAbilityReferenceCount >= 1);
        Assert.Contains(report.Summons, x =>
            x.Id == "shadowImage"
            && x.HasTimedDuration
            && x.ExpiresOnOwnerDeath
            && x.AbilityIds.Contains("ability.summon.shadow_image.shadow_strike"));
        Assert.True(report.DirectDamageObserved);
        Assert.True(report.BarrierObserved);
        Assert.True(report.DamageOverTimeObserved);
        Assert.True(report.ReflectObserved);
        Assert.Empty(report.Failures);
    }

    [Fact]
    public void Ability_catalog_coverage_reports_missing_and_ambiguous_essence_slots()
    {
        var essences = new List<EssenceDefinition>
        {
            new()
            {
                Id = "essence.covered",
                ActiveAbilityId = "ability.covered.active",
                PassiveAbilityId = "ability.covered.passive"
            },
            new()
            {
                Id = "essence.missing",
                ActiveAbilityId = "legacy.missing.active",
                PassiveAbilityId = "legacy.missing.passive"
            },
            new()
            {
                Id = "essence.ambiguous",
                ActiveAbilityId = "legacy.ambiguous.active",
                PassiveAbilityId = "legacy.ambiguous.passive"
            }
        };
        var catalog = AbilityCatalogValidator.CreateCatalog(
            [
                CreateOwnedAbility("ability.covered.active", "essence.covered", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.covered.passive", "essence.covered", AbilitySpecKind.Passive),
                CreateOwnedAbility("ability.missing.active", "essence.missing", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.ambiguous.active.one", "essence.ambiguous", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.ambiguous.active.two", "essence.ambiguous", AbilitySpecKind.Active),
                CreateOwnedAbility("ability.unowned", "essence.not.real", AbilitySpecKind.Active)
            ],
            []);
        var analyzer = new AbilityCatalogCoverageAnalyzer(
            new FakeLegacyDefinitionRepository([], essences),
            new FakeAbilityCatalogProvider(catalog));

        var report = analyzer.Analyze();

        Assert.False(report.IsComplete);
        Assert.Equal(3, report.EssenceCount);
        Assert.Equal(6, report.RequiredSlotCount);
        Assert.Equal(3, report.CoveredSlotCount);
        Assert.Equal(2, report.CurrentReferenceCoveredSlotCount);
        Assert.Equal(3, report.RuntimeLoadoutChecks.Count);
        Assert.Contains(report.RuntimeLoadoutChecks, x => x.EssenceId == "essence.missing" && !x.IsReady);
        Assert.Contains(report.Gaps, x => x.EssenceId == "essence.missing" && x.Slot == "Passive" && x.Reason.Contains("No Passive", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Gaps, x => x.EssenceId == "essence.ambiguous" && x.Slot == "Active" && x.Reason.Contains("Multiple Active", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.UnownedAbilityIds, x => x == "ability.unowned");
    }

    private static CombatResult RunBattle(
        IReadOnlyList<AbilitySpec> friendlyAbilities,
        IReadOnlyList<StatusSpec> statuses,
        int maxTicks,
        out RuntimeCombatant friendly,
        out RuntimeCombatant hostile,
        int seed = 1337)
    {
        var compiledAbilities = AbilityCompiler.CompileAbilities(friendlyAbilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(statuses);
        friendly = CreateCombatant("friendly", CombatTeam.Friendly, compiledAbilities.Values);
        hostile = CreateCombatant("hostile", CombatTeam.Hostile, []);

        var engine = new FastCombatEngine(compiledStatuses, new FastCombatEngineOptions(maxTicks, RandomSeed: seed));
        return engine.Run([friendly], [hostile]);
    }

    private static CombatEncounterRuntime CreateTrainingEncounterRuntime(
        out Character friendlyCharacter,
        out Character hostileCharacter,
        CombatMode mode = CombatMode.Idle)
    {
        friendlyCharacter = CreateSourceCharacter("Executor Friendly");
        hostileCharacter = CreateSourceCharacter("Executor Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, "essence.training");
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            mode,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            CreateSourceContext(mode, friendlyCharacter.Id, hostileCharacter.Id));

        return new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
    }

    private static CombatEncounterRuntime CreateRealEssenceEncounterRuntime(
        CombatMode mode,
        IReadOnlyList<string> friendlyEssenceIds,
        IReadOnlyList<string> hostileEssenceIds,
        out Character friendlyCharacter,
        out Character hostileCharacter)
    {
        friendlyCharacter = CreateSourceCharacter("Real Friendly");
        hostileCharacter = CreateSourceCharacter("Real Hostile");
        var friendlyCombatant = CreateCombatEntity("friendly-slot", friendlyCharacter, [.. friendlyEssenceIds]);
        var hostileCombatant = CreateCombatEntity("hostile-slot", hostileCharacter, [.. hostileEssenceIds]);
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            mode,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly-slot", friendlyCharacter.Id, CombatSide.Friendly),
                new CombatParticipantSlot("hostile-slot", hostileCharacter.Id, CombatSide.Hostile)
            ],
            CreateSourceContext(mode, friendlyCharacter.Id, hostileCharacter.Id));

        return new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), hostileCharacter, hostileCombatant)]);
    }

    private static CombatEncounterSourceContext CreateSourceContext(
        CombatMode mode,
        Guid friendlyCharacterId,
        Guid hostileCharacterId) =>
        mode switch
        {
            CombatMode.Dungeon => new DungeonEncounterSourceContext(Guid.NewGuid()),
            CombatMode.Pvp => new PvpEncounterSourceContext(Guid.NewGuid(), friendlyCharacterId, hostileCharacterId),
            CombatMode.Raid => new RaidEncounterSourceContext(Guid.NewGuid(), PhaseIndex: 1, StageKey: "test-stage"),
            _ => new IdleEncounterSourceContext(friendlyCharacterId, new Area(), TimeSpan.FromSeconds(1))
        };

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities,
        int maxHealth = 200,
        int dodgeChance = 0) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = maxHealth,
                [AttributeType.Power] = 50,
                [AttributeType.CritDamage] = 100,
                [AttributeType.DodgeChance] = dodgeChance
            },
            abilities,
            ["Role.Test"]);

    private static IReadOnlyDictionary<string, CompiledAbility> CompileCatalogAbilities(
        AbilityCatalog catalog,
        params string[] abilityIds) =>
        AbilityCompiler.CompileAbilities(abilityIds.Select(id => catalog.AbilitiesById[id]));

    private static AbilitySpec CreatePassiveBarrier(
        string id,
        string effectId,
        AbilityTriggerEvent triggerEvent,
        int value) =>
        new()
        {
            Id = id,
            Kind = AbilitySpecKind.Passive,
            Name = id,
            Triggers = [new() { Event = triggerEvent }],
            Effects =
            [
                new()
                {
                    Id = effectId,
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = value
                }
            ]
        };

    private static Character CreateSourceCharacter(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = 10
        };

    private static CombatEntity CreateCombatEntity(
        string runtimeId,
        Character source,
        params string[] equippedEssenceIds)
    {
        FillCombatAttributes(source.BaseCombatAttributes);
        FillCombatAttributes(source.CombatAttributes);

        var combatant = new CombatEntity(source)
        {
            Id = runtimeId,
            Name = source.Name,
            Level = source.Level
        };

        FillCombatAttributes(combatant.BaseCombatAttributes);
        FillCombatAttributes(combatant.CombatAttributes);
        combatant.SyncCurrentHealthToMax();

        foreach (var equippedEssenceId in equippedEssenceIds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            combatant.EquippedEssences.Add(new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = source.Id,
                EssenceDefinitionId = equippedEssenceId,
                Level = 1
            });
        }

        return combatant;
    }

    private static void FillCombatAttributes(IDictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = 200;
        attributes[AttributeType.Power] = 50;
        attributes[AttributeType.CritDamage] = 100;
    }

    private static AbilitySpec CreateDamageAbility(string id, string tag) =>
        new()
        {
            Id = id,
            Kind = AbilitySpecKind.Active,
            Name = id,
            Tags = [tag],
            Triggers = [new() { Event = AbilityTriggerEvent.OnAbilityUsed }],
            Effects =
            [
                new()
                {
                    Id = "effect.damage",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.CurrentTarget,
                    BaseValue = 10,
                    ScalingAttribute = AttributeType.Power,
                    ScalingCoefficient = 0.2f,
                    AttackType = AttackType.Melee,
                    DamageType = DamageType.Physical
                }
            ]
        };

    private static StatusSpec CreateBurnStatus() =>
        new()
        {
            Id = "status.burn",
            Name = "Burn",
            StackingPolicy = AbilityStatusStackingPolicy.Stack,
            MaxStacks = 5,
            DurationTicks = 20,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["effect.burn.dot"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.burn.dot",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.EventTarget,
                    BaseValue = 3,
                    DurationTicks = 9,
                    IntervalTicks = 3,
                    AttackType = AttackType.DamageOverTime,
                    DamageType = DamageType.Burn
                }
            ]
        };

    private static StatusSpec CreateThornsStatus() =>
        new()
        {
            Id = "status.thorns",
            Name = "Thorns",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 100,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnDamaged,
                    EffectIds = ["effect.thorns.reflect"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.thorns.reflect",
                    Operation = AbilityEffectOperation.Damage,
                    Target = AbilityTargetSelector.EventTarget,
                    BaseValue = 6,
                    AttackType = AttackType.None,
                    DamageType = DamageType.Physical
                }
            ]
        };

    private static StatusSpec CreateStunStatus() =>
        new()
        {
            Id = "status.stunned",
            Name = "Stunned",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 30,
            Tags = ["Control.Stun"]
        };

    private static StatusSpec CreateEmptyStatus(
        string id,
        AbilityStatusStackingPolicy stackingPolicy,
        int maxStacks,
        int durationTicks) =>
        new()
        {
            Id = id,
            Name = id,
            StackingPolicy = stackingPolicy,
            MaxStacks = maxStacks,
            DurationTicks = durationTicks
        };

    private static StatusSpec CreateTimedPowerBuffStatus() =>
        new()
        {
            Id = "status.power.buff",
            Name = "Power Buff",
            StackingPolicy = AbilityStatusStackingPolicy.Refresh,
            MaxStacks = 1,
            DurationTicks = 10,
            Triggers =
            [
                new()
                {
                    Event = AbilityTriggerEvent.OnStatusApplied,
                    EffectIds = ["effect.status.power.buff"]
                }
            ],
            Effects =
            [
                new()
                {
                    Id = "effect.status.power.buff",
                    Operation = AbilityEffectOperation.ModifyAttribute,
                    Target = AbilityTargetSelector.EventTarget,
                    Attribute = AttributeType.Power,
                    BaseValue = 20,
                    DurationTicks = 2
                }
            ]
        };

    private static AbilityEffectSpec CreateApplyStatusEffect(
        string id,
        string statusId,
        AbilityTargetSelector target = AbilityTargetSelector.CurrentTarget) =>
        new()
        {
            Id = id,
            Operation = AbilityEffectOperation.ApplyStatus,
            Target = target,
            StatusId = statusId,
            BaseValue = 1
        };

    private static AbilitySpec CreateOwnedAbility(string id, string owningEssenceId, AbilitySpecKind kind) =>
        new()
        {
            Id = id,
            Name = id,
            OwningEssenceId = owningEssenceId,
            Kind = kind,
            Effects =
            [
                new()
                {
                    Id = "effect.noop",
                    Operation = AbilityEffectOperation.GrantBarrier,
                    Target = AbilityTargetSelector.Self,
                    BaseValue = 1
                }
            ]
        };

    private static IConfiguration CreateConfig(bool? useV2Engine = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data",
                ["Combat:UseV2Engine"] = useV2Engine?.ToString()
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
            var dataPath = Path.Combine(directory.FullName, "src", "API", "API.LL", "Data");
            var abilityCandidate = Path.Combine(dataPath, "abilities.json");
            var statusCandidate = Path.Combine(dataPath, "statuses.json");
            var summonCandidate = Path.Combine(dataPath, "summons.json");
            if (File.Exists(abilityCandidate) && File.Exists(statusCandidate) && File.Exists(summonCandidate))
                return Path.Combine(directory.FullName, "src", "API", "API.LL");

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/abilities.json, statuses.json, and summons.json from test output directory.");
    }

    private sealed class FakeLegacyDefinitionRepository(
        IReadOnlyList<AbilitySpec> abilities,
        IReadOnlyList<EssenceDefinition>? essences = null) : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => essences ?? [];
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => abilities;
        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            essences?.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public EssenceDefinition? GetByMonsterId(string monsterId) => null;
        public AbilitySpec? GetAbilityById(string abilityId) =>
            abilities.FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeAbilityCatalogProvider(AbilityCatalog catalog) : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => catalog;
    }

}
