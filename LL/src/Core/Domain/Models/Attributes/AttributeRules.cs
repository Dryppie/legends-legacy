namespace Domain.Models.Attributes;

/// <summary>Explicit unit boundary between historical attributes and the current rules.</summary>
public static class AttributeRules
{
    public const int LegacyVersion = 17;
    public const int CurrentVersion = 18;
    public const float AbilityHasteCap = 200f / 3f;
    public const float TenacityCap = 80f;
    public const float TypedPenetrationCap = 40f;
    public const double DefenseHalfCapRating = 165d;

    public static void ValidateVersion(int version)
    {
        if (version is not LegacyVersion and not CurrentVersion)
            throw new ArgumentOutOfRangeException(nameof(version), "Unsupported attribute rules version.");
    }

    public static AttributeType RatingAttribute(AttributeType attribute) => attribute switch
    {
        AttributeType.Armor => AttributeType.ArmorRating,
        AttributeType.Resistance => AttributeType.ResistanceRating,
        _ => attribute
    };

    public static float Mitigation(double rating, float penetration = 0, float corrosion = 0)
    {
        if (!double.IsFinite(rating))
            throw new ArgumentOutOfRangeException(nameof(rating), "Defense must retain a finite rating.");
        var net = Math.Max(0d, rating) * (1d - Math.Clamp(corrosion, 0f, 50f) / 100d);
        var mitigation = .8d * (net / (net + DefenseHalfCapRating));
        // Penetration subtracts percentage points after the defense curve, never
        // making mitigation negative or bypassing block/general damage reduction.
        return (float)Math.Max(0d, mitigation - Math.Clamp(penetration, 0f, TypedPenetrationCap) / 100d);
    }

    /// <summary>Compatibility conversion for authored effective defenses, never used to oppose a hit.</summary>
    public static float RatingFromLegacyPercent(float percent)
    {
        // Legacy values at the old cap require a finite conversion, not an infinity sentinel.
        var value = Math.Clamp(percent, 0f, 79.99f);
        return (float)(DefenseHalfCapRating * value / (80d - value));
    }

    public static float HasteFromCooldownReduction(float reduction) =>
        (float)(100d * Math.Clamp(reduction, 0f, 40f) / (100d - Math.Clamp(reduction, 0f, 40f)));

    public static int CooldownTicks(int authoredTicks, float haste)
    {
        if (authoredTicks <= 0) return 0;
        return Math.Max(1, (int)Math.Ceiling(authoredTicks / (1d + Math.Clamp(haste, 0f, AbilityHasteCap) / 100d) - 1e-9d));
    }

    public static float TenacityResistanceChance(float tenacity) =>
        Effective(AttributeType.Tenacity, tenacity) / 100f;

    public static float Effective(AttributeType attribute, float raw)
    {
        if (!float.IsFinite(raw)) throw new ArgumentOutOfRangeException(nameof(raw));
        if (!AttributeCatalog.IsKnown(attribute)) return Math.Max(0f, raw);
        var definition = AttributeCatalog.Get(attribute);
        return definition.MaximumValue is { } cap && definition.CapKind == AttributeCapKind.Fixed
            ? Math.Clamp(raw, definition.MinimumValue, cap)
            : Math.Max(definition.MinimumValue, raw);
    }

    public static float RestorationMultiplier(float restoration) =>
        1f + Effective(AttributeType.Restoration, restoration) / 100f;

    public static bool IsOrdinaryEquipmentAttribute(AttributeType attribute) => attribute is
        AttributeType.Power or AttributeType.MaxHealth or AttributeType.Armor or AttributeType.Resistance
        or AttributeType.CritChance or AttributeType.CritDamage or AttributeType.ArmorPenetration
        or AttributeType.MagicPenetration or AttributeType.AttackSpeed or AttributeType.BlockChance
        or AttributeType.HealthRegeneration or AttributeType.Restoration or AttributeType.AbilityHaste
        or AttributeType.Tenacity;
}
