namespace Domain.Models.Items.Equipments.Progression;

public sealed record CombatAcquisitionArea(string AreaId);
public sealed record CombatAcquisitionSigil(string FamilyId, string ItemBaseId);

public sealed record EquipmentQualityWeights(
    double Crude,
    double Standard,
    double Fine,
    double Exceptional,
    double Masterpiece)
{
    public ItemQuality Roll(double roll)
    {
        if (!double.IsFinite(roll) || roll is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(roll));

        var cumulative = 0d;
        foreach (var (quality, weight) in Entries())
        {
            cumulative += weight;
            if (roll < cumulative)
                return quality;
        }

        return ItemQuality.Masterpiece;
    }

    public IReadOnlyList<(ItemQuality Quality, double Weight)> Entries() =>
    [
        (ItemQuality.Crude, Crude),
        (ItemQuality.Standard, Standard),
        (ItemQuality.Fine, Fine),
        (ItemQuality.Exceptional, Exceptional),
        (ItemQuality.Masterpiece, Masterpiece)
    ];

    public void Validate()
    {
        if (Entries().Any(x => !double.IsFinite(x.Weight) || x.Weight < 0)
            || Math.Abs(Entries().Sum(x => x.Weight) - 1d) > 0.000000001d)
            throw new ArgumentException("Equipment quality weights must be non-negative and total one.");
    }
}

public sealed record EquipmentRarityWeights(
    double Common,
    double Uncommon,
    double Rare,
    double Epic,
    double Unique,
    double Legendary,
    double Legacy)
{
    public EquipmentRarity Roll(double roll)
    {
        if (!double.IsFinite(roll) || roll is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(roll));

        var cumulative = 0d;
        foreach (var (rarity, weight) in Entries())
        {
            cumulative += weight;
            if (roll < cumulative)
                return rarity;
        }

        return EquipmentRarity.Legacy;
    }

    public IReadOnlyList<(EquipmentRarity Rarity, double Weight)> Entries() =>
    [
        (EquipmentRarity.Common, Common),
        (EquipmentRarity.Uncommon, Uncommon),
        (EquipmentRarity.Rare, Rare),
        (EquipmentRarity.Epic, Epic),
        (EquipmentRarity.Unique, Unique),
        (EquipmentRarity.Legendary, Legendary),
        (EquipmentRarity.Legacy, Legacy)
    ];

    public void Validate()
    {
        if (Entries().Any(x => !double.IsFinite(x.Weight) || x.Weight < 0)
            || Math.Abs(Entries().Sum(x => x.Weight) - 1d) > 0.000000001d)
            throw new ArgumentException("Equipment rarity weights must be non-negative and total one.");
    }
}

public sealed record EquipmentDropProfile(
    double DropChance,
    int Rank,
    EquipmentRarityWeights Rarities)
{
    public EquipmentQualityWeights Qualities { get; init; } = new(0d, 0.35d, 0.45d, 0.16d, 0.04d);
}

public sealed record CombatAcquisitionRules(
    string Version,
    string PoolId,
    int Region,
    int EquipmentTier,
    EquipmentDropProfile AreaEquipment,
    EquipmentDropProfile DungeonEquipment,
    double SigilDropChance,
    IReadOnlyList<CombatAcquisitionArea> Areas,
    IReadOnlyList<CombatAcquisitionSigil> Sigils,
    string RegionName);

