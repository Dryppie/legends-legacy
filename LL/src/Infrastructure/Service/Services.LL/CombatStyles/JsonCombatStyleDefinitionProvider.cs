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
        _definitions = [.. LoadDefinitions(contentRootPath, contentRoot, options).Select(ToDomain)];
        ThrowIfInvalid(_definitions);
    }

    public IReadOnlyCollection<CombatStyleDefinition> GetAll() => _definitions;

    public CombatStyleDefinition? GetById(string styleId) =>
        _definitions.FirstOrDefault(x => x.Id.Equals(styleId, StringComparison.OrdinalIgnoreCase));

    public CombatStyleFocusDefinition? GetFocus(string styleId, string focusId) =>
        GetById(styleId)?.Focuses.FirstOrDefault(x => x.Id.Equals(focusId, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<CombatStyleDefinitionJson> LoadDefinitions(
        string contentRootPath,
        string contentRoot,
        JsonSerializerOptions options)
    {
        var folderPath = Path.Combine(contentRootPath, contentRoot, "combat-styles");
        if (Directory.Exists(folderPath))
        {
            var files = Directory
                .EnumerateFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    yield return JsonSerializer.Deserialize<CombatStyleDefinitionJson>(
                        File.ReadAllText(file),
                        options) ?? new();
                }

                yield break;
            }
        }

        throw new DirectoryNotFoundException($"Could not locate combat style definition files under '{folderPath}'.");
    }

    private static CombatStyleDefinition ToDomain(CombatStyleDefinitionJson style)
    {
        var focuses = style.Focuses.Select(focus => ToDomain(style.Id, focus)).ToList();
        var skillTreeNodes = style.SkillTreeNodes.Select(ToDomain).ToList();

        return new(
            style.Id,
            style.Name,
            style.Description,
            style.ResourceId,
            style.ResourceMaxAmount,
            style.MaxLevel,
            focuses,
            skillTreeNodes,
            [.. style.Rules.Select(ToDomain)],
            [.. style.ResourceOverflowOperations.Select(ToDomain)],
            style.CoreMechanic);
    }

    private static CombatStyleTreeNodeDefinition ToDomain(CombatStyleTreeNodeDefinitionJson node) =>
        new(
            node.Id,
            node.BranchId,
            node.Name,
            node.Description,
            node.MaxRank,
            node.RequiredLevel,
            node.RequiredNodeId,
            node.X,
            node.Y,
            node.Tags,
            node.CountsTowardFocus)
        {
            Rules = [.. node.Rules.Select(ToDomain)],
            Row = node.Row,
            Lane = string.IsNullOrWhiteSpace(node.Lane) ? node.BranchId : node.Lane,
            NodeType = string.IsNullOrWhiteSpace(node.NodeType) ? CombatStyleNodeTypes.Minor : node.NodeType,
            MutatorKind = node.MutatorKind,
            MutatorGroups = node.MutatorGroups,
            Mutator = node.Mutator,
            Tooltip = node.Tooltip
        };

    private static CombatStyleFocusDefinition ToDomain(string styleId, CombatStyleFocusDefinitionJson focus) =>
        new(
            focus.Id,
            styleId,
            focus.Name,
            focus.Description,
            focus.UnlockLevel,
            [.. focus.Rules.Select(ToDomain)]);

    private static CombatStyleRuleDefinition ToDomain(CombatStyleRuleDefinitionJson rule) =>
        new()
        {
            Id = rule.Id,
            MinStyleLevel = rule.MinStyleLevel,
            MaxStyleLevel = rule.MaxStyleLevel,
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
                operation.UsesProcCoefficient ?? false,
                [.. operation.AdditivePercentModifiers.Select(ToDomain)]),
            "addDamageReduction" => new AddDamageReductionOperation(
                operation.Percent,
                operation.UsesProcCoefficient ?? false,
                [.. operation.PercentModifiers.Select(ToDomain)]),
            "gainStyleResource" => new GainStyleResourceOperation(
                operation.ResourceId ?? string.Empty,
                operation.Amount,
                operation.UsesProcCoefficient ?? true,
                [.. operation.AmountModifiers.Select(ToDomain)]),
            "addBonusDamageFromStat" => new AddBonusDamageFromStatOperation(
                operation.Stat,
                operation.Coefficient,
                operation.DamageType,
                operation.UsesProcCoefficient ?? true,
                [.. operation.CoefficientModifiers.Select(ToDomain)]),
            "modifySummonStats" => new ModifySummonStatsOperation(
                operation.MaxHealthPercent,
                operation.DamagePercent,
                operation.DamageReductionInheritancePercent,
                operation.MagicPowerInheritancePercent,
                operation.MaxInheritedDamageReductionPercent,
                [.. operation.MaxHealthPercentModifiers.Select(ToDomain)],
                [.. operation.DamagePercentModifiers.Select(ToDomain)]),
            "setPendingEmpowerment" => new SetPendingEmpowermentOperation(
                operation.EmpowermentId ?? string.Empty,
                operation.AppliesTo,
                operation.AdditivePercent,
                operation.ConsumeOnUse ?? true,
                [.. operation.AdditivePercentModifiers.Select(ToDomain)]),
            "grantBarrierFromMaxHealth" => new GrantBarrierFromMaxHealthOperation(
                operation.TriggerKey ?? string.Empty,
                operation.Percent,
                operation.MaxTriggersPerEncounter,
                [.. operation.PercentModifiers.Select(ToDomain)],
                [.. operation.MaxTriggerModifiers.Select(ToDomain)]),
            "triggerProtectiveShell" => new TriggerProtectiveShellOperation(),
            _ => new NoOpStyleRuleOperation()
        };

    private static StyleValueModifier ToDomain(StyleValueModifierJson modifier) =>
        new(
            modifier.Type,
            modifier.Value,
            modifier.NodeId,
            modifier.FocusId,
            modifier.MinStyleLevel,
            modifier.MaxStyleLevel);

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
            if (style.SkillTreeNodes.Count == 0)
                throw new InvalidOperationException($"Combat style '{style.Id}' must define skill tree nodes.");

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

            var duplicateNodes = style.SkillTreeNodes
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            if (duplicateNodes.Count > 0)
                throw new InvalidOperationException($"Combat style '{style.Id}' has duplicate skill tree node ids: {string.Join(", ", duplicateNodes)}");

            var missingRequiredNodes = style.SkillTreeNodes
                .Where(x => x.RequiredNodeId is not null
                    && !style.SkillTreeNodes.Any(candidate => candidate.Id.Equals(x.RequiredNodeId, StringComparison.OrdinalIgnoreCase)))
                .Select(x => $"{x.Id}->{x.RequiredNodeId}")
                .ToList();
            if (missingRequiredNodes.Count > 0)
                throw new InvalidOperationException($"Combat style '{style.Id}' has skill tree nodes with missing required nodes: {string.Join(", ", missingRequiredNodes)}");

            if (style.SkillTreeNodes.Any(x => x.Row > 0))
                ThrowIfInvalidRedesignedTree(style);
        }
    }

    private static void ThrowIfInvalidRedesignedTree(CombatStyleDefinition style)
    {
        var validLanes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            CombatStyleNodeLanes.Left,
            CombatStyleNodeLanes.Middle,
            CombatStyleNodeLanes.Right
        };

        var invalidLanes = style.SkillTreeNodes
            .Where(x => !validLanes.Contains(x.Lane))
            .Select(x => $"{x.Id}:{x.Lane}")
            .ToList();
        if (invalidLanes.Count > 0)
            throw new InvalidOperationException($"Combat style '{style.Id}' has invalid node lanes: {string.Join(", ", invalidLanes)}");

        var majorNodes = style.SkillTreeNodes
            .Where(x => IsNodeType(x, CombatStyleNodeTypes.Major))
            .ToList();
        var minorNodes = style.SkillTreeNodes
            .Where(x => IsNodeType(x, CombatStyleNodeTypes.Minor))
            .ToList();

        if (majorNodes.Count != 9 || minorNodes.Count != 12)
            throw new InvalidOperationException($"Combat style '{style.Id}' redesigned trees require 9 major nodes and 12 minor nodes.");

        foreach (var row in Enumerable.Range(1, 3))
        {
            var rowMajorNodes = majorNodes.Where(x => x.Row == row).ToList();
            if (rowMajorNodes.Count != 3
                || !validLanes.All(lane => rowMajorNodes.Any(x => x.Lane.Equals(lane, StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException($"Combat style '{style.Id}' must define one major node per lane in row {row}.");
            }
        }

        var missingMutators = majorNodes
            .Where(x => x.Row == 2 && x.Mutator is null)
            .Select(x => x.Id)
            .ToList();
        if (missingMutators.Count > 0)
            throw new InvalidOperationException($"Combat style '{style.Id}' row 2 major nodes require mutators: {string.Join(", ", missingMutators)}");
    }

    private static bool IsNodeType(CombatStyleTreeNodeDefinition node, string nodeType) =>
        node.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase);

    private sealed class CombatStyleDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public decimal ResourceMaxAmount { get; set; } = 100m;
        public int MaxLevel { get; set; } = 50;
        public IReadOnlyList<CombatStyleFocusDefinitionJson> Focuses { get; set; } = [];
        public IReadOnlyList<CombatStyleTreeNodeDefinitionJson> SkillTreeNodes { get; set; } = [];
        public IReadOnlyList<CombatStyleRuleDefinitionJson> Rules { get; set; } = [];
        public IReadOnlyList<CombatStyleRuleOperationJson> ResourceOverflowOperations { get; set; } = [];
        public string CoreMechanic { get; set; } = string.Empty;
    }

    private sealed class CombatStyleTreeNodeDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxRank { get; set; } = 1;
        public int RequiredLevel { get; set; }
        public string? RequiredNodeId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public IReadOnlyList<string> Tags { get; set; } = [];
        public bool CountsTowardFocus { get; set; }
        public IReadOnlyList<CombatStyleRuleDefinitionJson> Rules { get; set; } = [];
        public int Row { get; set; }
        public string Lane { get; set; } = CombatStyleNodeLanes.Middle;
        public string NodeType { get; set; } = CombatStyleNodeTypes.Minor;
        public string? MutatorKind { get; set; }
        public IReadOnlyList<string> MutatorGroups { get; set; } = [];
        public CombatStyleAbilityMutatorDefinition? Mutator { get; set; }
        public CombatStyleNodeTooltipDefinition Tooltip { get; set; } = new();
    }

    private sealed class CombatStyleFocusDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UnlockLevel { get; set; } = 10;
        public IReadOnlyList<CombatStyleRuleDefinitionJson> Rules { get; set; } = [];
    }

    private sealed class CombatStyleRuleDefinitionJson
    {
        public string Id { get; set; } = string.Empty;
        public int MinStyleLevel { get; set; } = 1;
        public int? MaxStyleLevel { get; set; }
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
        public string? TriggerKey { get; set; }
        public AttributeType Stat { get; set; }
        public decimal Coefficient { get; set; }
        public DamageType DamageType { get; set; }
        public bool? UsesProcCoefficient { get; set; }
        public string? EmpowermentId { get; set; }
        public EffectPredicate AppliesTo { get; set; } = new();
        public bool? ConsumeOnUse { get; set; }
        public decimal? MaxHealthPercent { get; set; }
        public decimal? DamagePercent { get; set; }
        public decimal? DamageReductionInheritancePercent { get; set; }
        public decimal? MagicPowerInheritancePercent { get; set; }
        public decimal? MaxInheritedDamageReductionPercent { get; set; }
        public int? MaxTriggersPerEncounter { get; set; }
        public IReadOnlyList<StyleValueModifierJson> AdditivePercentModifiers { get; set; } = [];
        public IReadOnlyList<StyleValueModifierJson> PercentModifiers { get; set; } = [];
        public IReadOnlyList<StyleValueModifierJson> AmountModifiers { get; set; } = [];
        public IReadOnlyList<StyleValueModifierJson> CoefficientModifiers { get; set; } = [];
        public IReadOnlyList<StyleValueModifierJson> MaxHealthPercentModifiers { get; set; } = [];
        public IReadOnlyList<StyleValueModifierJson> DamagePercentModifiers { get; set; } = [];
        public IReadOnlyList<StyleValueModifierJson> MaxTriggerModifiers { get; set; } = [];
    }

    private sealed class StyleValueModifierJson
    {
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? NodeId { get; set; }
        public string? FocusId { get; set; }
        public int MinStyleLevel { get; set; } = 1;
        public int? MaxStyleLevel { get; set; }
    }
}
