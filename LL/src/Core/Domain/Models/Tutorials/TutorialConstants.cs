namespace Domain.Models.Tutorials;

public static class TutorialConstants
{
    public const string FirstStepsTutorialId = "tutorial.first_steps";

    public const string StepDefeatTrainingCreature = "defeat_training_creature";
    public const string StepAbsorbEssence = "absorb_essence";
    public const string StepEquipEssence = "equip_essence";
    public const string StepCraftEquipment = "craft_equipment";
    public const string StepEquipEquipment = "equip_equipment";
    public const string StepStartLumoRuins = "start_lumo_ruins";
    public const string StepDefeatLumoRuins = "defeat_lumo_ruins";
    public const string StepComplete = "complete";

    public const string TrainingGroundsAreaId = "tutorial_area_training_grounds";
    public const string LumoRuinsAreaId = "region_01_area_01";
    public const string TutorialEssenceDefinitionId = "essence.legacy.goblin";
    public const string TutorialEssenceItemBaseId = "item.essence.legacy.goblin";
    public const string TutorialSwordItemBaseId = "tutorial_sword";
    public const string TutorialRingItemBaseId = "tutorial_ring";
    public const string TutorialCraftingOreItemBaseId = "ore";
    public const string TutorialCraftingWoodItemBaseId = "wood";

    public static readonly IReadOnlyList<string> TutorialOneHandedWeaponItemBaseIds =
    [
        "shortsword",
        "dagger",
        "hatchet",
        "mace",
        "wand"
    ];

    public const int TutorialCraftingOreQuantity = 10;
    public const int TutorialCraftingWoodQuantity = 3;
    public const int RequiredCraftedEquipmentCount = 1;
    public const int RequiredEquippedEquipmentCount = 1;
    public const int CompletionCinders = 150;
}
