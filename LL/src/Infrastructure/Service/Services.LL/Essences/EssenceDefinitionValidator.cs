using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Essences;

public sealed class EssenceDefinitionValidator : IEssenceDefinitionValidator
{
    public IReadOnlyList<string> Validate(IReadOnlyList<EssenceDefinition> definitions)
    {
        var errors = new List<string>();
        var duplicateIds = definitions.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key);
        errors.AddRange(duplicateIds.Select(id => $"Duplicate Essence id '{id}'."));

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id)) errors.Add("Essence id is required.");
            if (string.IsNullOrWhiteSpace(definition.SourceMonsterId)) errors.Add($"{definition.Id}: sourceMonsterId is required.");
            if (string.IsNullOrWhiteSpace(definition.ActiveAbilityId)) errors.Add($"{definition.Id}: activeAbilityId is required.");
            if (string.IsNullOrWhiteSpace(definition.PassiveAbilityId)) errors.Add($"{definition.Id}: passiveAbilityId is required.");
            if (definition.ActiveAbility is null || string.IsNullOrWhiteSpace(definition.ActiveAbility.Id)) errors.Add($"{definition.Id}: activeAbilityId '{definition.ActiveAbilityId}' could not be resolved.");
            if (definition.PassiveAbility is null || string.IsNullOrWhiteSpace(definition.PassiveAbility.Id)) errors.Add($"{definition.Id}: passiveAbilityId '{definition.PassiveAbilityId}' could not be resolved.");
            if (definition.ActiveAbility is not null && definition.ActiveAbility.Kind != AbilitySpecKind.Active)
                errors.Add($"{definition.Id}: activeAbilityId '{definition.ActiveAbilityId}' must reference an Active ability definition.");
            if (definition.PassiveAbility is not null && definition.PassiveAbility.Kind != AbilitySpecKind.Passive)
                errors.Add($"{definition.Id}: passiveAbilityId '{definition.PassiveAbilityId}' must reference a Passive ability definition.");
            if (definition.Evolution is null || string.IsNullOrWhiteSpace(definition.Evolution.Id)) errors.Add($"{definition.Id}: exactly one evolution is required.");
            if (definition.AttributeBonuses.Count == 0) errors.Add($"{definition.Id}: at least one attribute bonus is required.");

            foreach (var tag in definition.Tags.Concat(definition.ActiveAbility?.Tags ?? []).Concat(definition.PassiveAbility?.Tags ?? []).Concat(definition.Evolution?.AddsTags ?? []))
            {
                if (!EssenceTagCatalog.AllTags.Contains(tag)) errors.Add($"{definition.Id}: unknown tag '{tag}'.");
            }

            foreach (var bonus in definition.AttributeBonuses.Concat(definition.Evolution?.AttributeModifierChanges ?? []))
            {
                if (!AttributeCatalog.IsKnown(bonus.Attribute))
                    errors.Add($"{definition.Id}: unknown attribute '{bonus.Attribute}'.");
                else if (!AttributeCatalog.IsContentFacing(bonus.Attribute))
                    errors.Add($"{definition.Id}: attribute '{bonus.Attribute}' is runtime-only and cannot be authored as an Essence bonus.");
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

        if (ability.Kind == AbilitySpecKind.Active)
        {
            if (ability.CooldownTicks <= 0) errors.Add($"{essenceId}/{ability.Id}: active ability cooldown must be greater than zero.");
            if (ability.Effects.Count == 0) errors.Add($"{essenceId}/{ability.Id}: active ability requires at least one effect.");
        }

        if (ability.Kind == AbilitySpecKind.Passive
            && ability.Triggers.Count == 0
            && ability.Effects.Count == 0)
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
            if (effect.Operation != AbilityEffectOperation.ModifyAttribute
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

            ValidateConditions(essenceId, $"{ability.Id}/{effect.Id}", effect.Conditions, errors);
        }
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
        if (effect.Operation != AbilityEffectOperation.ModifyAttribute
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

        ValidateConditions(essenceId, ownerId, effect.Conditions, errors);
    }

    private static void ValidateConditions(string essenceId, string ownerId, IEnumerable<AbilityConditionSpec> conditions, List<string> errors)
    {
        foreach (var condition in conditions)
        {
            if (condition.Type == AbilityConditionType.HasTag &&
                (string.IsNullOrWhiteSpace(condition.Tag) || !EssenceTagCatalog.AllTags.Contains(condition.Tag)))
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
        }
    }

    public void ThrowIfInvalid(IReadOnlyList<EssenceDefinition> definitions)
    {
        var errors = Validate(definitions);
        if (errors.Count > 0)
            throw new InvalidOperationException("Essence definition validation failed: " + string.Join(" | ", errors));
    }
}
