namespace Domain.Models.Items.Equipments.Progression;

public sealed record StarterEquipmentOption(string DefinitionId, string Name, EquipmentType EquipmentType)
{
    public IReadOnlyDictionary<Domain.Models.Attributes.AttributeType, float> Stats { get; init; } =
        new Dictionary<Domain.Models.Attributes.AttributeType, float>();
}

public sealed class StarterEquipmentCatalog
{
    private readonly IReadOnlyDictionary<string, StarterEquipmentOption> _options;
    private readonly IReadOnlyList<string> _accessories;

    public StarterEquipmentCatalog(EquipmentEvaluator evaluator, IEnumerable<string> definitionIds, IEnumerable<EquipmentProgressionStyleSource>? styles = null)
    {
        Evaluator = evaluator;
        Styles = Array.AsReadOnly((styles ?? []).ToArray());
        _options = definitionIds.Select(id => evaluator.Evaluate(id, 1, 0, null))
            .Select(x =>
            {
                if (x.Definition.Rarity != EquipmentRarity.Common || x.Definition.NativeStyleId != null)
                    throw new ArgumentException("Starter definitions must be plain Common equipment.");
                return new StarterEquipmentOption(x.Definition.Id, x.Definition.Name, x.Archetype.EquipmentType) { Stats = x.Stats };
            }).ToDictionary(x => x.DefinitionId, StringComparer.Ordinal);
        _accessories = new[] { EquipmentType.Ring, EquipmentType.Necklace, EquipmentType.Relic }
            .Select(type => _options.Values.Single(x => x.EquipmentType == type).DefinitionId).ToArray();
        foreach (var type in new[] { EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs, EquipmentType.OneHanded, EquipmentType.TwoHanded, EquipmentType.OffHand })
            if (!_options.Values.Any(x => x.EquipmentType == type))
                throw new ArgumentException($"Missing starter equipment type {type}.");
    }

    public EquipmentEvaluator Evaluator { get; }
    public IReadOnlyList<EquipmentProgressionStyleSource> Styles { get; }
    public IReadOnlyList<StarterEquipmentOption> Options => _options.Values.OrderBy(x => x.EquipmentType).ThenBy(x => x.DefinitionId, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<StarterEquipmentOption> GetOptions(int tier) => Options.Select(option =>
        option with { Stats = Evaluator.Evaluate(option.DefinitionId, tier, 0, null).Stats }).ToArray();

    public IReadOnlyList<string> Select(StarterEquipmentGrantKind kind, IReadOnlyList<string> requestedIds)
    {
        if (kind == StarterEquipmentGrantKind.ReadyForRoad && requestedIds.Count == 0)
            return _accessories.ToArray();
        if (kind != StarterEquipmentGrantKind.FirstWeapon || requestedIds.Count is < 4 or > 5
            || requestedIds.Any(id => id is null || !_options.ContainsKey(id)))
            throw new ArgumentException("Select legal hands and one item for each armor slot.");
        var types = requestedIds.Select(id => _options[id].EquipmentType).ToArray();
        var armor = new[] { EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs };
        if (armor.Any(type => types.Count(x => x == type) != 1))
            throw new ArgumentException("Choose one Head, Chest and Legs item.");
        var hands = types.Where(type => !armor.Contains(type)).ToArray();
        if (!(hands.Length == 1 && hands[0] == EquipmentType.TwoHanded
            || hands.Length == 2 && hands.Count(x => x == EquipmentType.OneHanded) >= 1
                && hands.All(x => x is EquipmentType.OneHanded or EquipmentType.OffHand)))
            throw new ArgumentException("Choose one two-handed weapon, or a one-handed weapon with another weapon or offhand.");
        return requestedIds.Order(StringComparer.Ordinal).ToArray();
    }
}
