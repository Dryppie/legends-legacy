namespace Domain.Models.Items.Equipments.Progression;

public sealed record EquipmentBlueprintDefinition(string StyleId, string Name, string ItemId);
public sealed record EquipmentBlueprintSource(
    string FamilyId, string Name, int Region, string SelectionItemId,
    IReadOnlyList<string> StyleIds);

public sealed class EquipmentBlueprintCatalog
{
    public int Version { get; init; } = 1;
    public long CindersPerTier { get; init; } = 100;
    public double DropChance { get; init; } = 0.25;
    public int GuaranteeCompletions { get; init; } = 4;
    public double AreaVariantChance { get; init; } = 0.15;
    public double DungeonVariantChance { get; init; } = 0.50;
    public IReadOnlyList<EquipmentBlueprintDefinition> Blueprints { get; init; } = [];
    public IReadOnlyList<EquipmentBlueprintSource> Sources { get; init; } = [];

    public EquipmentBlueprintDefinition? Find(string? styleId) =>
        Blueprints.SingleOrDefault(x => x.StyleId == styleId);

    public EquipmentBlueprintSource? FindSource(string sigilItemId) =>
        Sources.SingleOrDefault(x => sigilItemId == $"sigil_{x.FamilyId}");

    public void Validate(StarterEquipmentCatalog equipment)
    {
        if (Version < 1 || CindersPerTier < 0 || GuaranteeCompletions < 1
            || new[] { DropChance, AreaVariantChance, DungeonVariantChance }
                .Any(x => !double.IsFinite(x) || x is < 0 or > 1)
            || Blueprints.Count == 0 || Sources.Count == 0
            || Blueprints.Select(x => x.StyleId).Distinct().Count() != Blueprints.Count
            || Blueprints.Select(x => x.ItemId).Distinct().Count() != Blueprints.Count
            || Sources.Select(x => x.FamilyId).Distinct().Count() != Sources.Count
            || Sources.Select(x => x.SelectionItemId).Distinct().Count() != Sources.Count)
            throw new InvalidOperationException("Invalid equipment blueprint catalog.");
        foreach (var blueprint in Blueprints)
        {
            EquipmentValidation.Id(blueprint.ItemId);
            EquipmentValidation.Id(blueprint.Name);
            if (!equipment.Styles.Any(x => x.Id == blueprint.StyleId)
                || !Sources.Any(x => x.StyleIds.Contains(blueprint.StyleId)))
                throw new InvalidOperationException($"Blueprint '{blueprint.StyleId}' has no style or acquisition source.");
        }
        foreach (var source in Sources)
        {
            EquipmentValidation.Id(source.FamilyId);
            EquipmentValidation.Id(source.Name);
            EquipmentValidation.Id(source.SelectionItemId);
            if (source.Region < 1 || source.StyleIds.Count == 0
                || source.StyleIds.Distinct().Count() != source.StyleIds.Count
                || source.StyleIds.Any(x => Find(x) is null))
                throw new InvalidOperationException($"Invalid blueprint source '{source.FamilyId}'.");
        }
    }

    public EquipmentState RollVariant(
        EquipmentState state, StarterEquipmentCatalog equipment,
        IReadOnlyCollection<string> styleIds, double chance, Random random)
    {
        if (random.NextDouble() >= chance) return state;
        var compatible = equipment.Styles.Where(x => styleIds.Contains(x.Id)
            && x.CompatibleArchetypeIds.Contains(state.ArchetypeId)).OrderBy(x => x.Id).ToArray();
        if (compatible.Length == 0) return state;
        var styleId = compatible[random.Next(compatible.Length)].Id;
        var variant = state.ApplyVariant(equipment.Evaluator, styleId);
        return EquipmentState.Restore(variant.ToSnapshot() with { NativeStyleId = styleId });
    }
}

public sealed class EquipmentBlueprintProgress
{
    public Guid CharacterId { get; set; }
    public string FamilyId { get; set; } = string.Empty;
    public int Misses { get; set; }
    public Guid? LastRunId { get; set; }

    public bool Complete(Guid runId, double roll, EquipmentBlueprintCatalog catalog)
    {
        if (runId == Guid.Empty || !double.IsFinite(roll) || roll is < 0 or >= 1)
            throw new ArgumentException("Invalid blueprint completion.");
        if (LastRunId == runId) return false;
        LastRunId = runId;
        var awarded = roll < catalog.DropChance || Misses + 1 >= catalog.GuaranteeCompletions;
        Misses = awarded ? 0 : Misses + 1;
        return awarded;
    }
}

public interface IEquipmentBlueprintRepository
{
    Task<EquipmentBlueprintProgress> LoadForCompletionAsync(Guid characterId, string familyId, CancellationToken ct);
    Task<IReadOnlyList<EquipmentBlueprintProgress>> GetProgressAsync(Guid characterId, CancellationToken ct);
}
