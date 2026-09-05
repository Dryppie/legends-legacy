namespace Domain.Models.Items.Equipments.Progression;

public sealed record EquipmentBlueprintOption(string StyleId, string Name, string ItemId,
    long Held, bool IsCurrent, IReadOnlyList<EquipmentBlueprintSourceProgress> Sources);
public sealed record EquipmentBlueprintSourceProgress(string Name, int Region, int CompletionsUntilGuaranteed);
