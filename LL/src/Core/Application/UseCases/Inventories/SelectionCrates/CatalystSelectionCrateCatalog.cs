namespace Application.UseCases.Inventories.SelectionCrates;

public sealed record CatalystSelectionOptionDefinition(
    string Id,
    string Name,
    string ItemId,
    int Quantity);

public static class CatalystSelectionCrateCatalog
{
    public const string ItemBaseId = "item.catalyst_selection_crate";

    public static IReadOnlyList<CatalystSelectionOptionDefinition> Options { get; } =
    [
        new("fury", "Fury Hearts", "fury_heart", 6),
        new("arcane", "Arcane Focuses", "arcane_focus", 6),
        new("venom", "Venom Glands", "venom_gland", 6),
        new("hive", "Royal Chitin Plates", "royal_chitin_plate", 6),
        new("primal", "Hive Ichors", "hive_ichor", 6)
    ];
}
