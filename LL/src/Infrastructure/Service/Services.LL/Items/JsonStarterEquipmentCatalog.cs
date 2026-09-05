using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Sets;

namespace Services.LL.Items;

public static class JsonStarterEquipmentCatalog
{
    public static StarterEquipmentCatalog Load(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var content = JsonSerializer.Deserialize<StarterContent>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Missing Equipment progression starter catalog.");
        var archetypes = content.Items.Select(x => new EquipmentArchetype(x.Id, x.ItemBaseId,
            x.EquipmentType, x.Behavior, x.StatWeights, minimumTier: 1, maximumTier: content.MaximumTier));
        var named = JsonSerializer.Deserialize<NamedContent[]>(File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(path)!, "equipment-named.v1.json")), options)
            ?? throw new InvalidOperationException("Missing Equipment progression named equipment.");
        var definitions = content.Items.SelectMany(x => Enum.GetValues<EquipmentRarity>().Select(rarity =>
                new EquipmentDefinition(
                    rarity == EquipmentRarity.Common
                        ? x.Id
                        : $"{x.Id}.rarity.{rarity.ToString().ToLowerInvariant()}",
                    x.Name,
                    x.Id,
                    rarity)))
            .Concat(named.Select(x => new EquipmentDefinition(x.Id, x.Name, x.ArchetypeId,
                EquipmentRarity.Rare, x.NativeStyleId)));
        var styleContent = JsonSerializer.Deserialize<StyleContent[]>(File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(path)!, "equipment-styles.v1.json")), options)
            ?? throw new InvalidOperationException("Missing Equipment progression styles.");
        var styles = styleContent.Select(x => new EquipmentStyle(x.Id, x.CompatibleArchetypeIds,
            x.StatWeights, x.EquipmentSetId)).ToArray();
        var root = Path.GetDirectoryName(path)!;
        var sets = JsonSerializer.Deserialize<EquipmentSetDefinition[]>(File.ReadAllText(
            Path.Combine(root, "equipment-sets.v1.json")), options)
            ?? throw new InvalidOperationException("Missing equipment set definitions.");
        var equipmentBases = ReadEquipmentBases(
            Path.Combine(Directory.GetParent(root)!.FullName, "items", "items.json"), options);
        var evaluator = new EquipmentEvaluator(new(content.BalanceVersion, content.BaseTierBudget,
            content.StyleShare, content.RankIncrement), archetypes, styles, definitions);
        return new(evaluator, content.Items.Select(x => x.Id), styles, sets, equipmentBases);
    }

    private static IReadOnlyDictionary<string, EquipmentBase> ReadEquipmentBases(
        string path,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .EnumerateArray()
            .Where(element =>
                element.TryGetProperty("itemType", out var itemType)
                && itemType.GetString()?.Equals("Equipment", StringComparison.OrdinalIgnoreCase) == true)
            .Select(element =>
                JsonSerializer.Deserialize<EquipmentBase>(element.GetRawText(), options)
                ?? throw new InvalidOperationException("Unable to parse an equipment item definition."))
            .ToDictionary(equipment => equipment.Id, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record NamedContent(string Id, string Name, string ArchetypeId, string NativeStyleId);

    public static CombatAcquisitionCatalog LoadOrdinary(StarterEquipmentCatalog equipment, string path) => new(equipment,
        JsonSerializer.Deserialize<CombatAcquisitionRules[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Missing ordinary equipment acquisition rules."));

    private sealed record StyleContent(string Id, IReadOnlyList<string> CompatibleArchetypeIds,
        IReadOnlyDictionary<AttributeType, double> StatWeights, string? EquipmentSetId);

    private sealed record StarterContent(int BalanceVersion, double BaseTierBudget, double StyleShare,
        double RankIncrement, int MaximumTier, IReadOnlyList<StarterItem> Items);
    private sealed record StarterItem(string Id, string ItemBaseId, string Name, EquipmentType EquipmentType,
        EquipmentBehaviorDefinition Behavior, IReadOnlyDictionary<AttributeType, double> StatWeights);
}
