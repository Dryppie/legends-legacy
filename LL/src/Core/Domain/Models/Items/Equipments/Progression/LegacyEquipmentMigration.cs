using System.Security.Cryptography;
using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Items.Equipments.Progression;

public sealed record LegacyEquipmentModifier(Guid Id, AttributeType Attribute, float Amount,
    ModifierType ModifierType, float RarityBonusAmount = 0);

/// <summary>Receipt-only snapshot of equipment that predates progression descriptors.</summary>
public sealed record LegacyEquipmentSnapshot(Guid Id, string ItemBaseId, string DisplayName,
    EquipmentType EquipmentType, Rarity Rarity, ItemQuality Quality, int Tier, bool BaseIsBound,
    EquipmentOwnership Ownership, DateTimeOffset AcquiredAtUtc, string AcquisitionSource,
    bool IsFavorite, IReadOnlyList<string> AffinityTags,
    IReadOnlyList<LegacyEquipmentModifier> BaseModifiers, IReadOnlyList<LegacyEquipmentModifier> InstanceModifiers)
{
    public string Hash() => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this with
    {
        BaseModifiers = BaseModifiers.OrderBy(x => x.Id).ToArray(),
        InstanceModifiers = InstanceModifiers.OrderBy(x => x.Id).ToArray()
    })));

    public EquipmentMigrationPreview Preview(EquipmentMigrationTarget target, EquipmentEvaluator evaluator,
        EquipmentBalance sourceBalance, string? definitionId)
    {
        if (target.ItemId != Id || target.Location != EquipmentMigrationLocation.Instance
            || evaluator.Balance.Version <= 1 || evaluator.Balance.AttributeVersion != AttributeRules.CurrentVersion)
            throw new InvalidOperationException("Legacy imports require an instance and a modern equipment release.");
        var before = Describe(evaluator);
        EquipmentMigrationPreview proposal;
        if (before is not null) proposal = EquipmentMigrationPolicy.Preview(target, before, evaluator, definitionId, sourceBalance);
        else
        {
            var archetypeId = LegacyEquipmentMigration.ResolveArchetype("plain." + ItemBaseId);
            var choices = evaluator.Definitions.Where(x => x.ArchetypeId == archetypeId
                && x.Rarity == (EquipmentRarity)Rarity && x.NativeStyleId is null).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            var definition = (definitionId is null ? choices.SingleOrDefault(x => x.SpecializationId == "default")
                : choices.SingleOrDefault(x => x.Id == definitionId))
                ?? throw new InvalidOperationException("No compatible authored default exists for this empty legacy item.");
            var evaluated = EquipmentData.Create(EquipmentState.Restore(State() with
            {
                BalanceVersion = evaluator.Balance.Version, DefinitionId = definition.Id, ArchetypeId = archetypeId
            }), evaluator);
            var after = new EquipmentData(evaluated.State, ItemBaseId, DisplayName, evaluated.Rarity, EquipmentType,
                evaluated.Behavior, evaluated.Stats, evaluated.EquipmentSetId, evaluated.BaseStats, evaluated.Allocation);
            proposal = new(target, Hash(), null, after, 0, after.Allocation!.Total, choices.Select(x => x.Id).ToArray(), "", 1);
        }
        return proposal with
        {
            SourceHash = Hash(), LegacyBefore = this,
            MappingReason = "Imported unversioned equipment with verified ownership; preserved item ID, base, name, tier, rarity, quality, binding and acquisition metadata. "
                + (before is null ? "No positive attributes were recorded; selected the authored default profile unless explicitly chosen. "
                    : "Legacy affixes select the nearest legal profile. ")
                + "No reinforcement rank or shared roll existed: initialized rank 0 and roll 1.00. Exact original modifiers and rarity bonuses are retained in the rollback receipt."
        };
    }

    private EquipmentData? Describe(EquipmentEvaluator evaluator)
    {
        var archetypeId = LegacyEquipmentMigration.ResolveArchetype("plain." + ItemBaseId);
        if (!evaluator.Definitions.Any(x => x.ArchetypeId == archetypeId))
            throw new InvalidOperationException("No authored legacy equipment mapping exists for this item base.");
        var archetype = evaluator.GetArchetype(archetypeId);
        if (archetype.EquipmentType != EquipmentType)
            throw new InvalidOperationException("The legacy item mapping changes its equipment slot.");
        if (BaseModifiers.Concat(InstanceModifiers).Any(x => x.ModifierType != ModifierType.Flat
            || !EquipmentStatBudgetCatalog.IsKnown(x.Attribute)
            || !float.IsFinite(x.Amount) || x.Amount < 0 || !float.IsFinite(x.RarityBonusAmount)))
            throw new InvalidOperationException("Legacy equipment has unsupported modifiers; manual mapping is required.");
        var stats = BaseModifiers.Select(x => new KeyValuePair<AttributeType, float>(x.Attribute,
                EquipmentInstance.GetBoostedBaseModifierAmount(x.Attribute, x.Amount, Rarity)))
            .Concat(InstanceModifiers.Select(x => new KeyValuePair<AttributeType, float>(x.Attribute, x.Amount)))
            .GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Sum(v => v.Value));
        if (!stats.Values.Any(x => x > 0)) return null;
        return new(State(), ItemBaseId, DisplayName, (EquipmentRarity)Rarity, EquipmentType,
            archetype.Behavior, stats, null);
    }

    // The old schema records rarity/quality/tier and affix amounts, but no reinforcement
    // rank or common roll. Never infer either from unrelated per-attribute rolls.
    private EquipmentStateSnapshot State() => new(EquipmentBalance.ModelVersion, Id, "plain." + ItemBaseId,
        "plain." + ItemBaseId, Tier, 0, 1, null, null,
        new(EquipmentAwardKind.LegacyImport, AcquisitionSource, Id.ToString("N")), Ownership, Quality);
}

public static class LegacyEquipmentMigration
{
    // Explicit retired identities, never a slot-based guess. Existing item-base IDs and
    // names remain frozen; these aliases select the authored successor's stat profiles.
    private static readonly IReadOnlyDictionary<string, string> Archetypes = new Dictionary<string, string>
    {
        ["plain.cloth_cowl"] = "plain.light_hood",
        ["plain.cloth_pants"] = "plain.light_leggings"
    };

    public static string ResolveArchetype(string id) => Archetypes.GetValueOrDefault(id, id);

    public static string SerializeBefore(LegacyEquipmentSnapshot snapshot) =>
        JsonSerializer.Serialize(new LegacyReceipt(1, snapshot));

    public static LegacyEquipmentSnapshot? ReadBefore(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty(nameof(LegacyReceipt.LegacyImportVersion), out var version)) return null;
        if (version.GetInt32() != 1) throw new InvalidOperationException("Unsupported legacy equipment receipt version.");
        return JsonSerializer.Deserialize<LegacyReceipt>(json)?.LegacyEquipment
            ?? throw new InvalidOperationException("Missing legacy equipment receipt snapshot.");
    }

    private sealed record LegacyReceipt(int LegacyImportVersion, LegacyEquipmentSnapshot LegacyEquipment);
}
