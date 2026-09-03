using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;

namespace Domain.Models.Administration;

public sealed record CompensationEquipmentOption(string DefinitionId, string Name, string ItemBaseId,
    string ArchetypeId, int MinimumTier, int MaximumTier, string? NativeStyleId,
    IReadOnlyList<string> CompatibleStyleIds);

public sealed record CompensationEquipmentOptions(bool UsesEquipmentProgression, int MaximumQuantity,
    IReadOnlyList<CompensationEquipmentOption> Options);

public sealed record CompensationGrantPlan(ItemBase ItemBase, bool UsesEquipmentProgression, EquipmentData? Equipment);
