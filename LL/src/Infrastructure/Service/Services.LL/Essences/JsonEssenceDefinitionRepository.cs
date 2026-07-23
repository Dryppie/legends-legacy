using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Essences;

public sealed class JsonEssenceDefinitionRepository : IEssenceDefinitionRepository
{
    private readonly IReadOnlyList<EssenceDefinition> _definitions;
    private readonly IReadOnlyDictionary<string, AbilitySpec> _abilities;
    private readonly IReadOnlyDictionary<(string EssenceId, AbilitySpecKind Kind), AbilitySpec> _abilitiesByEssenceSlot;

    public JsonEssenceDefinitionRepository(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IEssenceDefinitionValidator essenceValidator)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var essencePath = Path.Combine(contentRootPath, contentRoot, "essences", "essences.json");
        var abilityPath = Path.Combine(contentRootPath, contentRoot, "combat", "abilities.json");
        var essenceJson = File.ReadAllText(essencePath);
        var abilityJson = File.ReadAllText(abilityPath);
        var document = JsonSerializer.Deserialize<EssenceDefinitionDocument>(essenceJson, options) ?? new();
        var abilitySpecs = JsonSerializer.Deserialize<List<AbilitySpec>>(abilityJson, options) ?? [];

        ThrowIfDuplicateAbilityIds(abilitySpecs);
        _abilities = abilitySpecs.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        _abilitiesByEssenceSlot = abilitySpecs
            .Where(x => !string.IsNullOrWhiteSpace(x.OwningEssenceId))
            .GroupBy(x => (EssenceId: x.OwningEssenceId!, x.Kind))
            .Where(x => x.Count() == 1)
            .ToDictionary(
                x => x.Key,
                x => _abilities[x.Single().Id],
                new EssenceSlotComparer());
        ResolveAbilityReferences(document.Essences);
        essenceValidator.ThrowIfInvalid(document.Essences);
        _definitions = document.Essences;
    }

    public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;

    public IReadOnlyList<AbilitySpec> GetAllAbilities() =>
        _abilities.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();

    public EssenceDefinition? GetById(string essenceDefinitionId) =>
        _definitions.FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

    public AbilitySpec? GetAbilityById(string abilityId) =>
        _abilities.TryGetValue(abilityId, out var ability) ? ability : null;

    private static void ThrowIfDuplicateAbilityIds(IEnumerable<AbilitySpec> abilities)
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
            else if (_abilitiesByEssenceSlot.TryGetValue((essence.Id, AbilitySpecKind.Active), out active))
            {
                essence.ActiveAbility = active;
                essence.ActiveAbilityId = active.Id;
            }

            if (!string.IsNullOrWhiteSpace(essence.PassiveAbilityId) && _abilities.TryGetValue(essence.PassiveAbilityId, out var passive))
                essence.PassiveAbility = passive;
            else if (_abilitiesByEssenceSlot.TryGetValue((essence.Id, AbilitySpecKind.Passive), out passive))
            {
                essence.PassiveAbility = passive;
                essence.PassiveAbilityId = passive.Id;
            }
        }
    }

    private sealed class EssenceDefinitionDocument
    {
        public List<EssenceDefinition> Essences { get; set; } = [];
    }

    private sealed class EssenceSlotComparer : IEqualityComparer<(string EssenceId, AbilitySpecKind Kind)>
    {
        public bool Equals((string EssenceId, AbilitySpecKind Kind) x, (string EssenceId, AbilitySpecKind Kind) y) =>
            x.Kind == y.Kind && StringComparer.OrdinalIgnoreCase.Equals(x.EssenceId, y.EssenceId);

        public int GetHashCode((string EssenceId, AbilitySpecKind Kind) obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.EssenceId), obj.Kind);
    }
}
