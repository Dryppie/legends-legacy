namespace Domain.Models.Items.Equipments.Progression;

public sealed record EquipmentSelectionWeights(
    double Weapons,
    double Armor,
    double Jewelry,
    double OneHanded,
    double TwoHanded)
{
    public void Validate()
    {
        ValidateDistribution(Weapons, Armor, Jewelry);
        ValidateDistribution(OneHanded, TwoHanded);
    }

    public EquipmentDefinition Roll(
        IReadOnlyList<EquipmentDefinition> definitions,
        EquipmentEvaluator evaluator,
        Random random)
    {
        var byType = definitions.ToLookup(definition => evaluator.GetArchetype(definition.ArchetypeId).EquipmentType);
        // Off-hand equipment shares the one-handed allocation.
        var oneHanded = byType[EquipmentType.OneHanded].Concat(byType[EquipmentType.OffHand]).ToArray();
        var twoHanded = byType[EquipmentType.TwoHanded].ToArray();
        var armor = byType[EquipmentType.Head].Concat(byType[EquipmentType.Chest]).Concat(byType[EquipmentType.Legs]).ToArray();
        var jewelry = byType[EquipmentType.Ring].Concat(byType[EquipmentType.Necklace]).Concat(byType[EquipmentType.Relic]).ToArray();

        // Restricted sources may omit a category or a handedness. Redistribute its
        // weight among the remaining eligible groups at that stage of the roll.
        var hasWeapons = (oneHanded.Length > 0 && OneHanded > 0) || (twoHanded.Length > 0 && TwoHanded > 0);
        var category = RollWeighted(random,
            (Category.Weapons, hasWeapons ? Weapons : 0),
            (Category.Armor, armor.Length > 0 ? Armor : 0),
            (Category.Jewelry, jewelry.Length > 0 ? Jewelry : 0));
        var candidates = category switch
        {
            Category.Weapons => RollWeighted(random,
                (oneHanded, oneHanded.Length > 0 ? OneHanded : 0),
                (twoHanded, twoHanded.Length > 0 ? TwoHanded : 0)),
            Category.Armor => armor,
            Category.Jewelry => jewelry,
            _ => throw new InvalidOperationException("Unknown equipment drop category.")
        };
        return candidates[random.Next(candidates.Length)];
    }

    private static T RollWeighted<T>(Random random, params (T Value, double Weight)[] entries)
    {
        var eligible = entries.Where(entry => entry.Weight > 0).ToArray();
        var total = eligible.Sum(entry => entry.Weight);
        if (total <= 0)
            throw new InvalidOperationException("No eligible equipment has a positive selection weight.");

        var roll = random.NextDouble() * total;
        var cumulative = 0d;
        foreach (var entry in eligible)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
                return entry.Value;
        }

        return eligible[^1].Value;
    }

    private static void ValidateDistribution(params double[] weights)
    {
        if (weights.Any(weight => !double.IsFinite(weight) || weight < 0)
            || Math.Abs(weights.Sum() - 1d) > 0.000000001d)
            throw new ArgumentException("Equipment selection weights must be non-negative and total one in each distribution.");
    }

    private enum Category { Weapons, Armor, Jewelry }
}
