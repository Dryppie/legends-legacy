namespace Application.UseCases.Inventories.SelectionCrates;

public sealed record SelectionContainerOptionDefinition(
    string Id,
    string Name,
    string ItemId,
    int Quantity);

public sealed record SelectionContainerDefinition(
    string ItemBaseId,
    string DisplayName,
    string SelectionLabel,
    IReadOnlyList<SelectionContainerOptionDefinition> Options);

public static class CatalystSelectionCrateCatalog
{
    public const string ItemBaseId = "item.catalyst_selection_crate";

    public static IReadOnlyList<SelectionContainerOptionDefinition> Options { get; } =
    [
        new("fury", "Fury Hearts", "fury_heart", 6),
        new("arcane", "Arcane Focuses", "arcane_focus", 6),
        new("endurance", "Endurance Cores", "endurance_core", 6),
        new("phoenix", "Phoenix Embers", "phoenix_ember", 6)
    ];
}

public static class BlueprintSelectionBoxCatalog
{
    public const string ItemBaseId = "item.blueprint_selection_box";

    public static IReadOnlyList<SelectionContainerOptionDefinition> Options { get; } =
    [
        new("fury", "Fury Blueprint", "blueprint_fury", 1),
        new("arcane", "Arcane Blueprint", "blueprint_arcane", 1),
        new("execution", "Execution Blueprint", "blueprint_execution", 1),
        new("aegis", "Aegis Blueprint", "blueprint_aegis", 1),
        new("warden", "Warden Blueprint", "blueprint_warden", 1),
        new("endurance", "Endurance Blueprint", "blueprint_endurance", 1),
        new("phoenix", "Phoenix Blueprint", "blueprint_phoenix", 1),
        new("spirit", "Spirit Blueprint", "blueprint_spirit", 1),
        new("primal", "Primal Blueprint", "blueprint_primal", 1),
        new("venom", "Venom-Touched Sword Blueprint", "blueprint_venom_touched_sword", 1),
        new("hive", "Hivefang Dagger Blueprint", "blueprint_hivefang_dagger", 1)
    ];
}

public static class SelectionContainerCatalog
{
    private static readonly IReadOnlyDictionary<string, SelectionContainerDefinition> Definitions =
        new[]
        {
            new SelectionContainerDefinition(
                CatalystSelectionCrateCatalog.ItemBaseId,
                "Catalyst Selection Cache",
                "Catalyst",
                CatalystSelectionCrateCatalog.Options),
            new SelectionContainerDefinition(
                BlueprintSelectionBoxCatalog.ItemBaseId,
                "Blueprint Selection Box",
                "Blueprint",
                BlueprintSelectionBoxCatalog.Options)
        }.ToDictionary(definition => definition.ItemBaseId, StringComparer.OrdinalIgnoreCase);

    public static SelectionContainerDefinition? Find(string itemBaseId) =>
        Definitions.GetValueOrDefault(itemBaseId);
}
