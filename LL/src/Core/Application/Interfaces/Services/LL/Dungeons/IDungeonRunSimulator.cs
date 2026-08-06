namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRunSimulator
{
    DungeonSimulationOptions GetOptions();
    Task<DungeonSimulationReport> RunAsync(
        DungeonSimulationRequest request,
        CancellationToken cancellationToken);
}

public sealed record DungeonSimulationOptions(
    IReadOnlyList<DungeonSimulationDungeonOption> Dungeons,
    IReadOnlyList<DungeonSimulationEssenceOption> Essences,
    IReadOnlyList<DungeonSimulationEquipmentSlotOption> EquipmentSlots,
    IReadOnlyList<DungeonSimulationEquipmentRarityOption> EquipmentRarities);

public sealed record DungeonSimulationDungeonOption(
    string Id,
    string FamilyId,
    string Name,
    string Difficulty,
    int Tier);

public sealed record DungeonSimulationEssenceOption(string Id, string Name);

public sealed record DungeonSimulationEquipmentSlotOption(
    string Id,
    string Name,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, float>> AttributeBonusesByRarity);

public sealed record DungeonSimulationEquipmentRarityOption(
    string Id,
    string Name,
    int TemperingSteps);

public sealed record DungeonSimulationRequest(
    string DungeonDefinitionId,
    int RunCount,
    int RandomSeed,
    int MasteryLevel,
    string RouteStrategy,
    DungeonSimulationCharacter Character);

public sealed record DungeonSimulationCharacter(
    string Name,
    int Level,
    float MaxHealth,
    float Power,
    float Armor,
    float Resistance,
    float CritChance,
    float CritDamage,
    float AttackSpeed,
    float HealthRegeneration,
    IReadOnlyList<string> EssenceIds,
    DungeonSimulationEquipment? Equipment);

public sealed record DungeonSimulationEquipment(
    string Rarity,
    IReadOnlyList<string> EquippedSlots);

public sealed record DungeonSimulationReport(
    string DungeonDefinitionId,
    string DungeonName,
    string Difficulty,
    int Tier,
    int RequestedRuns,
    int CompletedRuns,
    int FailedRuns,
    double ClearRate,
    double AverageFinalVigor,
    double AverageRoomsCleared,
    int RandomSeed,
    string RouteStrategy,
    IReadOnlyList<DungeonSimulationRunResult> Runs);

public sealed record DungeonSimulationRunResult(
    int RunNumber,
    int Seed,
    bool Completed,
    string Outcome,
    int FinalVigor,
    int RoomsCleared,
    int TotalCombatTicks,
    IReadOnlyList<DungeonSimulationRoomResult> Rooms);

public sealed record DungeonSimulationRoomResult(
    int RoomIndex,
    string Name,
    string RoomType,
    string Outcome,
    int VigorBefore,
    int VigorAfter,
    int VigorChange,
    int CombatTicks,
    int DamageTaken,
    IReadOnlyList<string> Enemies);
