using Application.Interfaces.Services.LL.Essences;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Essences;

public sealed class JsonEssenceDefinitionRepository : IEssenceDefinitionRepository
{
    private readonly IReadOnlyList<EssenceDefinition> _definitions;
    private readonly IReadOnlyDictionary<string, AbilityDefinition> _abilities;

    public JsonEssenceDefinitionRepository(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IAbilityCatalogValidator abilityValidator,
        IEssenceDefinitionValidator essenceValidator)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var essencePath = Path.Combine(contentRootPath, contentRoot, "essences.json");
        var abilityPath = Path.Combine(contentRootPath, contentRoot, "abilities.json");
        var essenceJson = File.ReadAllText(essencePath);
        var abilityJson = File.ReadAllText(abilityPath);
        var document = JsonSerializer.Deserialize<EssenceDefinitionDocument>(essenceJson, options) ?? new();
        var abilities = JsonSerializer.Deserialize<List<AbilityDefinition>>(abilityJson, options) ?? [];

        ThrowIfDuplicateAbilityIds(abilities);
        abilityValidator.ThrowIfInvalid(abilities);
        _abilities = abilities.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        ResolveAbilityReferences(document.Essences);
        essenceValidator.ThrowIfInvalid(document.Essences);
        _definitions = document.Essences;
    }

    public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;

    public IReadOnlyList<AbilityDefinition> GetAllAbilities() =>
        _abilities.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();

    public EssenceDefinition? GetById(string essenceDefinitionId) =>
        _definitions.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

    public EssenceDefinition? GetByMonsterId(string monsterId) =>
        _definitions.FirstOrDefault(x => x.SourceMonsterId.Equals(monsterId, StringComparison.OrdinalIgnoreCase));

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
        public List<EssenceDefinition> Essences { get; set; } = [];
    }
}
