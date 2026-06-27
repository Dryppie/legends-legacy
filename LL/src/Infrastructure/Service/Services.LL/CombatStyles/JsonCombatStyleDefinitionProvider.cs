using System.Text.Json;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;
using Microsoft.Extensions.Configuration;

namespace Services.LL.CombatStyles;

public sealed class JsonCombatStyleDefinitionProvider : ICombatStyleDefinitionProvider
{
    private readonly IReadOnlyList<CombatStyleDefinition> _definitions;

    public JsonCombatStyleDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "combat-styles.json");
        var document = JsonSerializer.Deserialize<CombatStyleDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        _definitions = [.. document.Styles.Select(ToDomain)];
        ThrowIfInvalid(_definitions);
    }

    public IReadOnlyCollection<CombatStyleDefinition> GetAll() => _definitions;

    public CombatStyleDefinition? GetById(string styleId) =>
        _definitions.FirstOrDefault(x => x.Id.Equals(styleId, StringComparison.OrdinalIgnoreCase));

    public CombatStyleFocusDefinition? GetFocus(string styleId, string focusId) =>
        GetById(styleId)?.Focuses.FirstOrDefault(x => x.Id.Equals(focusId, StringComparison.OrdinalIgnoreCase));

    private static CombatStyleDefinition ToDomain(CombatStyleDefinitionJson style) =>
        new(
            style.Id,
            style.Name,
            style.Description,
            style.ResourceId,
            style.MaxLevel,
            style.RecommendedTags,
            style.RecommendedStats,
            [.. style.Focuses.Select(focus => ToDomain(style.Id, focus))],
            [.. style.Rules.Select(ToDomain)],
            style.CoreMechanic);

    private static CombatStyleFocusDefinition ToDomain(string styleId, CombatStyleFocusDefinitionJson focus) =>
        new(
            focus.Id,
            styleId,
            focus.Name,
            focus.Description,
            focus.UnlockLevel,
            focus.RecommendedTags,
            focus.RecommendedStats,
            [.. focus.Rules.Select(ToDomain)]);

    private static CombatStyleRuleDefinition ToDomain(CombatStyleRuleDefinitionJson rule) =>
        new()
        {
            Id = rule.Id,
            MinStyleLevel = rule.MinStyleLevel,
            EventType = rule.EventType,
            Predicate = rule.Predicate,
            Operation = ToDomain(rule.Operation),
            MaxTriggersPerEncounter = rule.MaxTriggersPerEncounter,
            MaxTriggersPerSource = rule.MaxTriggersPerSource,
            MaxTriggersPerTarget = rule.MaxTriggersPerTarget
        };

    private static StyleRuleOperation ToDomain(CombatStyleRuleOperationJson operation) =>
        operation.Type switch
        {
            "modifyEffectAmount" => new ModifyEffectAmountOperation(
                operation.AdditivePercent,
                operation.UsesProcCoefficient),
            "addDamageReduction" => new AddDamageReductionOperation(
                operation.Percent,
                operation.UsesProcCoefficient),
            "gainStyleResource" => new GainStyleResourceOperation(
                operation.ResourceId ?? string.Empty,
                operation.Amount,
                operation.UsesProcCoefficient),
            "addBonusDamageFromStat" => new AddBonusDamageFromStatOperation(
                operation.Stat,
                operation.Coefficient,
                operation.DamageType,
                operation.UsesProcCoefficient),
            _ => new NoOpStyleRuleOperation()
        };

    private static void ThrowIfInvalid(IReadOnlyList<CombatStyleDefinition> definitions)
    {
        var duplicateStyles = definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateStyles.Count > 0)
            throw new InvalidOperationException("Duplicate combat style ids: " + string.Join(", ", duplicateStyles));

        var invalidStyles = definitions
            .Where(x => string.IsNullOrWhiteSpace(x.Id)
                || string.IsNullOrWhiteSpace(x.Name)
                || string.IsNullOrWhiteSpace(x.ResourceId)
                || x.MaxLevel <= 0)
            .Select(x => string.IsNullOrWhiteSpace(x.Id) ? "<missing>" : x.Id)
            .ToList();
        if (invalidStyles.Count > 0)
            throw new InvalidOperationException("Combat styles require id, name, resourceId, and MaxLevel greater than zero: " + string.Join(", ", invalidStyles));

        foreach (var style in definitions)
        {
            var duplicateFoci = style.Focuses
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            if (duplicateFoci.Count > 0)
                throw new InvalidOperationException($"Combat style '{style.Id}' has duplicate focus ids: {string.Join(", ", duplicateFoci)}");

            var invalidFoci = style.Focuses
                .Where(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name))
                .ToList();
            if (invalidFoci.Count > 0)
                throw new InvalidOperationException($"Combat style '{style.Id}' has focus definitions without ids or names.");
        }
    }

    private sealed class CombatStyleDefinitionDocument
    {
        public List<CombatStyleDefinitionJson> Styles { get; set; } = [];
    }

    private sealed class CombatStyleDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int MaxLevel { get; set; } = 50;
        public IReadOnlyList<string> RecommendedTags { get; set; } = [];
        public IReadOnlyList<AttributeType> RecommendedStats { get; set; } = [];
        public IReadOnlyList<CombatStyleFocusDefinitionJson> Focuses { get; set; } = [];
        public IReadOnlyList<CombatStyleRuleDefinitionJson> Rules { get; set; } = [];
        public string CoreMechanic { get; set; } = string.Empty;
    }

    private sealed class CombatStyleFocusDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UnlockLevel { get; set; } = 10;
        public IReadOnlyList<string> RecommendedTags { get; set; } = [];
        public IReadOnlyList<AttributeType> RecommendedStats { get; set; } = [];
        public IReadOnlyList<CombatStyleRuleDefinitionJson> Rules { get; set; } = [];
    }

    private sealed class CombatStyleRuleDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public int MinStyleLevel { get; set; } = 1;
        public CombatStyleEventType EventType { get; set; }
        public EffectPredicate Predicate { get; set; } = new();
        public CombatStyleRuleOperationJson Operation { get; set; } = new();
        public int? MaxTriggersPerEncounter { get; set; }
        public int? MaxTriggersPerSource { get; set; }
        public int? MaxTriggersPerTarget { get; set; }
    }

    private sealed class CombatStyleRuleOperationJson
    {
        public string Type { get; set; } = string.Empty;
        public decimal AdditivePercent { get; set; }
        public decimal Percent { get; set; }
        public string? ResourceId { get; set; }
        public decimal Amount { get; set; }
        public AttributeType Stat { get; set; }
        public decimal Coefficient { get; set; }
        public DamageType DamageType { get; set; }
        public bool UsesProcCoefficient { get; set; } = true;
    }
}
