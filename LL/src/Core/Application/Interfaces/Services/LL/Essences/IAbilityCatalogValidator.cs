using Domain.Models.AbilityDefinitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogValidator
{
    IReadOnlyList<string> Validate(IReadOnlyList<AbilityDefinition> abilities);
    void ThrowIfInvalid(IReadOnlyList<AbilityDefinition> abilities);
    AbilityCatalogSupportMatrix GetSupportMatrix();
}

public sealed record AbilityCatalogSupportMatrix(
    IReadOnlyList<string> KnownEffectTypes,
    IReadOnlyList<string> SupportedEffectTypes,
    IReadOnlyList<string> KnownTriggerTypes,
    IReadOnlyList<string> SupportedTriggerTypes,
    IReadOnlyList<string> KnownConditionTypes,
    IReadOnlyList<string> SupportedConditionTypes,
    IReadOnlyList<string> KnownTargetSelectors,
    IReadOnlyList<string> SupportedTargetSelectors)
{
    public IReadOnlyList<string> UnsupportedEffectTypes => Except(KnownEffectTypes, SupportedEffectTypes);
    public IReadOnlyList<string> UnsupportedTriggerTypes => Except(KnownTriggerTypes, SupportedTriggerTypes);
    public IReadOnlyList<string> UnsupportedConditionTypes => Except(KnownConditionTypes, SupportedConditionTypes);
    public IReadOnlyList<string> UnsupportedTargetSelectors => Except(KnownTargetSelectors, SupportedTargetSelectors);

    private static IReadOnlyList<string> Except(IReadOnlyList<string> known, IReadOnlyList<string> supported) =>
        known.Except(supported, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
