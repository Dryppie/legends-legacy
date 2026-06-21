namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCatalogService
{
    Task<EssenceCatalogReport> GetCatalogAsync(CancellationToken cancellationToken);
}

public sealed record EssenceCatalogReport(
    IReadOnlyList<EssenceCatalogRegion> Regions);

public sealed record EssenceCatalogRegion(
    string Id,
    string Name,
    IReadOnlyList<EssenceCatalogArea> Areas);

public sealed record EssenceCatalogArea(
    string Id,
    string Name,
    string SourceType,
    string Tier,
    IReadOnlyList<EssenceCatalogMonster> Monsters);

public sealed record EssenceCatalogMonster(
    string Id,
    string Name,
    string ImagePath,
    string SourceType,
    string SourceName,
    string Tier,
    EssenceCatalogEssence? Essence);

public sealed record EssenceCatalogEssence(
    string Id,
    string Name,
    string Description,
    string Rarity,
    string? ItemId,
    IReadOnlyList<string> Tags,
    IReadOnlyList<EssenceCatalogAttributeBonus> AttributeBonuses,
    EssenceCatalogDrop Drop,
    EssenceCatalogAbility? ActiveAbility,
    EssenceCatalogAbility? PassiveAbility);

public sealed record EssenceCatalogAttributeBonus(
    string Attribute,
    double BaseValue);

public sealed record EssenceCatalogDrop(
    double BaseDropChance,
    double ResonanceGainPerFailedEligibleKill,
    double DropChanceBonusPerResonance,
    double MaxResonanceBonus);

public sealed record EssenceCatalogAbility(
    string Id,
    string Name,
    string Kind,
    string Description,
    int CooldownTicks,
    IReadOnlyList<string> Tags,
    IReadOnlyList<EssenceCatalogTrigger> Triggers,
    IReadOnlyList<EssenceCatalogEffect> Effects);

public sealed record EssenceCatalogTrigger(
    string Event,
    int InternalCooldownTicks,
    IReadOnlyList<string> EffectIds,
    IReadOnlyList<EssenceCatalogCondition> Conditions);

public sealed record EssenceCatalogEffect(
    string Id,
    string Operation,
    string Target,
    int BaseValue,
    string? ScalingAttribute,
    float ScalingCoefficient,
    string? Attribute,
    string? StatusId,
    string? SummonId,
    string Resource,
    int DurationTicks,
    int IntervalTicks,
    int Uses,
    string AttackType,
    string DamageType,
    float LifeStealPercentage,
    IReadOnlyList<string> Tags,
    IReadOnlyList<EssenceCatalogCondition> Conditions);

public sealed record EssenceCatalogCondition(
    string Type,
    string Subject,
    string? StatusId,
    string? Tag,
    int Value);
