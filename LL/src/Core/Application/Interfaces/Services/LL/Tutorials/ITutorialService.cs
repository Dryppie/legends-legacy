using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Tutorials;

public interface ITutorialService
{
    Task<TutorialState?> GetStateAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> CanStartCombatAreaAsync(Guid characterId, string areaId, CancellationToken cancellationToken);
}

public interface ITutorialProgressionService
{
    Task<TutorialProgressResult?> TryProgressAsync(
        Guid characterId,
        TutorialTrigger trigger,
        CancellationToken cancellationToken);
}

public sealed record TutorialProgressResult(
    TutorialState? State,
    IReadOnlyList<InventoryItem> Loot,
    bool Progressed);

public sealed record TutorialState(
    string TutorialId,
    string Title,
    int Version,
    string CurrentStep,
    string Objective,
    int CurrentAmount,
    int RequiredAmount,
    TutorialStepPresentation Presentation,
    string ActionLabel,
    string DestinationRoute,
    string? GuidePageId,
    string? TourPageId,
    bool IsCompleted);

public sealed record TutorialStepPresentation(
    string ActionLabel,
    string DestinationRoute,
    string? GuidePageId,
    string? TourPageId);

public sealed record TutorialTrigger(
    string Type,
    string? StepKey = null,
    string? AreaId = null,
    bool? WonEncounter = null,
    string? EssenceDefinitionId = null,
    IReadOnlyCollection<Guid>? AttunedPlayerEssenceIds = null,
    IReadOnlyCollection<string>? CraftedItemBaseIds = null,
    IReadOnlyCollection<int>? CraftedItemTiers = null,
    string? Route = null)
{
    public static TutorialTrigger IdleCombatCompleted(string areaId, bool wonEncounter) =>
        new("IdleCombatCompleted", AreaId: areaId, WonEncounter: wonEncounter);

    public static TutorialTrigger EssenceAbsorbed(string essenceDefinitionId) =>
        new("EssenceAbsorbed", EssenceDefinitionId: essenceDefinitionId);

    public static TutorialTrigger EssenceLoadoutChanged(IReadOnlyCollection<Guid> attunedPlayerEssenceIds) =>
        new("EssenceLoadoutChanged", AttunedPlayerEssenceIds: attunedPlayerEssenceIds);

    public static TutorialTrigger CraftedEquipment(
        IReadOnlyCollection<string> itemBaseIds,
        IReadOnlyCollection<int> itemTiers) =>
        new("CraftedEquipment", CraftedItemBaseIds: itemBaseIds, CraftedItemTiers: itemTiers);

    public static TutorialTrigger EquipmentChanged() =>
        new("EquipmentChanged");

    public static TutorialTrigger ClientStep(string stepKey, string triggerType, string? route = null) =>
        new(triggerType, StepKey: stepKey, Route: route);
}
