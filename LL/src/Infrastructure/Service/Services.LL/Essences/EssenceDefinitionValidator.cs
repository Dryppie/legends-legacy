using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using System.Text.RegularExpressions;

namespace Services.LL.Essences;

public sealed class EssenceDefinitionValidator : IEssenceDefinitionValidator
{
    private static readonly Regex DescriptionPlaceholderPattern = new(
        @"\{(?<kind>[a-zA-Z]+)(?<index>\d*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<string> Validate(IReadOnlyList<EssenceDefinition> definitions)
    {
        var errors = new List<string>();
        var duplicateIds = definitions.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key);
        errors.AddRange(duplicateIds.Select(id => $"Duplicate Essence id '{id}'."));

        foreach (var creatureVariants in definitions
                     .Where(x => !string.IsNullOrWhiteSpace(x.SourceMonsterId))
                     .GroupBy(x => x.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1))
        {
            var baseNames = creatureVariants
                .Select(x => x.Name?.Trim() ?? string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (baseNames.Count > 1)
                errors.Add($"{creatureVariants.Key}: Essence variants must use the same base name.");

            foreach (var definition in creatureVariants.Where(x => string.IsNullOrWhiteSpace(x.VariantName)))
                errors.Add($"{definition.Id}: variantName is required when a creature has multiple Essences.");

            var duplicateVariantNames = creatureVariants
                .Where(x => !string.IsNullOrWhiteSpace(x.VariantName))
                .GroupBy(x => x.VariantName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);
            errors.AddRange(duplicateVariantNames.Select(name =>
                $"{creatureVariants.Key}: duplicate Essence variantName '{name}'."));
        }

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id)) errors.Add("Essence id is required.");
            if (string.IsNullOrWhiteSpace(definition.SourceMonsterId)) errors.Add($"{definition.Id}: sourceMonsterId is required.");
            if (string.IsNullOrWhiteSpace(definition.Name)) errors.Add($"{definition.Id}: name is required.");
            if (string.IsNullOrWhiteSpace(definition.ActiveAbilityId)) errors.Add($"{definition.Id}: activeAbilityId is required.");
            if (string.IsNullOrWhiteSpace(definition.PassiveAbilityId)) errors.Add($"{definition.Id}: passiveAbilityId is required.");
            if (definition.ActiveAbility is null || string.IsNullOrWhiteSpace(definition.ActiveAbility.Id)) errors.Add($"{definition.Id}: activeAbilityId '{definition.ActiveAbilityId}' could not be resolved.");
            if (definition.PassiveAbility is null || string.IsNullOrWhiteSpace(definition.PassiveAbility.Id)) errors.Add($"{definition.Id}: passiveAbilityId '{definition.PassiveAbilityId}' could not be resolved.");
            if (definition.ActiveAbility is not null && definition.ActiveAbility.Kind != AbilitySpecKind.Active)
                errors.Add($"{definition.Id}: activeAbilityId '{definition.ActiveAbilityId}' must reference an Active ability definition.");
            if (definition.PassiveAbility is not null && definition.PassiveAbility.Kind != AbilitySpecKind.Passive)
                errors.Add($"{definition.Id}: passiveAbilityId '{definition.PassiveAbilityId}' must reference a Passive ability definition.");
            if (definition.Evolution is null || string.IsNullOrWhiteSpace(definition.Evolution.Id)) errors.Add($"{definition.Id}: exactly one evolution is required.");
            if (definition.AttributeBonuses.Count > 0 || definition.Evolution?.AttributeModifierChanges.Count > 0)
                errors.Add($"{definition.Id}: Essence attribute bonuses are no longer supported; use ability effects instead.");

            foreach (var tag in definition.Tags.Concat(definition.Evolution?.AddsTags ?? []))
            {
                if (!IsKnownTag(tag)) errors.Add($"{definition.Id}: unknown tag '{tag}'.");
            }

