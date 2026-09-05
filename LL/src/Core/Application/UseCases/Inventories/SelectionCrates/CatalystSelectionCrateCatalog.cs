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

public static class LegacyBlueprintSelectionBoxCatalog
{
    public const string ItemBaseId = "item.blueprint_selection_box";

    public static SelectionContainerDefinition Definition { get; } = new(
        ItemBaseId,
        "Blueprint Selection Box",
        "Blueprint",
        [
            Blueprint("fury", "Fury"),
            Blueprint("arcane", "Arcane"),
            Blueprint("execution", "Execution"),
            Blueprint("aegis", "Aegis"),
            Blueprint("warden", "Warden"),
            Blueprint("endurance", "Endurance"),
            Blueprint("phoenix", "Phoenix"),
            Blueprint("spirit", "Spirit"),
            Blueprint("primal", "Primal"),
            Blueprint("venom", "Venom"),
            Blueprint("hive", "Hive")
        ]);

    private static SelectionContainerOptionDefinition Blueprint(string id, string name) =>
        new(id, $"Blueprint: {name}", $"item.blueprint_{id}", 1);
}

public static class TutorialArmsChestCatalog
{
    public const string ItemBaseId = "item.arms_chest";

    public static SelectionContainerDefinition Definition { get; } = new(
        ItemBaseId,
        "Arms Chest",
        "Weapon",
        [
            Weapon("shortsword", "Shortsword"),
            Weapon("dagger", "Dagger"),
            Weapon("hatchet", "Hatchet"),
            Weapon("mace", "Mace"),
            Weapon("wand", "Wand")
        ]);

    private static SelectionContainerOptionDefinition Weapon(string id, string name) =>
        new($"plain.{id}", name, id, 1);
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
        ShenicEssenceTokenCatalog.Definitions
            .Append(TutorialArmsChestCatalog.Definition)
            .Append(LegacyBlueprintSelectionBoxCatalog.Definition)
            .ToDictionary(definition => definition.ItemBaseId, StringComparer.OrdinalIgnoreCase);

    public static SelectionContainerDefinition? Find(string itemBaseId,
        Domain.Models.Items.Equipments.Progression.EquipmentBlueprintCatalog? blueprints = null)
    {
        if (Definitions.TryGetValue(itemBaseId, out var definition)) return definition;
        var source = blueprints?.Sources.SingleOrDefault(x => x.SelectionItemId == itemBaseId);
        return source is null ? null : new(source.SelectionItemId, $"{source.Name} Blueprint Choice", "Blueprint",
            source.StyleIds.Select(id => blueprints!.Find(id)!).Select(x =>
                new SelectionContainerOptionDefinition(x.StyleId, $"Blueprint: {x.Name}", x.ItemId, 1)).ToArray());
    }

}
