using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Professions.Crafting.V2;

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
        var definitions = content.Items.Select(x => new EquipmentDefinition(x.Id, x.Name, x.Id, EquipmentRarity.Common))
            .Concat(named.Select(x => new EquipmentDefinition(x.Id, x.Name, x.ArchetypeId,
                EquipmentRarity.Rare, x.NativeStyleId, x.RandomDiscoveryBaseScrap)));
        var styleContent = JsonSerializer.Deserialize<StyleContent[]>(File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(path)!, "equipment-styles.v1.json")), options)
            ?? throw new InvalidOperationException("Missing Equipment progression styles.");
        var styles = styleContent.Select(x => new EquipmentProgressionStyleSource(x.Id, x.Name, x.ItemBaseId,
            new(x.Id, x.CompatibleArchetypeIds, x.StatWeights, x.EquipmentSetId))).ToArray();
        if (styles.Select(x => x.ItemBaseId).Distinct(StringComparer.Ordinal).Count() != styles.Length)
            throw new InvalidOperationException("Blueprint books must identify one Equipment progression style.");
        var evaluator = new EquipmentEvaluator(new(content.BalanceVersion, content.BaseTierBudget,
            content.StyleShare, content.RankIncrement), archetypes, styles.Select(x => x.Style), definitions);
        return new(evaluator, content.Items.Select(x => x.Id), styles);
    }

    public static ForgePrices LoadForgePrices(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Deserialize<ForgePrices>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Missing Equipment progression Forge prices.");
    }

    public static EquipmentAcquisitionCatalog LoadAcquisition(StarterEquipmentCatalog equipment, string path) => new(equipment.Evaluator,
        JsonSerializer.Deserialize<EquipmentProtectionPool[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Missing equipment protection pools."));

    private sealed record NamedContent(string Id, string Name, string ArchetypeId, string NativeStyleId, long RandomDiscoveryBaseScrap);

    public static CombatAcquisitionCatalog LoadOrdinary(StarterEquipmentCatalog equipment, string path) => new(equipment,
        JsonSerializer.Deserialize<CombatAcquisitionRules[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Missing ordinary equipment acquisition rules."));

    private sealed record StyleContent(string Id, string Name, string ItemBaseId, IReadOnlyList<string> CompatibleArchetypeIds,
        IReadOnlyDictionary<AttributeType, double> StatWeights, string? EquipmentSetId);

    private sealed record StarterContent(int BalanceVersion, double BaseTierBudget, double StyleShare,
        double RankIncrement, int MaximumTier, IReadOnlyList<StarterItem> Items);
    private sealed record StarterItem(string Id, string ItemBaseId, string Name, EquipmentType EquipmentType,
        EquipmentBehaviorDefinition Behavior, IReadOnlyDictionary<AttributeType, double> StatWeights);
}
