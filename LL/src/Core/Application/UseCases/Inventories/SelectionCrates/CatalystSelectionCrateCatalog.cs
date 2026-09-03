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

public static class ShenicEssenceTokenCatalog
{
    public static IReadOnlyList<SelectionContainerDefinition> Definitions { get; } =
    [
        EssenceToken("lumo_ruins", "Lumo Ruins",
            Essence("lumo_wisp", "Lumo Wisp"),
            Essence("lumo_sentinel", "Lumo Sentinel"),
            Essence("goblin", "Goblin"),
            Essence("goblin_archer", "Goblin Archer"),
            Essence("goblin_warrior", "Goblin Warrior")),
        EssenceToken("blood_grove", "Blood Grove",
            Essence("vampire_bat", "Vampire Bat"),
            Essence("raven", "Raven"),
            Essence("venomous_snake", "Venomous Snake"),
            Essence("nightshade_blossom", "Nightshade Blossom"),
            Essence("blood_zombie", "Blood Zombie")),
        EssenceToken("crystal_creek", "Crystal Creek",
            Essence("frost_imp", "Frost Imp"),
            Essence("crystal_wisp", "Crystal Wisp"),
            Essence("blue_slime", "Blue Slime"),
            Essence("transparent_slime", "Transparent Slime"),
            Essence("moss_lizard", "Moss Lizard")),
        EssenceToken("moonlit_graves", "Moonlit Graves",
            Essence("shadow_imp", "Shadow Imp"),
            Essence("grave_hound", "Grave Hound"),
            Essence("lost_soul", "Lost Soul"),
            Essence("grave_wisp", "Grave Wisp"),
            Essence("skeleton", "Skeleton")),
        EssenceToken("twilight_clearing", "Twilight Clearing",
            Essence("pixie", "Pixie"),
            Essence("wood_nymph", "Wood Nymph"),
            Essence("rainbow_slime", "Rainbow Slime"),
            Essence("enchanted_fairy", "Enchanted Fairy"),
            Essence("illusion_fox", "Illusion Fox")),
        EssenceToken("old_forest", "Old Forest",
            Essence("thornback_boar", "Thornback Boar"),
            Essence("hollow_stag", "Hollow Stag"),
            Essence("treant_sapling", "Treant Sapling"),
            Essence("glade_panther", "Glade Panther"),
            Essence("forest_spirit", "Forest Spirit")),
        EssenceToken("thornroot_hollow", "Thornroot Hollow",
            Essence("rotroot_shambler", "Rotroot Shambler"),
            Essence("spider", "Spider"),
            Essence("giant_spider", "Giant Spider"),
            Essence("venomous_spiderling", "Venomous Spiderling"),
            Essence("blackjaw_spider", "Blackjaw Spider")),
        EssenceToken("embercap_burrows", "Embercap Burrows",
            Essence("flame_imp", "Flame Imp"),
            Essence("smolder_rat", "Smolder Rat"),
            Essence("cinder_beetle", "Cinder Beetle"),
            Essence("red_slime", "Red Slime"),
            Essence("giant_worm", "Giant Worm")),
        EssenceToken("moonveil_marsh", "Moonveil Marsh",
            Essence("bog_mite", "Bog Mite"),
            Essence("green_slime", "Green Slime"),
            Essence("large_rat", "Large Rat"),
            Essence("viper", "Viper"),
            Essence("poisonous_rat", "Poisonous Rat")),
        EssenceToken("duskmire_hollow", "Duskmire Hollow",
            Essence("rotfly_toad", "Rotfly Toad"),
            Essence("brown_slime", "Brown Slime"),
            Essence("cave_bat", "Cave Bat"),
            Essence("giant_bat", "Giant Bat"),
            Essence("undead", "Undead"))
    ];

    public static string ItemBaseId(string areaKey) => $"item.essence_token.{areaKey}";

    private static SelectionContainerDefinition EssenceToken(
        string areaKey,
        string areaName,
        params SelectionContainerOptionDefinition[] options) =>
        new(ItemBaseId(areaKey), $"{areaName} - Essence Token", "Essence", options);

    private static SelectionContainerOptionDefinition Essence(string id, string name) =>
        new(id, $"{name} Essence", $"item.essence.{id}", 1);
}

public static class SelectionContainerCatalog
{
    private static readonly IReadOnlyDictionary<string, SelectionContainerDefinition> Definitions =
        new List<SelectionContainerDefinition>
        {
            new SelectionContainerDefinition(
                BlueprintSelectionBoxCatalog.ItemBaseId,
                "Blueprint Selection Box",
                "Blueprint",
                BlueprintSelectionBoxCatalog.Options)
        }
        .Concat(ShenicEssenceTokenCatalog.Definitions)
        .ToDictionary(definition => definition.ItemBaseId, StringComparer.OrdinalIgnoreCase);

    public static SelectionContainerDefinition? Find(string itemBaseId) =>
        Definitions.GetValueOrDefault(itemBaseId);

}
