using Application.Interfaces.Services.LL.Essences;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;
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
            if (definition.ActiveAbility is not null && !definition.ActiveAbility.Kind.Equals(AbilityDefinitionKind.Active, StringComparison.OrdinalIgnoreCase))
                errors.Add($"{definition.Id}: activeAbilityId '{definition.ActiveAbilityId}' must reference an Active ability definition.");
            if (definition.PassiveAbility is not null && !definition.PassiveAbility.Kind.Equals(AbilityDefinitionKind.Passive, StringComparison.OrdinalIgnoreCase))
                errors.Add($"{definition.Id}: passiveAbilityId '{definition.PassiveAbilityId}' must reference a Passive ability definition.");
            if (definition.Evolution is null || string.IsNullOrWhiteSpace(definition.Evolution.Id)) errors.Add($"{definition.Id}: exactly one evolution is required.");

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

            var tiers = definition.Ascension?.Tiers ?? [];
            if (tiers.Count != 4 || tiers.Select(x => x.Tier).Order().SequenceEqual([0, 1, 2, 3]) == false)
                errors.Add($"{definition.Id}: ascension tiers 0-3 are required.");

            if (definition.Evolution?.RequiredAscensionTier is < 0 or > 3)
                errors.Add($"{definition.Id}: evolution required tier must be 0-3.");
        }

        return errors;
    }

    private static void ValidateAbility(string essenceId, AbilityDefinition? ability, List<string> errors)
    {
        if (ability is null) return;

        if (string.IsNullOrWhiteSpace(ability.Kind)) errors.Add($"{essenceId}/{ability.Id}: ability kind is required.");
        else if (!AbilityDefinitionKind.All.Contains(ability.Kind)) errors.Add($"{essenceId}/{ability.Id}: unknown ability kind '{ability.Kind}'.");

        if (!string.IsNullOrWhiteSpace(ability.Targeting) && !AbilityTargetSelector.All.Contains(ability.Targeting))
            errors.Add($"{essenceId}/{ability.Id}: unknown target selector '{ability.Targeting}'.");

        foreach (var trigger in ability.Triggers)
        {
            var normalized = AbilityTriggerType.Normalize(trigger.Type);
            if (!AbilityTriggerType.All.Contains(normalized))
                errors.Add($"{essenceId}/{ability.Id}: unknown trigger '{trigger.Type}'.");
        }

        ValidateConditions(essenceId, ability.Id, ability.Conditions, errors);

        var duplicateEffectIds = ability.Effects
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);
        errors.AddRange(duplicateEffectIds.Select(id => $"{essenceId}/{ability.Id}: duplicate effect id '{id}'."));

        foreach (var effect in ability.Effects)
        {
            if (string.IsNullOrWhiteSpace(effect.Id)) errors.Add($"{essenceId}/{ability.Id}: effect id is required.");
            if (!AbilityEffectType.All.Contains(effect.Type)) errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: unknown effect type '{effect.Type}'.");
            if (!AbilityTargetSelector.All.Contains(effect.Target)) errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: unknown target selector '{effect.Target}'.");
            if (effect.Type.Equals(AbilityEffectType.ModifyAttribute, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(effect.Attribute))
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: ModifyAttribute requires attribute.");
            if (!string.IsNullOrWhiteSpace(effect.Attribute))
            {
                if (!Enum.TryParse<AttributeType>(effect.Attribute, ignoreCase: true, out var attributeType))
                    errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: unknown attribute '{effect.Attribute}'.");
                else if (!AttributeCatalog.IsContentFacing(attributeType))
                    errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: attribute '{effect.Attribute}' is runtime-only and cannot be authored.");
            }
            if (effect.Type.Equals(AbilityEffectType.ApplyStatus, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(effect.Status))
                errors.Add($"{essenceId}/{ability.Id}/{effect.Id}: ApplyStatus requires status.");

            ValidateConditions(essenceId, $"{ability.Id}/{effect.Id}", effect.Conditions, errors);
        }
    }

    private static void ValidateConditions(string essenceId, string ownerId, IEnumerable<AbilityConditionDefinition> conditions, List<string> errors)
    {
        foreach (var condition in conditions)
        {
            if (!AbilityConditionType.All.Contains(condition.Type))
                errors.Add($"{essenceId}/{ownerId}: unknown condition '{condition.Type}'.");

            if ((condition.Type.Equals(AbilityConditionType.SourceHasTag, StringComparison.OrdinalIgnoreCase) ||
                 condition.Type.Equals(AbilityConditionType.TargetHasTag, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(condition.Tag) || !EssenceTagCatalog.AllTags.Contains(condition.Tag)))
            {
                errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires a known tag.");
            }

            if (condition.Type.Equals(AbilityConditionType.IsSpecies, StringComparison.OrdinalIgnoreCase))
            {
                var speciesTag = string.IsNullOrWhiteSpace(condition.Tag)
                    ? string.Empty
                    : condition.Tag.StartsWith("Species.", StringComparison.OrdinalIgnoreCase) ? condition.Tag : $"Species.{condition.Tag}";
                if (!EssenceTagCatalog.AllTags.Contains(speciesTag))
                    errors.Add($"{essenceId}/{ownerId}: condition '{condition.Type}' requires a known species tag.");
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