            ValidateAbility(definition.Id, definition.ActiveAbility, errors);
            ValidateAbility(definition.Id, definition.PassiveAbility, errors);
            ValidateAbilityModifiers(definition.Id, definition.ActiveAbility, definition.Evolution?.ActiveAbilityModifiers ?? [], errors);
            ValidateAbilityModifiers(definition.Id, definition.PassiveAbility, definition.Evolution?.PassiveAbilityModifiers ?? [], errors);

            var tiers = definition.Ascension?.Tiers ?? [];
            if (tiers.Count != 4 || tiers.Select(x => x.Tier).Order().SequenceEqual([0, 1, 2, 3]) == false)
                errors.Add($"{definition.Id}: ascension tiers 0-3 are required.");

            if (definition.Evolution?.RequiredAscensionTier is < 0 or > 3)
                errors.Add($"{definition.Id}: evolution required tier must be 0-3.");
        }

        return errors;
    }

    private static void ValidateAbility(string essenceId, AbilitySpec? ability, List<string> errors)
    {
        if (ability is null) return;

        if (string.IsNullOrWhiteSpace(ability.Id)) errors.Add($"{essenceId}: ability id is required.");
        if (string.IsNullOrWhiteSpace(ability.Name)) errors.Add($"{essenceId}/{ability.Id}: ability name is required.");
        ValidateDescriptionPlaceholders(essenceId, ability, errors);

        if (ability.Kind == AbilitySpecKind.Active)
        {
            if (ability.CooldownTicks <= 0) errors.Add($"{essenceId}/{ability.Id}: active ability cooldown must be greater than zero.");
            if (ability.Effects.Count == 0) errors.Add($"{essenceId}/{ability.Id}: active ability requires at least one effect.");
        }

        if (ability.Kind == AbilitySpecKind.Passive
            && ability.Triggers.Count == 0
            && ability.Effects.Count == 0
            && !ability.Tags.Contains("NonCombat", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"{essenceId}/{ability.Id}: passive ability requires a trigger or permanent effect.");
        }

        foreach (var trigger in ability.Triggers)
            ValidateConditions(essenceId, ability.Id, trigger.Conditions, errors);

        var duplicateEffectIds = ability.Effects
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);
        errors.AddRange(duplicateEffectIds.Select(id => $"{essenceId}/{ability.Id}: duplicate effect id '{id}'."));

        foreach (var effect in ability.Effects)
        {
            if (string.IsNullOrWhiteSpace(effect.Id)) errors.Add($"{essenceId}/{ability.Id}: effect id is required.");
            if (!AllowsNegativeValue(effect.Operation)
                && effect.BaseValue < 0)
            {
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: scaling values cannot be negative.");
            }
            if (effect.Operation == AbilityEffectOperation.ModifyAttribute && effect.Attribute is null)
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: ModifyAttribute requires attribute.");
            if (effect.Attribute is { } attribute && !AttributeCatalog.IsContentFacing(attribute))
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: attribute '{effect.Attribute}' is runtime-only and cannot be authored.");
            if (effect.Operation == AbilityEffectOperation.ApplyStatus && string.IsNullOrWhiteSpace(effect.StatusId))
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: ApplyStatus requires status.");
            if (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is null)
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: ApplyCondition requires condition.");
            if (effect.StaggerPower < 0)
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: staggerPower cannot be negative.");
            if (effect.StaggerPower > 0
                && (effect.Operation != AbilityEffectOperation.ApplyCondition
                    || effect.Condition is not (StandardConditionType.Stun or StandardConditionType.Freeze)))
            {
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: staggerPower requires an ApplyCondition Stun or Freeze effect.");
            }
            ValidateStandardConditionEffect(essenceId, $"{ability.Id}/{effect.Id}", effect, errors);

            ValidateConditions(essenceId, $"{ability.Id}/{effect.Id}", effect.Conditions, errors);
        }
    }

    private static void ValidateDescriptionPlaceholders(
        string essenceId,
        AbilitySpec ability,
        ICollection<string> errors)
    {
        foreach (Match match in DescriptionPlaceholderPattern.Matches(ability.Description ?? string.Empty))
        {
            var kind = match.Groups["kind"].Value;
            var rawIndex = match.Groups["index"].Value;
            var index = string.IsNullOrEmpty(rawIndex) ? 1 : int.Parse(rawIndex);
            var matchingEffectCount = CountPlaceholderEffects(ability.Effects, kind);

            if (index < 1 || matchingEffectCount < index)
            {
                errors.Add(
                    $"{essenceId}/{ability.Id}: description placeholder '{match.Value}' could not be resolved to an effect.");
            }
        }
    }

    private static int CountPlaceholderEffects(
        IEnumerable<AbilityEffectSpec> effects,
        string kind)
    {
        var normalizedKind = kind.ToLowerInvariant();
        return normalizedKind switch
        {
            "eventscaling" => effects.Count(x => x.EventMagnitudeCoefficient != 0),
            "conditionscaling" => effects.Count(x => x.ConditionScalingCoefficient != 0),
            "statusscaling" => effects.Count(x => x.StatusScalingCoefficient != 0),
            "duration" => effects.Count(x => x.DurationTicks > 0),
            "damage" => effects.Count(x => x.Operation == AbilityEffectOperation.Damage),
            "heal" => effects.Count(x => x.Operation == AbilityEffectOperation.Heal),
            "barrier" => effects.Count(x => x.Operation == AbilityEffectOperation.GrantBarrier),
            "modify" => effects.Count(x => x.Operation == AbilityEffectOperation.ModifyAttribute),
            "resource" => effects.Count(x => x.Operation == AbilityEffectOperation.RestoreResource),
            "status" => effects.Count(x => x.Operation == AbilityEffectOperation.ApplyStatus),
            _ => Enum.TryParse<AbilityEffectOperation>(kind, true, out var operation)
                ? effects.Count(x => x.Operation == operation)
                : 0
        };
    }

    private static void ValidateAbilityModifiers(
        string essenceId,
        AbilitySpec? ability,
        IReadOnlyList<EssenceAbilityModifierDefinition> modifiers,
        List<string> errors)
    {
        if (ability is null)
            return;

        var abilityEffectIds = ability.Effects.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var modifier in modifiers)
        {
            var label = $"{essenceId}/{ability.Id}/modifier.{modifier.Operation}";
            if (string.IsNullOrWhiteSpace(modifier.Operation))
            {
                errors.Add($"{label}: operation is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(modifier.Target))
                errors.Add($"{label}: target effect id is required.");
            else if (!abilityEffectIds.Contains(modifier.Target))
                errors.Add($"{label}: target effect '{modifier.Target}' could not be resolved.");

            if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                if (modifier.Value <= -1)
                    errors.Add($"{label}: AddMultiplier value must be greater than -1.");
                continue;
            }

            if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase))
            {
                if (modifier.Effect is null)
                {
                    errors.Add($"{label}: AddEffect requires an effect payload.");
                    continue;
                }

                if (abilityEffectIds.Contains(modifier.Effect.Id))
                    errors.Add($"{label}: AddEffect payload effect id '{modifier.Effect.Id}' must be unique within the ability.");

                ValidateEffect(essenceId, $"{ability.Id}/{modifier.Effect.Id}", modifier.Effect, errors);
                continue;
            }

            errors.Add($"{label}: unsupported modifier operation '{modifier.Operation}'.");
        }
    }

    private static void ValidateEffect(
        string essenceId,
        string ownerId,
        AbilityEffectSpec effect,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(effect.Id)) errors.Add($"{essenceId}/{ownerId}: effect id is required.");
        if (!AllowsNegativeValue(effect.Operation)
            && effect.BaseValue < 0)
        {
            errors.Add($"{essenceId}/{ownerId}: scaling values cannot be negative.");
        }
        if (effect.Operation == AbilityEffectOperation.ModifyAttribute && effect.Attribute is null)
            errors.Add($"{essenceId}/{ownerId}: ModifyAttribute requires attribute.");
        if (effect.Attribute is { } attribute && !AttributeCatalog.IsContentFacing(attribute))
            errors.Add($"{essenceId}/{ownerId}: attribute '{effect.Attribute}' is runtime-only and cannot be authored.");
        if (effect.Operation == AbilityEffectOperation.ApplyStatus && string.IsNullOrWhiteSpace(effect.StatusId))
            errors.Add($"{essenceId}/{ownerId}: ApplyStatus requires status.");
        if (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is null)
            errors.Add($"{essenceId}/{ownerId}: ApplyCondition requires condition.");
        if (effect.StaggerPower < 0)
            errors.Add($"{essenceId}/{ownerId}: staggerPower cannot be negative.");
        if (effect.StaggerPower > 0
            && (effect.Operation != AbilityEffectOperation.ApplyCondition
                || effect.Condition is not (StandardConditionType.Stun or StandardConditionType.Freeze)))
        {
            errors.Add($"{essenceId}/{ownerId}: staggerPower requires an ApplyCondition Stun or Freeze effect.");
        }
        ValidateStandardConditionEffect(essenceId, ownerId, effect, errors);

        ValidateConditions(essenceId, ownerId, effect.Conditions, errors);
    }

    private static void ValidateConditions(string essenceId, string ownerId, IEnumerable<AbilityConditionSpec> conditions, List<string> errors)
    {
        foreach (var condition in conditions)
        {
            if (condition.Type == AbilityConditionType.HasTag &&
                (string.IsNullOrWhiteSpace(condition.Tag) || !IsKnownTag(condition.Tag)))
            {
                errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires a known tag.");
            }

            if (condition.Type == AbilityConditionType.ChancePercent)
            {
                if (condition.Value is < 0 or > 100)
                    errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires a value from 0 to 100.");
            }

            if (condition.Type == AbilityConditionType.StatusStacksAtLeast
                && (string.IsNullOrWhiteSpace(condition.StatusId) || condition.Value <= 0))
            {
                errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires status and a positive stack value.");
            }

            if ((condition.Type == AbilityConditionType.HasCondition
                 || condition.Type == AbilityConditionType.ConditionStacksAtLeast)
                && condition.Condition is null)
            {
                errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires condition.");
            }

            if (condition.Type == AbilityConditionType.ConditionStacksAtLeast && condition.Value <= 0)
                errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires a positive stack value.");
        }
    }

    private static bool AllowsNegativeValue(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute
            or AbilityEffectOperation.ModifyStatusStacks
            or AbilityEffectOperation.ModifyThreat
            or AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyRegenerationInterval
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition;

    private static void ValidateStandardConditionEffect(
        string essenceId,
        string ownerId,
        AbilityEffectSpec effect,
        ICollection<string> errors)
    {
        if (effect.Operation != AbilityEffectOperation.ApplyCondition || effect.Condition is not { } condition)
            return;

        if (condition == StandardConditionType.Thorns && effect.DurationTicks <= 0)
            errors.Add($"{essenceId}/{ownerId}: Thorns requires a positive durationTicks.");
        if (condition == StandardConditionType.Thorns && effect.IntervalTicks > 0)
            errors.Add($"{essenceId}/{ownerId}: Thorns cannot use intervalTicks because durationTicks is its condition duration.");
        if (condition != StandardConditionType.Thorns
            && effect.DurationTicks > 0
            && effect.IntervalTicks <= 0)
        {
            errors.Add($"{essenceId}/{ownerId}: durationTicks requires intervalTicks; condition duration comes from canonical X or its fixed rule.");
        }
        if (condition is not StandardConditionType.Empower
            and not StandardConditionType.Weaken
            and not StandardConditionType.Haste
            and not StandardConditionType.Slow
            && effect.BaseValue <= 0
            && effect.ScalingCoefficient <= 0)
        {
            errors.Add($"{essenceId}/{ownerId}: {condition} requires a positive condition value.");
        }
    }

    private static bool IsKnownTag(string tag) =>
        EssenceTagCatalog.AllTags.Contains(tag)
        || tag.StartsWith("Creature.", StringComparison.OrdinalIgnoreCase);

    public void ThrowIfInvalid(IReadOnlyList<EssenceDefinition> definitions)
    {
        var errors = Validate(definitions);
        if (errors.Count > 0)
            throw new InvalidOperationException("Essence definition validation failed: " + string.Join(" | ", errors));
    }
}
