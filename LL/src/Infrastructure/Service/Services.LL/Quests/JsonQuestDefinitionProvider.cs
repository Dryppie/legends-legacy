using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Quests;

public sealed class JsonQuestDefinitionProvider : IQuestDefinitionProvider
{
    private static readonly HashSet<string> ObjectiveTypes =
    [
        "CombatEncounterCompleted",
        "EssenceAbsorbed",
        "EssenceEquipped",
        "EssenceFocusSet",
        "EquipmentCrafted",
        "EquipmentEquipped",
        "GatheringToolEquipped",
        "AreaActionCompletedWithTool",
        "CharacterLevelReached",
        "ColosseumBattleStarted",
        "DailyProphecyCompleted"
    ];

    private static readonly HashSet<string> GatheringTypes =
        ["Mining", "Woodcutting", "Skinning"];

    private static readonly HashSet<string> RewardTypes = ["Item"];
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, QuestDefinition>> _definitions;
    private readonly IReadOnlyList<QuestDefinition> _latest;

    public JsonQuestDefinitionProvider(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "quests");
        var definitions = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
                .Select(file => Read(file, options))
                .ToList()
            : [];

        Validate(definitions);
        ValidateReferences(definitions, Path.Combine(contentRootPath, contentRoot));
        _definitions = definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<int, QuestDefinition>)group.ToDictionary(x => x.Version),
                StringComparer.OrdinalIgnoreCase);
        _latest = _definitions.Values
            .Select(versions => versions.Values.MaxBy(x => x.Version)!)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<QuestDefinition> GetAll() => _latest;

    public QuestDefinition Get(string questId, int? version = null)
    {
        if (!_definitions.TryGetValue(questId, out var versions))
        {
            throw new InvalidOperationException($"Unknown quest definition '{questId}'.");
        }

        if (version.HasValue)
        {
            return versions.TryGetValue(version.Value, out var selected)
                ? selected
                : throw new InvalidOperationException(
                    $"Unknown quest definition '{questId}' version {version.Value}.");
        }

        return versions.Values.MaxBy(x => x.Version)!;
    }

    public bool TryGet(string questId, out QuestDefinition definition)
    {
        if (_definitions.TryGetValue(questId, out var versions))
        {
            definition = versions.Values.MaxBy(x => x.Version)!;
            return true;
        }

        definition = null!;
        return false;
    }

    private static QuestDefinition Read(string path, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<QuestDefinition>(File.ReadAllText(path), options)
        ?? throw new InvalidOperationException($"Quest definition '{path}' was empty.");

    private static void Validate(IReadOnlyList<QuestDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("At least one quest definition is required.");
        }

        var duplicateVersions = definitions
            .GroupBy(x => $"{x.Id}:{x.Version}", StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateVersions.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate quest definition versions: " + string.Join(", ", duplicateVersions));
        }

        var latest = definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.MaxBy(definition => definition.Version)!, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) ||
                definition.Version <= 0 ||
                string.IsNullOrWhiteSpace(definition.Title) ||
                definition.Objectives.Count == 0)
            {
                throw new InvalidOperationException(
                    "Quest definitions require id, positive version, title, and at least one objective.");
            }

            if (definition.ObjectiveMode is not ("Sequential" or "All"))
            {
                throw new InvalidOperationException(
                    $"Quest '{definition.Id}' has unsupported objective mode '{definition.ObjectiveMode}'.");
            }

            if (definition.Chain is not null &&
                (string.IsNullOrWhiteSpace(definition.Chain.Id) ||
                 string.IsNullOrWhiteSpace(definition.Chain.Title) ||
                 definition.Chain.Step <= 0 ||
                 definition.Chain.TotalSteps < definition.Chain.Step))
            {
                throw new InvalidOperationException(
                    $"Quest '{definition.Id}' has invalid chain metadata.");
            }

            if (definition.Choice is not null)
            {
                if (string.IsNullOrWhiteSpace(definition.Choice.SelectionTitle) ||
                    definition.Choice.Options.Count < 2)
                {
                    throw new InvalidOperationException(
                        $"Quest '{definition.Id}' choices require a selection title and at least two options.");
                }

                EnsureUniqueKeys(
                    definition.Id,
                    "choice option",
                    definition.Choice.Options.Select(x => x.Key));
                foreach (var option in definition.Choice.Options)
                {
                    if (string.IsNullOrWhiteSpace(option.Title) ||
                        option.CreatureId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(option.EssenceDefinitionId) ||
                        string.IsNullOrWhiteSpace(option.RewardItemBaseId) ||
                        string.IsNullOrWhiteSpace(option.EncounterKey))
                    {
                        throw new InvalidOperationException(
                            $"Quest '{definition.Id}' has an invalid choice option '{option.Key}'.");
                    }
                }
            }

            EnsureUniqueKeys(definition.Id, "objective", definition.Objectives.Select(x => x.Key));
            EnsureUniqueKeys(definition.Id, "reward", definition.Rewards.Select(x => x.Key));

            foreach (var prerequisite in definition.Availability.CompletedQuestIds)
            {
                if (!latest.ContainsKey(prerequisite))
                {
                    throw new InvalidOperationException(
                        $"Quest '{definition.Id}' references missing prerequisite '{prerequisite}'.");
                }
            }

            foreach (var objective in definition.Objectives)
            {
                if (string.IsNullOrWhiteSpace(objective.Key) ||
                    !ObjectiveTypes.Contains(objective.Type) ||
                    objective.RequiredAmount <= 0)
                {
                    throw new InvalidOperationException(
                        $"Quest '{definition.Id}' has an invalid objective '{objective.Key}'.");
                }

                if (objective.Type == "AreaActionCompletedWithTool" &&
                    (string.IsNullOrWhiteSpace(objective.Filters.AreaId) ||
                     string.IsNullOrWhiteSpace(objective.Filters.GatheringType) ||
                     !GatheringTypes.Contains(objective.Filters.GatheringType)))
                {
                    throw new InvalidOperationException(
                        $"Quest '{definition.Id}' objective '{objective.Key}' requires a valid area and gathering type.");
                }
            }

            foreach (var reward in definition.Rewards)
            {
                if (string.IsNullOrWhiteSpace(reward.Key) ||
                    !RewardTypes.Contains(reward.Type) ||
                    string.IsNullOrWhiteSpace(reward.ItemBaseId) ||
                    reward.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        $"Quest '{definition.Id}' has an invalid reward '{reward.Key}'.");
                }
            }
        }

        foreach (var chain in latest.Values
                     .Where(x => x.Chain is not null)
                     .GroupBy(x => x.Chain!.Id, StringComparer.OrdinalIgnoreCase))
        {
            var totalSteps = chain.Select(x => x.Chain!.TotalSteps).Distinct().ToList();
            var titles = chain.Select(x => x.Chain!.Title).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var duplicateSteps = chain
                .GroupBy(x => x.Chain!.Step)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            var hasCompleteSequence = totalSteps.Count == 1 &&
                                      chain.Select(x => x.Chain!.Step).Order().SequenceEqual(
                                          Enumerable.Range(1, totalSteps[0]));
            if (totalSteps.Count != 1 || titles.Count != 1 || duplicateSteps.Count > 0 || !hasCompleteSequence)
            {
                throw new InvalidOperationException(
                    $"Quest chain '{chain.Key}' must have one title and every step from 1 through its total.");
            }
        }

        DetectCycles(latest);
    }

    private static void ValidateReferences(
        IReadOnlyList<QuestDefinition> definitions,
        string dataRoot)
    {
        var itemPath = Path.Combine(dataRoot, "items", "items.json");
        var regionPath = Path.Combine(dataRoot, "world", "regions.json");
        if (!File.Exists(itemPath) || !File.Exists(regionPath))
        {
            throw new InvalidOperationException(
                "Quest validation requires Data/items/items.json and Data/world/regions.json.");
        }

        using var itemDocument = JsonDocument.Parse(File.ReadAllText(itemPath));
        var items = itemDocument.RootElement
            .EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("id").GetString()!,
                x => x,
                StringComparer.OrdinalIgnoreCase);
        var itemIds = items.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingItems = definitions
            .SelectMany(x => x.Rewards)
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemBaseId) && !itemIds.Contains(x.ItemBaseId))
            .Select(x => x.ItemBaseId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingItems.Count > 0)
        {
            throw new InvalidOperationException(
                "Quest rewards reference missing item bases: " + string.Join(", ", missingItems));
        }

        var creaturePath = Path.Combine(dataRoot, "world", "creatures.json");
        var essencePath = Path.Combine(dataRoot, "essences", "essences.json");
        if (!File.Exists(creaturePath) || !File.Exists(essencePath))
        {
            throw new InvalidOperationException(
                "Quest choice validation requires creature and Essence catalogs.");
        }

        using var creatureDocument = JsonDocument.Parse(File.ReadAllText(creaturePath));
        var creatures = creatureDocument.RootElement
            .GetProperty("creatures")
            .EnumerateArray()
            .ToDictionary(
                x => Guid.Parse(x.GetProperty("id").GetString()!),
                x => x);
        using var essenceDocument = JsonDocument.Parse(File.ReadAllText(essencePath));
        var essences = essenceDocument.RootElement
            .GetProperty("essences")
            .EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("id").GetString()!,
                x => x,
                StringComparer.OrdinalIgnoreCase);

        foreach (var (quest, option) in definitions
                     .Where(x => x.Choice is not null)
                     .SelectMany(
                         quest => quest.Choice!.Options.Select(option => (quest, option))))
        {
            if (!creatures.TryGetValue(option.CreatureId, out var creature))
            {
                throw new InvalidOperationException(
                    $"Quest '{quest.Id}' choice '{option.Key}' references missing creature '{option.CreatureId}'.");
            }

            if (!essences.TryGetValue(option.EssenceDefinitionId, out var essence))
            {
                throw new InvalidOperationException(
                    $"Quest '{quest.Id}' choice '{option.Key}' references missing Essence '{option.EssenceDefinitionId}'.");
            }

            if (!items.TryGetValue(option.RewardItemBaseId, out var rewardItem) ||
                !string.Equals(
                    rewardItem.GetProperty("itemType").GetString(),
                    "Essence",
                    StringComparison.OrdinalIgnoreCase) ||
                !option.RewardItemBaseId.Equals(
                    $"item.{option.EssenceDefinitionId}",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Quest '{quest.Id}' choice '{option.Key}' has an invalid Essence reward item.");
            }

            var creatureName = creature.GetProperty("name").GetString()!;
            var expectedMonsterId = "monster." + creatureName.Trim()
                .Replace("'", string.Empty, StringComparison.Ordinal)
                .Replace(" ", "_", StringComparison.Ordinal)
                .ToLowerInvariant();
            if (!expectedMonsterId.Equals(
                    essence.GetProperty("sourceMonsterId").GetString(),
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(essence.GetProperty("activeAbilityId").GetString()) ||
                string.IsNullOrWhiteSpace(essence.GetProperty("passiveAbilityId").GetString()))
            {
                throw new InvalidOperationException(
                    $"Quest '{quest.Id}' choice '{option.Key}' does not match its creature's Essence abilities.");
            }

            option.CreatureName = creatureName;
        }

        using var regionDocument = JsonDocument.Parse(File.ReadAllText(regionPath));
        var areas = regionDocument.RootElement
            .GetProperty("regions")
            .EnumerateArray()
            .SelectMany(region => region.GetProperty("areas").EnumerateArray())
            .ToList();
        var areaIds = areas
            .Select(area => area.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingAreas = definitions
            .SelectMany(x => x.Objectives)
            .Select(x => x.Filters.AreaId)
            .Where(x => !string.IsNullOrWhiteSpace(x) && !areaIds.Contains(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingAreas.Count > 0)
        {
            throw new InvalidOperationException(
                "Quest objectives reference missing combat areas: " + string.Join(", ", missingAreas));
        }

        var questIds = definitions
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingAreaQuestIds = areas
            .SelectMany(area => new[]
            {
                ReadOptionalString(area, "requiredActiveQuestId"),
                ReadOptionalString(area, "requiredCompletedQuestId")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x) && !questIds.Contains(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingAreaQuestIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Combat areas reference missing quests: " + string.Join(", ", missingAreaQuestIds));
        }

        var latestDefinitions = definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.MaxBy(definition => definition.Version)!,
                StringComparer.OrdinalIgnoreCase);
        foreach (var (quest, objective) in definitions.SelectMany(
                     quest => quest.Objectives.Select(objective => (quest, objective))))
        {
            var choiceQuestId = objective.Filters.EssenceDefinitionFromChoiceQuestId;
            if (string.IsNullOrWhiteSpace(choiceQuestId)) continue;
            if (!latestDefinitions.TryGetValue(choiceQuestId, out var choiceQuest) ||
                choiceQuest.Choice is null)
            {
                throw new InvalidOperationException(
                    $"Quest '{quest.Id}' objective '{objective.Key}' references a missing choice quest '{choiceQuestId}'.");
            }
        }
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static void EnsureUniqueKeys(string questId, string kind, IEnumerable<string> keys)
    {
        var duplicates = keys
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Quest '{questId}' has duplicate or empty {kind} keys: {string.Join(", ", duplicates)}.");
        }
    }

    private static void DetectCycles(IReadOnlyDictionary<string, QuestDefinition> definitions)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var questId in definitions.Keys)
        {
            Visit(questId);
        }

        void Visit(string questId)
        {
            if (visited.Contains(questId)) return;
            if (!visiting.Add(questId))
            {
                throw new InvalidOperationException($"Quest prerequisite cycle detected at '{questId}'.");
            }

            foreach (var prerequisite in definitions[questId].Availability.CompletedQuestIds)
            {
                Visit(prerequisite);
            }

            visiting.Remove(questId);
            visited.Add(questId);
        }
    }
}
