using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Essences;

public sealed class JsonCreatureEssenceLootTableRepository : ICreatureEssenceLootTableRepository
{
    private readonly IReadOnlyList<CreatureEssenceLootTableDefinition> _tables;
    private readonly IReadOnlyDictionary<string, CreatureEssenceLootTableDefinition> _tablesByCreatureId;
    private readonly IReadOnlyDictionary<string, CreatureEssenceLootTableDefinition> _tablesByEssenceDefinitionId;

    public JsonCreatureEssenceLootTableRepository(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "world", "creature-essence-loot-tables.json");
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<CreatureEssenceLootTableDocument>(json, options) ?? new();

        _tables = document.Creatures
            .Select(x => new CreatureEssenceLootTableDefinition
            {
                CreatureId = x.Id,
                BaseDropChance = x.EssenceLootTable.BaseDropChance,
                PassiveAbilityId = x.EssenceLootTable.PassiveAbilityId,
                Variants = x.EssenceLootTable.Variants
            })
            .ToList();

        Validate(_tables, essenceDefinitions);
        _tablesByCreatureId = _tables.ToDictionary(x => x.CreatureId, StringComparer.OrdinalIgnoreCase);
        _tablesByEssenceDefinitionId = _tables
            .SelectMany(table => table.Variants.Select(variant => (variant.EssenceDefinitionId, Table: table)))
            .ToDictionary(x => x.EssenceDefinitionId, x => x.Table, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CreatureEssenceLootTableDefinition> GetAll() => _tables;

    public CreatureEssenceLootTableDefinition? GetByCreatureId(string creatureId) =>
        _tablesByCreatureId.GetValueOrDefault(creatureId);

    public CreatureEssenceLootTableDefinition? GetByEssenceDefinitionId(string essenceDefinitionId) =>
        _tablesByEssenceDefinitionId.GetValueOrDefault(essenceDefinitionId);

    private static void Validate(
        IReadOnlyList<CreatureEssenceLootTableDefinition> tables,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        var errors = new List<string>();
        var duplicateCreatureIds = tables
            .GroupBy(x => x.CreatureId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);
        errors.AddRange(duplicateCreatureIds.Select(id => $"Duplicate creature Essence loot table '{id}'."));

        var duplicateEssenceDefinitionIds = tables
            .SelectMany(x => x.Variants)
            .GroupBy(x => x.EssenceDefinitionId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);
        errors.AddRange(duplicateEssenceDefinitionIds.Select(id => $"Essence variant '{id}' belongs to more than one creature loot table."));

        foreach (var table in tables)
        {
            if (string.IsNullOrWhiteSpace(table.CreatureId))
                errors.Add("Creature Essence loot table id is required.");
            if (table.BaseDropChance is < 0 or > 1)
                errors.Add($"{table.CreatureId}: baseDropChance must be between 0 and 1.");
            if (string.IsNullOrWhiteSpace(table.PassiveAbilityId))
                errors.Add($"{table.CreatureId}: passiveAbilityId is required.");
            else if (essenceDefinitions.GetAbilityById(table.PassiveAbilityId) is not { Kind: Domain.Models.Combat.Abilities.AbilitySpecKind.Passive })
                errors.Add($"{table.CreatureId}: unknown or non-passive ability '{table.PassiveAbilityId}'.");
            if (table.Variants.Count == 0)
                errors.Add($"{table.CreatureId}: at least one active Essence variant is required.");

            var duplicateEssenceIds = table.Variants
                .GroupBy(x => x.EssenceDefinitionId, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);
            errors.AddRange(duplicateEssenceIds.Select(id => $"{table.CreatureId}: duplicate Essence variant '{id}'."));

            var duplicateActiveAbilityIds = table.Variants
                .GroupBy(x => x.ActiveAbilityId, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);
            errors.AddRange(duplicateActiveAbilityIds.Select(id => $"{table.CreatureId}: duplicate active ability variant '{id}'."));

            foreach (var variant in table.Variants)
            {
                if (variant.Weight <= 0)
                    errors.Add($"{table.CreatureId}/{variant.EssenceDefinitionId}: weight must be greater than zero.");

                var essence = essenceDefinitions.GetById(variant.EssenceDefinitionId);
                if (essence is null)
                {
                    errors.Add($"{table.CreatureId}: unknown Essence definition '{variant.EssenceDefinitionId}'.");
                    continue;
                }

                if (!essence.SourceMonsterId.Equals(table.CreatureId, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{table.CreatureId}/{variant.EssenceDefinitionId}: Essence sourceMonsterId must match its creature.");
                if (!essence.ActiveAbilityId.Equals(variant.ActiveAbilityId, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{table.CreatureId}/{variant.EssenceDefinitionId}: activeAbilityId does not match the Essence definition.");
                if (!essence.PassiveAbilityId.Equals(table.PassiveAbilityId, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{table.CreatureId}/{variant.EssenceDefinitionId}: passiveAbilityId does not match the creature's shared passive.");
                if (essenceDefinitions.GetAbilityById(variant.ActiveAbilityId) is not { Kind: Domain.Models.Combat.Abilities.AbilitySpecKind.Active })
                    errors.Add($"{table.CreatureId}/{variant.EssenceDefinitionId}: unknown or non-active ability '{variant.ActiveAbilityId}'.");
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Creature Essence loot table validation failed: " + string.Join(" | ", errors));
    }

    private sealed class CreatureEssenceLootTableDocument
    {
        public List<CreatureEssenceLootTableOwner> Creatures { get; set; } = [];
    }

    private sealed class CreatureEssenceLootTableOwner
    {
        public string Id { get; set; } = string.Empty;
        public CreatureEssenceLootTablePayload EssenceLootTable { get; set; } = new();
    }

    private sealed class CreatureEssenceLootTablePayload
    {
        public double BaseDropChance { get; set; }
        public string PassiveAbilityId { get; set; } = string.Empty;
        public List<CreatureEssenceVariantDefinition> Variants { get; set; } = [];
    }
}