public sealed class CombatAcquisitionCatalog
{
    public CombatAcquisitionCatalog(StarterEquipmentCatalog equipment, IEnumerable<CombatAcquisitionRules> pools)
    {
        Equipment = equipment;
        Pools = Array.AsReadOnly(pools.Select(r => r with
        {
            Areas = Array.AsReadOnly(r.Areas.ToArray()),
            Sigils = Array.AsReadOnly(r.Sigils.ToArray())
        }).ToArray());

        if (Pools.Count == 0
            || Pools.Select(r => r.PoolId).Distinct(StringComparer.Ordinal).Count() != Pools.Count
            || Pools.Select(r => r.Region).Distinct().Count() != Pools.Count
            || Pools.SelectMany(r => r.Areas).Select(a => a.AreaId).Distinct(StringComparer.Ordinal).Count()
                != Pools.Sum(r => r.Areas.Count))
            throw new ArgumentException("Equipment drop pools, regions, and areas must be unique.");

        foreach (var rules in Pools)
        {
            EquipmentValidation.Id(rules.Version);
            EquipmentValidation.Id(rules.PoolId);
            EquipmentValidation.Id(rules.RegionName);
            if (rules.Region < 1 || rules.EquipmentTier < 1 || rules.Areas.Count == 0 || rules.Sigils.Count == 0
                || !ValidProfile(rules.AreaEquipment) || !ValidProfile(rules.DungeonEquipment)
                || !double.IsFinite(rules.SigilDropChance) || rules.SigilDropChance is <= 0 or > 1
                || rules.Areas.Select(x => x.AreaId).Distinct(StringComparer.Ordinal).Count() != rules.Areas.Count
                || rules.Sigils.Select(x => x.FamilyId).Distinct(StringComparer.Ordinal).Count() != rules.Sigils.Count
                || rules.Sigils.Select(x => x.ItemBaseId).Distinct(StringComparer.Ordinal).Count() != rules.Sigils.Count)
                throw new ArgumentException("Invalid equipment drop rules.");

            rules.AreaEquipment.Rarities.Validate();
            rules.DungeonEquipment.Rarities.Validate();
            rules.AreaEquipment.Qualities.Validate();
            rules.DungeonEquipment.Qualities.Validate();
            foreach (var area in rules.Areas) EquipmentValidation.Id(area.AreaId);
            foreach (var sigil in rules.Sigils)
            {
                EquipmentValidation.Id(sigil.FamilyId);
                EquipmentValidation.Id(sigil.ItemBaseId);
            }

            foreach (var rarity in Enum.GetValues<EquipmentRarity>())
            {
                var definitions = DropDefinitions(rarity);
                if (definitions.Count == 0)
                    throw new ArgumentException($"Missing {rarity} equipment drop definitions.");
                foreach (var definition in definitions)
                {
                    equipment.Evaluator.Evaluate(definition.Id, rules.EquipmentTier, rules.AreaEquipment.Rank, null);
                    equipment.Evaluator.Evaluate(definition.Id, rules.EquipmentTier, rules.DungeonEquipment.Rank, null);
                }
            }
        }
    }

    public StarterEquipmentCatalog Equipment { get; }
    public IReadOnlyList<CombatAcquisitionRules> Pools { get; }

    public CombatAcquisitionRules? FindArea(string areaId) => Pools.SingleOrDefault(pool =>
        pool.Areas.Any(area => area.AreaId.Equals(areaId, StringComparison.Ordinal)));

    public CombatAcquisitionRules? FindRegion(int region) => Pools.SingleOrDefault(pool => pool.Region == region);

    public IReadOnlyList<EquipmentDefinition> DropDefinitions(EquipmentRarity rarity) => Equipment.Evaluator.Definitions
        .Where(definition => definition.Rarity == rarity)
        .OrderBy(definition => definition.ArchetypeId, StringComparer.Ordinal)
        .ThenBy(definition => definition.Id, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<EquipmentDefinition> BaseDropDefinitions(EquipmentRarity rarity) =>
        DropDefinitions(rarity).Where(x => x.NativeStyleId is null).ToArray();

    private static bool ValidProfile(EquipmentDropProfile profile) => profile is not null
        && double.IsFinite(profile.DropChance) && profile.DropChance is > 0 and <= 1
        && profile.Rank is >= 0 and <= EquipmentBalance.MaximumRank
        && profile.Rarities is not null
        && profile.Qualities is not null;
}
