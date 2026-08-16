using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Quests.Events;

public sealed class JsonEventQuestDefinitionProvider : IEventQuestDefinitionProvider
{
    private static readonly HashSet<string> ObjectiveTypes =
    [
        "CombatEncounterCompleted",
        "AreaActionCompletedWithTool",
        "EssenceAbsorbed",
        "EssenceFocusSet",
        "FocusedCreatureEssenceReceived",
        "EssenceAscended",
        "CompatibleEssenceLoadout",
        "EquipmentCrafted",
        "EquipmentTempered",
        "TemperingActionCompleted",
        "CharacterLevelReached",
        "ColosseumBattleStarted",
        "TournamentBattleCompleted",
        "DungeonRunStarted",
        "DungeonRunCompleted",
        "DailyProphecyCompleted"
    ];

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, EventQuestDefinition>> _definitions;
    private readonly IReadOnlyList<EventQuestDefinition> _latest;

    public JsonEventQuestDefinitionProvider(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "event-quests");
        var definitions = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
                .Select(file => JsonSerializer.Deserialize<EventQuestDefinition>(File.ReadAllText(file), options)
                    ?? throw new InvalidOperationException($"Event quest definition '{file}' was empty."))
                .ToList()
            : [];

        Validate(definitions);
        ValidateReferences(definitions, Path.Combine(contentRootPath, contentRoot));
        _definitions = definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<int, EventQuestDefinition>)group.ToDictionary(x => x.Version),
                StringComparer.OrdinalIgnoreCase);
        _latest = _definitions.Values
            .Select(versions => versions.Values.MaxBy(x => x.Version)!)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartsAtUtc)
            .ToList();
    }

    public IReadOnlyList<EventQuestDefinition> GetAll() => _latest;

    public EventQuestDefinition Get(string eventQuestId, int? version = null)
    {
        if (!_definitions.TryGetValue(eventQuestId, out var versions))
        {
            throw new InvalidOperationException($"Unknown event quest definition '{eventQuestId}'.");
        }

        if (!version.HasValue) return versions.Values.MaxBy(x => x.Version)!;
        return versions.TryGetValue(version.Value, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"Unknown event quest definition '{eventQuestId}' version {version.Value}.");
    }

    private static void Validate(IReadOnlyList<EventQuestDefinition> definitions)
    {
        var duplicates = definitions
            .GroupBy(x => $"{x.Id}:{x.Version}", StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate event quest definition versions: " + string.Join(", ", duplicates));
        }

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) ||
                definition.Version <= 0 ||
                string.IsNullOrWhiteSpace(definition.Title) ||
                definition.Objectives.Count == 0 ||
                definition.StartsAtUtc >= definition.EndsAtUtc ||
                definition.EndsAtUtc >= definition.ClaimEndsAtUtc ||
                definition.MinimumContribution < 0)
            {
                throw new InvalidOperationException(
                    $"Event quest '{definition.Id}' has invalid identity, schedule, objectives, or eligibility.");
            }

            EnsureUniqueKeys(definition.Id, "objective", definition.Objectives.Select(x => x.Key));
            EnsureUniqueKeys(definition.Id, "reward", definition.Rewards.Select(x => x.Key));
            EnsureUniqueKeys(
                definition.Id,
                "personal milestone",
                definition.PersonalMilestones.Select(x => x.Key));
            foreach (var objective in definition.Objectives)
            {
                if (!ObjectiveTypes.Contains(objective.Type) || objective.RequiredAmount <= 0)
                {
                    throw new InvalidOperationException(
                        $"Event quest '{definition.Id}' has invalid objective '{objective.Key}'.");
                }
            }

            ValidateRewards(definition.Id, "community", definition.Rewards);

            long previousThreshold = 0;
            foreach (var milestone in definition.PersonalMilestones)
            {
                if (milestone.RequiredContribution <= previousThreshold || milestone.Rewards.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Event quest '{definition.Id}' personal milestone '{milestone.Key}' must have an ascending positive threshold and at least one reward.");
                }

                EnsureUniqueKeys(
                    definition.Id,
                    $"reward in milestone '{milestone.Key}'",
                    milestone.Rewards.Select(x => x.Key));
                ValidateRewards(definition.Id, milestone.Key, milestone.Rewards);
                previousThreshold = milestone.RequiredContribution;
            }
        }
    }

    private static void ValidateRewards(
        string eventQuestId,
        string rewardGroup,
        IReadOnlyList<QuestRewardDefinition> rewards)
    {
        foreach (var reward in rewards)
        {
            var validItem = reward.Type == "Item" && !string.IsNullOrWhiteSpace(reward.ItemBaseId);
            var validCurrency = reward.Type == "SigilFragments" && string.IsNullOrWhiteSpace(reward.ItemBaseId);
            if ((!validItem && !validCurrency) || reward.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    $"Event quest '{eventQuestId}' has invalid reward '{reward.Key}' in '{rewardGroup}'.");
            }
        }
    }

    private static void EnsureUniqueKeys(string id, string kind, IEnumerable<string> keys)
    {
        var invalid = keys.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (invalid.Count > 0)
        {
            throw new InvalidOperationException(
                $"Event quest '{id}' has duplicate or empty {kind} keys: {string.Join(", ", invalid)}.");
        }
    }

    private static void ValidateReferences(
        IReadOnlyList<EventQuestDefinition> definitions,
        string dataRoot)
    {
        var itemPath = Path.Combine(dataRoot, "items", "items.json");
        var regionPath = Path.Combine(dataRoot, "world", "regions.json");
        if (!File.Exists(itemPath) || !File.Exists(regionPath))
        {
            throw new InvalidOperationException(
                "Event quest validation requires the item and region catalogs.");
        }

        using var itemDocument = JsonDocument.Parse(File.ReadAllText(itemPath));
        var itemIds = itemDocument.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingItems = definitions.SelectMany(x =>
                x.Rewards.Concat(x.PersonalMilestones.SelectMany(milestone => milestone.Rewards)))
            .Where(x => x.Type == "Item")
            .Select(x => x.ItemBaseId!)
            .Where(x => !itemIds.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingItems.Count > 0)
        {
            throw new InvalidOperationException(
                "Event quest rewards reference missing item bases: " + string.Join(", ", missingItems));
        }

        using var regionDocument = JsonDocument.Parse(File.ReadAllText(regionPath));
        var areaIds = regionDocument.RootElement.GetProperty("regions")
            .EnumerateArray()
            .SelectMany(region => region.GetProperty("areas").EnumerateArray())
            .Select(area => area.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingAreas = definitions.SelectMany(x => x.Objectives)
            .Select(x => x.Filters.AreaId)
            .Where(x => !string.IsNullOrWhiteSpace(x) && !areaIds.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingAreas.Count > 0)
        {
            throw new InvalidOperationException(
                "Event quest objectives reference missing areas: " + string.Join(", ", missingAreas));
        }
    }
}
