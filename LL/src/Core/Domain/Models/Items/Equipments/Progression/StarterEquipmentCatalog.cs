using Domain.Models.Items.Equipments.Sets;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>
/// Authoritative runtime equipment content. This deliberately owns combat-facing
/// definitions so live equipment never needs to consult the retired crafting catalog.
/// </summary>
public class EquipmentCatalog
{
    private readonly IReadOnlyDictionary<string, EquipmentBase> _equipmentBases;
    private readonly IReadOnlyDictionary<string, EquipmentSetDefinition> _equipmentSets;

    public EquipmentCatalog(
        EquipmentEvaluator evaluator,
        IEnumerable<EquipmentStyle>? styles = null,
        IEnumerable<EquipmentSetDefinition>? equipmentSets = null,
        IReadOnlyDictionary<string, EquipmentBase>? equipmentBases = null)
    {
        Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        Styles = Array.AsReadOnly((styles ?? []).ToArray());
        _equipmentSets = (equipmentSets ?? []).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        _equipmentBases = equipmentBases is null
            ? new Dictionary<string, EquipmentBase>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, EquipmentBase>(equipmentBases, StringComparer.OrdinalIgnoreCase);

        foreach (var style in Styles)
        {
            if (style.EquipmentSetId is not null && !_equipmentSets.ContainsKey(style.EquipmentSetId))
                throw new ArgumentException(
                    $"Equipment style '{style.Id}' references unknown set '{style.EquipmentSetId}'.",
                    nameof(equipmentSets));
        }
    }

    public EquipmentEvaluator Evaluator { get; }
    public IReadOnlyList<EquipmentStyle> Styles { get; }
    public IReadOnlyList<EquipmentSetDefinition> EquipmentSets =>
        _equipmentSets.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
    public IReadOnlyDictionary<string, EquipmentBase> EquipmentBases => _equipmentBases;

    public EquipmentSetDefinition? GetEquipmentSet(string equipmentSetId) =>
        _equipmentSets.GetValueOrDefault(equipmentSetId);

    public EquipmentBase GetEquipmentBase(string itemBaseId) =>
        _equipmentBases.TryGetValue(itemBaseId, out var itemBase)
            ? itemBase
            : throw new ArgumentException($"Unknown equipment item base '{itemBaseId}'.", nameof(itemBaseId));
}

public sealed record StarterEquipmentOption(string DefinitionId, string Name, EquipmentType EquipmentType)
{
    public IReadOnlyDictionary<Domain.Models.Attributes.AttributeType, float> Stats { get; init; } =
        new Dictionary<Domain.Models.Attributes.AttributeType, float>();
}

public sealed class StarterEquipmentCatalog : EquipmentCatalog
{
    private readonly IReadOnlyDictionary<string, StarterEquipmentOption> _options;

    public StarterEquipmentCatalog(
        EquipmentEvaluator evaluator,
        IEnumerable<string> definitionIds,
        IEnumerable<EquipmentStyle>? styles = null,
        IEnumerable<EquipmentSetDefinition>? equipmentSets = null,
        IReadOnlyDictionary<string, EquipmentBase>? equipmentBases = null)
        : base(evaluator, styles, equipmentSets, equipmentBases)
    {
        _options = definitionIds.Select(id => evaluator.Evaluate(id, 1, 0, null))
            .Select(x =>
            {
                if (x.Definition.Rarity != EquipmentRarity.Common || x.Definition.NativeStyleId != null)
                    throw new ArgumentException("Starter definitions must be plain Common equipment.");
                return new StarterEquipmentOption(x.Definition.Id, x.Definition.Name, x.Archetype.EquipmentType) { Stats = x.Stats };
            }).ToDictionary(x => x.DefinitionId, StringComparer.Ordinal);
        foreach (var type in new[] { EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs, EquipmentType.OneHanded, EquipmentType.TwoHanded, EquipmentType.OffHand })
            if (!_options.Values.Any(x => x.EquipmentType == type))
                throw new ArgumentException($"Missing starter equipment type {type}.");
    }

    public IReadOnlyList<StarterEquipmentOption> Options => _options.Values.OrderBy(x => x.EquipmentType).ThenBy(x => x.DefinitionId, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<StarterEquipmentOption> GetOptions(int tier) => Options.Select(option =>
        option with { Stats = Evaluator.Evaluate(option.DefinitionId, tier, 0, null).Stats }).ToArray();

    public IReadOnlyList<string> Select(StarterEquipmentGrantKind kind, IReadOnlyList<string> requestedIds)
    {
        if (kind != StarterEquipmentGrantKind.FirstWeapon || requestedIds.Count != 1
            || requestedIds[0] is null || !_options.TryGetValue(requestedIds[0], out var option)
            || option.EquipmentType != EquipmentType.OneHanded)
            throw new ArgumentException("Select one starter one-handed weapon.");
        return [option.DefinitionId];
    }
}
