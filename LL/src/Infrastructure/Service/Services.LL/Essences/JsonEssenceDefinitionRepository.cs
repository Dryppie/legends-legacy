using Application.Interfaces.Services.LL.Essences;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Essences;

public sealed class JsonEssenceDefinitionRepository : IEssenceDefinitionRepository
{
    private readonly IReadOnlyList<EssenceDefinition> _definitions;
    private readonly IReadOnlyDictionary<string, EssenceProgressionTemplate> _templates;
    private readonly IReadOnlyDictionary<string, AbilityDefinition> _abilities;

    public JsonEssenceDefinitionRepository(IConfiguration config, string contentRootPath, JsonSerializerOptions options, IEssenceDefinitionValidator validator)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "essences.json");
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<EssenceDefinitionDocument>(json, options) ?? new();

        ThrowIfDuplicateAbilityIds(document.AbilityDefinitions);
        _abilities = document.AbilityDefinitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        ResolveAbilityReferences(document.Essences);
        validator.ThrowIfInvalid(document.Essences);
        _definitions = document.Essences;
        _templates = document.ProgressionTemplates.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;

    public EssenceDefinition? GetById(string essenceDefinitionId) =>
        _definitions.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

    public EssenceDefinition? GetByMonsterId(string monsterId) =>
        _definitions.FirstOrDefault(x => x.SourceMonsterId.Equals(monsterId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyDictionary<string, EssenceProgressionTemplate> GetProgressionTemplates() => _templates;

    public AbilityDefinition? GetAbilityById(string abilityId) =>
        _abilities.TryGetValue(abilityId, out var ability) ? ability : null;

    private static void ThrowIfDuplicateAbilityIds(IEnumerable<AbilityDefinition> abilities)
    {
        var duplicates = abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new InvalidOperationException("Duplicate Ability definition ids: " + string.Join(", ", duplicates));
    }

    private void ResolveAbilityReferences(IEnumerable<EssenceDefinition> essences)
    {
        foreach (var essence in essences)
        {
            if (!string.IsNullOrWhiteSpace(essence.ActiveAbilityId) && _abilities.TryGetValue(essence.ActiveAbilityId, out var active))
                essence.ActiveAbility = active;

            if (!string.IsNullOrWhiteSpace(essence.PassiveAbilityId) && _abilities.TryGetValue(essence.PassiveAbilityId, out var passive))
                essence.PassiveAbility = passive;
        }
    }

    private sealed class EssenceDefinitionDocument
    {
        public List<EssenceProgressionTemplate> ProgressionTemplates { get; set; } = [];
        public List<AbilityDefinition> AbilityDefinitions { get; set; } = [];
        public List<EssenceDefinition> Essences { get; set; } = [];
    }
}
