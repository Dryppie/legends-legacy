using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Essences;

public sealed class JsonEssenceCodexCollectionDefinitionProvider : IEssenceCodexCollectionDefinitionProvider
{
    private readonly IReadOnlyList<EssenceCodexCollectionDefinition> _definitions;

    public JsonEssenceCodexCollectionDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "essence-codex-collections.json");
        var document = JsonSerializer.Deserialize<EssenceCodexCollectionDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Collections, essenceDefinitions);
        _definitions = document.Collections;
    }

    public IReadOnlyList<EssenceCodexCollectionDefinition> GetAll() => _definitions;

    private static void ThrowIfInvalid(
        IReadOnlyList<EssenceCodexCollectionDefinition> definitions,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        var duplicates = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidDataException("Duplicate Essence Codex collection ids: " + string.Join(", ", duplicates));
        }

        var invalidHeaders = definitions
            .Where(x =>
                string.IsNullOrWhiteSpace(x.Id) ||
                string.IsNullOrWhiteSpace(x.Title) ||
                string.IsNullOrWhiteSpace(x.Description) ||
                string.IsNullOrWhiteSpace(x.Category) ||
                string.IsNullOrWhiteSpace(x.Bonus.Description))
            .Select(x => string.IsNullOrWhiteSpace(x.Id) ? "<missing id>" : x.Id)
            .ToList();

        if (invalidHeaders.Count > 0)
        {
            throw new InvalidDataException("Essence Codex collections require ids, titles, descriptions, categories, and bonus descriptions: " + string.Join(", ", invalidHeaders));
        }

        var invalidCounts = definitions
            .Where(x => x.EssenceDefinitionIds.Count is < 2 or > 6)
            .Select(x => x.Id)
            .ToList();

        if (invalidCounts.Count > 0)
        {
            throw new InvalidDataException("Essence Codex collections must contain between 2 and 6 essences: " + string.Join(", ", invalidCounts));
        }

        var duplicatedMembers = definitions
            .Where(x => x.EssenceDefinitionIds
                .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
            .Select(x => x.Id)
            .ToList();

        if (duplicatedMembers.Count > 0)
        {
            throw new InvalidDataException("Essence Codex collections cannot contain duplicate essence ids: " + string.Join(", ", duplicatedMembers));
        }

        var unknownEssences = definitions
            .SelectMany(collection => collection.EssenceDefinitionIds.Select(id => new { collection.Id, EssenceDefinitionId = id }))
            .Where(x => string.IsNullOrWhiteSpace(x.EssenceDefinitionId) || essenceDefinitions.GetById(x.EssenceDefinitionId) is null)
            .Select(x => $"{x.Id}:{x.EssenceDefinitionId}")
            .ToList();

        if (unknownEssences.Count > 0)
        {
            throw new InvalidDataException("Essence Codex collections reference unknown essence ids: " + string.Join(", ", unknownEssences));
        }

        var invalidBonusValues = definitions
            .Where(x => x.Bonus.Value <= 0 || x.Bonus.ValuePerCollectionAscensionTier < 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidBonusValues.Count > 0)
        {
            throw new InvalidDataException("Essence Codex collections require positive base bonus values and non-negative ascension bonus values: " + string.Join(", ", invalidBonusValues));
        }
    }

    private sealed class EssenceCodexCollectionDefinitionDocument
    {
        public List<EssenceCodexCollectionDefinition> Collections { get; set; } = [];
    }
}
