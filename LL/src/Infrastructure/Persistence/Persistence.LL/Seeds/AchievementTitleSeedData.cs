using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Achievements;

namespace Persistence.LL.Seeds;

internal static class AchievementTitleSeedData
{
    private const string AchievementCatalogDirectoryName = "achievements";
    private const string TitleCatalogDirectoryName = "titles";
    private const string LegacyTitlesFileName = "titles.json";
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AchievementTitleSeedCatalog Load()
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
        var achievementCatalogPath = Path.Combine(dataPath, AchievementCatalogDirectoryName);
        var titleCatalogPath = Path.Combine(dataPath, TitleCatalogDirectoryName);

        if (!Directory.Exists(achievementCatalogPath))
        {
            throw new DirectoryNotFoundException(
                $"Achievement catalog directory was not found at '{achievementCatalogPath}'. Ensure Data/{AchievementCatalogDirectoryName} is copied to the output directory.");
        }

        if (!Directory.Exists(titleCatalogPath))
        {
            throw new DirectoryNotFoundException(
                $"Title catalog directory was not found at '{titleCatalogPath}'. Ensure Data/{TitleCatalogDirectoryName} is copied to the output directory.");
        }

        var achievementSeeds = LoadCatalog<AchievementSeed>(
            achievementCatalogPath,
            "achievement",
            path => !Path.GetFileName(path).Equals(LegacyTitlesFileName, StringComparison.OrdinalIgnoreCase));
        var titleSeeds = LoadCatalog<TitleSeed>(titleCatalogPath, "title");
        var document = new AchievementTitleSeedDocument
        {
            Achievements = achievementSeeds,
            Titles = titleSeeds
        };

        ValidateUniqueKeys(document.Achievements.Select(x => x.Key), "achievement");
        ValidateUniqueKeys(document.Titles.Select(x => x.Key), "title");
        ValidateTitleSources(document);

