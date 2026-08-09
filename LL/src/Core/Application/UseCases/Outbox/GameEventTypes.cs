namespace Application.UseCases.Outbox;

public static class GameEventTypes
{
    public const string EquipmentChanged = "equipment.changed";
    public const string EssenceAbsorbed = "essence.absorbed";
    public const string EssenceLoadoutChanged = "essence.loadout_changed";
    public const string EssenceAscended = "essence.ascended";
    public const string EquipmentCrafted = "equipment.crafted";
    public const string EquipmentTempered = "equipment.tempered";
    public const string BlueprintUnlocked = "crafting.blueprint_unlocked";
    public const string IdleCombatEncounterCompleted = "combat.idle_encounter_completed";
    public const string CharacterCreated = "character.created";
    public const string CharacterLevelReached = "character.level_reached";
    public const string DungeonRunStarted = "dungeon.run_started";
    public const string DungeonRunCompleted = "dungeon.run_completed";
    public const string ColosseumBattleCompleted = "colosseum.battle_completed";
}
