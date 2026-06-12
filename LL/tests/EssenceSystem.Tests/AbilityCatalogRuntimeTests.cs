using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Interfaces.Combat;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.Statuses;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.CombatEngine;
using Services.LL.Combat.Stats;
using Services.LL.Essences;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class AbilityCatalogRuntimeTests
{
    [Fact]
    public void Authored_catalog_resolves_every_essence_ability_to_runtime_combat_abilities()
    {
        var definitions = CreateDefinitionRepository();
        var service = CreateEssenceService(definitions);
        var equippedEssences = definitions.GetAll()
            .Select((definition, index) => CreatePlayerEssence(definition, index))
            .ToList();

        var loadout = service.Resolve(Guid.NewGuid(), equippedEssences);

        Assert.Equal(definitions.GetAll().Count, loadout.ActiveAbilities.Count);
        Assert.Equal(definitions.GetAll().Count, loadout.PassiveAbilities.Count);

        foreach (var definition in definitions.GetAll())
        {
            Assert.Contains(loadout.ActiveAbilities, x => x.AbilityDefinitionId == definition.ActiveAbilityId);
            Assert.Contains(loadout.PassiveAbilities, x => x.AbilityDefinitionId == definition.PassiveAbilityId);
        }
    }

    [Fact]
    public void Authored_catalog_has_unique_effect_ids_inside_each_ability()
    {
        var definitions = CreateDefinitionRepository();
        var failures = definitions.GetAll()
            .SelectMany(definition => new[] { definition.ActiveAbility, definition.PassiveAbility })
            .Where(ability => ability.Effects
                .Where(effect => !string.IsNullOrWhiteSpace(effect.Id))
                .GroupBy(effect => effect.Id, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
            .Select(ability => ability.Id)
            .ToList();

        Assert.True(failures.Count == 0, "Abilities with duplicate effect ids: " + string.Join(", ", failures));
    }

    [Fact]
    public void Authored_catalog_references_every_ability_from_exactly_one_essence_slot()
    {
        var definitions = CreateDefinitionRepository();
        var abilities = ReadAuthoredAbilityDefinitions()
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var referencedAbilityIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var definition in definitions.GetAll())
        {
            AssertAbilityReference(definition.Id, "active", "Active", definition.ActiveAbilityId, abilities, referencedAbilityIds, failures);
            AssertAbilityReference(definition.Id, "passive", "Passive", definition.PassiveAbilityId, abilities, referencedAbilityIds, failures);
        }

        foreach (var duplicate in referencedAbilityIds.Where(x => x.Value.Count > 1))
            failures.Add($"Ability '{duplicate.Key}' is referenced by multiple essence slots: {string.Join(", ", duplicate.Value)}");

        var unreferenced = abilities.Keys
            .Where(x => !referencedAbilityIds.ContainsKey(x))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unreferenced.Count > 0)
            failures.Add("Unreferenced authored abilities: " + string.Join(", ", unreferenced));

        Assert.True(failures.Count == 0, BuildFailureMessage(failures));
    }

    [Fact]
    public void Essence_catalog_does_not_author_xp_progression_or_tier_scaling()
    {
        var path = Path.Combine(FindApiContentRoot(), "Data", "essences.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        Assert.False(document.RootElement.TryGetProperty("progressionTemplates", out _));
        foreach (var definition in document.RootElement.GetProperty("essences").EnumerateArray())
        {
            Assert.False(definition.TryGetProperty("progressionTemplateId", out _));

            foreach (var bonus in definition.GetProperty("attributeBonuses").EnumerateArray())
                AssertNoIndividualBonusScaling(bonus);

            if (definition.GetProperty("evolution").TryGetProperty("attributeModifierChanges", out var changes))
            {
                foreach (var bonus in changes.EnumerateArray())
                    AssertNoIndividualBonusScaling(bonus);
            }
        }
    }

    [Fact]
    public void Ability_catalog_uses_base_values_without_individual_progression_scaling()
    {
        var path = Path.Combine(FindApiContentRoot(), "Data", "abilities.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        foreach (var ability in document.RootElement.EnumerateArray())
        {
            foreach (var effect in ability.GetProperty("effects").EnumerateArray())
            {
                if (!effect.TryGetProperty("scaling", out var scaling)) continue;

                Assert.False(scaling.TryGetProperty("perLevel", out _));
                Assert.False(scaling.TryGetProperty("perAscensionTier", out _));
            }
        }
    }

    [Fact]
    public void Every_authored_combat_effect_can_execute_in_a_controlled_combat_context()
    {
        var definitions = CreateDefinitionRepository();
        var service = CreateEssenceService(definitions);
        var failures = new List<string>();

        foreach (var definition in definitions.GetAll())
        {
            var playerEssence = CreatePlayerEssence(definition, 0);
            var loadout = service.Resolve(Guid.NewGuid(), [playerEssence]);

            foreach (var resolvedAbility in loadout.Abilities)
            {
                foreach (var trigger in resolvedAbility.Ability.Definition.Triggers)
                {
                    foreach (var effect in trigger.Actions)
                    {
                        var source = CreateCombatEntity("Source", roleTags: resolvedAbility.Tags);
                        var target = CreateCombatEntity("Target", roleTags: ["Role.Tank", "Species.Beast"]);
                        PrepareLikelyPreconditions(source, target);
                        var context = CreateCombatContext(source, target);
                        var before = CombatEffectSnapshot.Capture(source, target, context);

                        try
                        {
                            var instance = new EffectInstance(effect.Clone(), source, target);
                            instance.ExecuteAction(context);
                            AssertObservableOutcome(definition.Id, resolvedAbility.AbilityDefinitionId, effect, instance, before, source, target, context);
                            instance.ExecuteOnExpireAction(context);
                            context.EffectManager.Tick();
                        }
                        catch (Exception ex)
                        {
                            failures.Add($"{definition.Id} -> {resolvedAbility.AbilityDefinitionId} -> {effect.SourceName}/{effect.Action.GetType().Name}: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, BuildFailureMessage(failures));
    }

    private static void AssertAbilityReference(
        string essenceId,
        string slotName,
        string expectedKind,
        string abilityId,
        IReadOnlyDictionary<string, AbilityDefinition> abilities,
        IDictionary<string, List<string>> referencedAbilityIds,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            failures.Add($"{essenceId} has no {slotName} ability id.");
            return;
        }

        if (!abilities.TryGetValue(abilityId, out var ability))
        {
            failures.Add($"{essenceId} references missing {slotName} ability '{abilityId}'.");
            return;
        }

        if (!referencedAbilityIds.TryGetValue(abilityId, out var references))
        {
            references = [];
            referencedAbilityIds.Add(abilityId, references);
        }

        references.Add($"{essenceId}.{slotName}");

        if (!ability.Kind.Equals(expectedKind, StringComparison.OrdinalIgnoreCase))
            failures.Add($"{essenceId} {slotName} ability '{abilityId}' is kind '{ability.Kind}', expected '{expectedKind}'.");
    }

    private static void AssertObservableOutcome(
        string essenceDefinitionId,
        string abilityDefinitionId,
        EffectDefinition effect,
        EffectInstance instance,
        CombatEffectSnapshot before,
        CombatEntity source,
        CombatEntity target,
        CombatContext context)
    {
        if (effect.Chance < 100 || !instance.HasTriggered)
            return;

        var action = Assert.IsType<CombatEffectAction>(effect.Action);
        var after = CombatEffectSnapshot.Capture(source, target, context);
        var changed = action.Operation switch
        {
            CombatEffectOperation.Damage => after.TargetHealth < before.TargetHealth || after.TargetBarrier < before.TargetBarrier,
            CombatEffectOperation.RestoreResource when action.Resource?.ToString() == "Barrier" => after.TargetBarrier > before.TargetBarrier,
            CombatEffectOperation.RestoreResource => after.TargetHealth > before.TargetHealth,
            CombatEffectOperation.ModifyAttribute => after.TargetTemporaryModifiers > before.TargetTemporaryModifiers,
            CombatEffectOperation.ApplyStatus => after.TargetStatuses > before.TargetStatuses,
            CombatEffectOperation.RemoveStatus => after.TargetStatuses < before.TargetStatuses || after.TargetStatusEffectStacks < before.TargetStatusEffectStacks,
            CombatEffectOperation.ModifyStatusEffect => after.TargetStatusEffectStacks != before.TargetStatusEffectStacks,
            CombatEffectOperation.Cleanse => after.TargetStatuses == 0 && after.TargetStatusEffects == 0,
            CombatEffectOperation.Summon => after.SourceTeamCount > before.SourceTeamCount,
            CombatEffectOperation.SelfDestruct => after.TargetTeamCount < before.TargetTeamCount || after.SourceTeamCount < before.SourceTeamCount,
            CombatEffectOperation.TriggerSecondaryEffect => true,
            _ => throw new NotSupportedException($"No observable outcome assertion exists for combat effect operation '{action.Operation}'.")
        };

        Assert.True(
            changed,
            $"{essenceDefinitionId} -> {abilityDefinitionId} -> {effect.SourceName}/{action.Operation} executed without producing the expected state change.");
    }

    private static JsonEssenceDefinitionRepository CreateDefinitionRepository()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();

        return new JsonEssenceDefinitionRepository(
            config,
            FindApiContentRoot(),
            CreateJsonOptions(),
            new EssenceDefinitionValidator());
    }

    private static IReadOnlyList<AbilityDefinition> ReadAuthoredAbilityDefinitions()
    {
        var path = Path.Combine(FindApiContentRoot(), "Data", "abilities.json");
        return JsonSerializer.Deserialize<List<AbilityDefinition>>(File.ReadAllText(path), CreateJsonOptions()) ?? [];
    }

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

    private static EssenceSystemService CreateEssenceService(IEssenceDefinitionRepository definitions) =>
        new(
            essences: null!,
            inventory: null!,
            itemBases: null!,
            definitions: definitions,
            progression: new EssenceProgressionService(),
            slotUnlocks: new EssenceSlotUnlockService(),
            loadoutLimits: new EssenceLoadoutLimitService(),
            random: null!);

    private static PlayerEssence CreatePlayerEssence(EssenceDefinition definition, int index) =>
        new()
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            EssenceDefinitionId = definition.Id,
            Level = 5 + index % 5,
            CurrentXp = 100,
            AscensionTier = index % 3,
            IsEvolved = index % 2 == 0
        };

    private static CombatContext CreateCombatContext(CombatEntity source, CombatEntity target)
    {
        var context = new CombatContext(new CombatEventBus(), new PermissiveStatusDefinitionService(), new CombatStatsAggregator());
        context.EntityManager.InitializeCombatEntityManager([source], [target]);
        return context;
    }

    private static CombatEntity CreateCombatEntity(string name, IEnumerable<string>? roleTags = null)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = 10,
            BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(Guid.NewGuid())
        };

        var entity = new CombatEntity(character);
        foreach (var tag in roleTags ?? [])
            entity.Tags.Add(tag);

        AttributeCalculator.CalculateBaseCombatAttributes(entity);
        entity.SetCurrentHealth(entity.GetAttributeValue(AttributeType.MaxHealth) * 0.4f);
        return entity;
    }

    private static void PrepareLikelyPreconditions(CombatEntity source, CombatEntity target)
    {
        source.Tags.Add("Role.Offensive");
        source.Tags.Add("Species.Beast");
        target.Tags.Add("Role.Tank");
        target.Tags.Add("Species.Beast");

        foreach (var statusId in CommonStatusIds)
        {
            var status = CreateStatus(statusId);
            source.ApplyStatus(new StatusInstance(status.Clone(), source, source));
            target.ApplyStatus(new StatusInstance(status.Clone(), source, target));

            if (Enum.TryParse<StatusEffectType>(statusId, ignoreCase: true, out var effectType))
            {
                source.ModifyStatusEffects(effectType, 10);
                target.ModifyStatusEffects(effectType, 10);
            }
        }
    }

    private static StatusDefinition CreateStatus(string statusId) =>
        new()
        {
            Id = statusId,
            Name = statusId,
            IsStackable = true,
            Duration = new TimedDuration(100)
        };

    private static string BuildFailureMessage(IReadOnlyCollection<string> failures)
    {
        if (failures.Count == 0) return string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine("Authored combat effects failed runtime execution:");
        foreach (var failure in failures)
            builder.AppendLine("- " + failure);
        return builder.ToString();
    }

    private static void AssertNoIndividualBonusScaling(JsonElement bonus)
    {
        Assert.False(bonus.TryGetProperty("perLevel", out _));
        Assert.False(bonus.TryGetProperty("perAscensionTier", out _));
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var dataPath = Path.Combine(directory.FullName, "src", "API", "API.LL", "Data");
            var essenceCandidate = Path.Combine(dataPath, "essences.json");
            var abilityCandidate = Path.Combine(dataPath, "abilities.json");
            if (File.Exists(essenceCandidate) && File.Exists(abilityCandidate))
                return Path.Combine(directory.FullName, "src", "API", "API.LL");

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data/essences.json and abilities.json from test output directory.");
    }

    private static readonly IReadOnlyCollection<string> CommonStatusIds =
    [
        "Bleed",
        "Burn",
        "Curse",
        "Poison",
        "Frozen",
        "Stunned",
        "sneakAttackBleed",
        "piercingArrowBleed",
        "Cold",
        "Ignite",
        "Shadow",
        "Vampiric",
        "Acid",
        "Necrotic"
    ];

    private sealed record CombatEffectSnapshot(
        float TargetHealth,
        float TargetBarrier,
        int TargetTemporaryModifiers,
        int TargetStatuses,
        int TargetStatusEffects,
        int TargetStatusEffectStacks,
        int SourceTeamCount,
        int TargetTeamCount)
    {
        public static CombatEffectSnapshot Capture(CombatEntity source, CombatEntity target, CombatContext context)
        {
            var ownTeam = context.EntityManager.GetOwnTeam(source);
            var targetTeam = context.EntityManager.GetOwnTeam(target);
            return new CombatEffectSnapshot(
                target.CurrentHealth,
                target.CurrentBarrier,
                target.TemporaryModifiers.Count,
                target.Statuses.Count,
                target.StatusEffects.Count,
                target.StatusEffects.Values.Sum(),
                ownTeam.Count,
                targetTeam.Count);
        }
    }

    private sealed class PermissiveStatusDefinitionService : IStatusDefinitionService
    {
        public bool TryGetById(string id, out StatusDefinition def)
        {
            def = CreateStatus(id);
            return true;
        }

        public IReadOnlyCollection<StatusDefinition> GetAll() =>
            CommonStatusIds.Select(CreateStatus).ToList();
    }
}