        return new AchievementTitleSeedCatalog(
            [.. document.Achievements
                .OrderBy(seed => seed.SortOrder ?? int.MaxValue)
                .ThenBy(seed => seed.Key, StringComparer.OrdinalIgnoreCase)
                .Select((seed, index) => ToAchievement(seed, index + 1))],
            [.. document.Titles
                .OrderBy(seed => seed.SortOrder ?? int.MaxValue)
                .ThenBy(seed => seed.Key, StringComparer.OrdinalIgnoreCase)
                .Select((seed, index) => ToTitle(seed, index + 1))]);
    }

    private static List<TSeed> LoadCatalog<TSeed>(
        string catalogPath,
        string catalogName,
        Func<string, bool>? includeFile = null)
    {
        var catalogFiles = Directory
            .EnumerateFiles(catalogPath, "*.json")
            .Where(path => includeFile?.Invoke(path) ?? true)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (catalogFiles.Count == 0)
        {
            throw new FileNotFoundException(
                $"No {catalogName} catalog files were found in '{catalogPath}'. Add one or more JSON files.",
                catalogPath);
        }

        var seeds = new List<TSeed>();
        foreach (var file in catalogFiles)
        {
            seeds.AddRange(LoadJson<List<TSeed>>(file));
        }

        return seeds;
    }

    private static T LoadJson<T>(string path) where T : new() =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? new T();

    private static AchievementDefinition ToAchievement(AchievementSeed seed, int fallbackSortOrder)
    {
        var key = Required(seed.Key, "achievement.key");
        return new AchievementDefinition
        {
            Id = StableGuid("achievement", key),
            Key = key,
            Name = Required(seed.Name, $"{key}.name"),
            Description = Required(seed.Description, $"{key}.description"),
            Hint = seed.Hint,
            PlayerSystemMessageTemplate = seed.PlayerSystemMessageTemplate,
            GlobalSystemMessageTemplate = seed.GlobalSystemMessageTemplate,
            Category = seed.Category,
            Type = seed.Type,
            Scope = seed.Scope,
            Visibility = seed.Visibility,
            Rarity = seed.Rarity,
            RequirementType = seed.RequirementType,
            RequirementTarget = seed.RequirementTarget,
            RequirementAmount = seed.RequirementAmount,
            Points = seed.Points,
            IsRepeatable = seed.IsRepeatable,
            IsActive = seed.IsActive,
            SortOrder = seed.SortOrder ?? fallbackSortOrder,
            IconKey = seed.IconKey,
            MetadataJson = seed.MetadataJson,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        };
    }

    private static TitleDefinition ToTitle(TitleSeed seed, int fallbackSortOrder)
    {
        var key = Required(seed.Key, "title.key");
        return new TitleDefinition
        {
            Id = StableGuid("title", key),
            Key = key,
            Name = Required(seed.Name, $"{key}.name"),
            Description = Required(seed.Description, $"{key}.description"),
            Category = seed.Category,
            Rarity = seed.Rarity,
            Scope = seed.Scope,
            SourceAchievementKey = Required(seed.SourceAchievementKey, $"{key}.sourceAchievementKey"),
            IsHiddenUntilUnlocked = seed.IsHiddenUntilUnlocked,
            IsActive = seed.IsActive,
            SeasonNumber = seed.SeasonNumber,
            IconKey = seed.IconKey,
            SortOrder = seed.SortOrder ?? fallbackSortOrder,
            MetadataJson = seed.MetadataJson,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        };
    }

    private static Guid StableGuid(string type, string key)
    {
        var normalized = Required(key, $"{type}.key").Trim().ToLowerInvariant();
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"legends-legacy:{type}:{normalized}"));

        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        return new Guid(hash);
    }

    private static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required achievement/title catalog field '{fieldName}'.");
        }

        return value.Trim();
    }

    private static void ValidateUniqueKeys(IEnumerable<string?> keys, string type)
    {
        var duplicates = keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .GroupBy(key => key!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate {type} catalog keys: {string.Join(", ", duplicates)}");
        }
    }

    private static void ValidateTitleSources(AchievementTitleSeedDocument document)
    {
        var achievementKeys = document.Achievements
            .Select(x => x.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = document.Titles
            .Where(title => !string.IsNullOrWhiteSpace(title.SourceAchievementKey))
            .Where(title => !achievementKeys.Contains(title.SourceAchievementKey!))
            .Select(title => $"{title.Key} -> {title.SourceAchievementKey}")
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Title catalog entries reference missing achievements: {string.Join(", ", missing)}");
        }
    }

    private sealed class AchievementTitleSeedDocument
    {
        public List<AchievementSeed> Achievements { get; set; } = [];
        public List<TitleSeed> Titles { get; set; } = [];
    }

    private sealed class AchievementSeed
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Hint { get; set; }
        public string? PlayerSystemMessageTemplate { get; set; }
        public string? GlobalSystemMessageTemplate { get; set; }
        public AchievementCategory Category { get; set; }
        public AchievementType Type { get; set; }
        public AchievementScope Scope { get; set; }
        public AchievementVisibility Visibility { get; set; }
        public TitleRarity Rarity { get; set; }
        public AchievementRequirementType RequirementType { get; set; }
        public string? RequirementTarget { get; set; }
        public long RequirementAmount { get; set; }
        public int Points { get; set; }
        public bool IsRepeatable { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SortOrder { get; set; }
        public string? IconKey { get; set; }
        public string? MetadataJson { get; set; }
    }

    private sealed class TitleSeed
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public AchievementCategory Category { get; set; }
        public TitleRarity Rarity { get; set; }
        public TitleScope Scope { get; set; }
        public string? SourceAchievementKey { get; set; }
        public bool IsHiddenUntilUnlocked { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SeasonNumber { get; set; }
        public string? IconKey { get; set; }
        public int? SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }
}

internal sealed record AchievementTitleSeedCatalog(
    IReadOnlyList<AchievementDefinition> Achievements,
    IReadOnlyList<TitleDefinition> Titles);
