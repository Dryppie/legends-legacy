using System.Text.Json;
using Application.Interfaces.Services.LL.Combat;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Combat;

public sealed class JsonCreatureAbilityDefinitionProvider : ICreatureAbilityDefinitionProvider
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _abilityIdsByMonsterId;

    public JsonCreatureAbilityDefinitionProvider(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "combat", "creature-abilities.json");
        var document = JsonSerializer.Deserialize<CreatureAbilityDocument>(
                           File.ReadAllText(path),
                           jsonOptions)
                       ?? new CreatureAbilityDocument();

        var duplicateMonsterId = document.Creatures
            .GroupBy(x => x.MonsterId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateMonsterId is not null)
            throw new InvalidOperationException($"Duplicate creature ability profile '{duplicateMonsterId}'.");

        foreach (var profile in document.Creatures)
        {
            if (string.IsNullOrWhiteSpace(profile.MonsterId))
                throw new InvalidOperationException("Creature ability profile monsterId is required.");
            if (profile.AbilityIds.Count == 0 || profile.AbilityIds.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"{profile.MonsterId}: at least one valid abilityId is required.");
            if (profile.AbilityIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != profile.AbilityIds.Count)
                throw new InvalidOperationException($"{profile.MonsterId}: duplicate abilityId.");
        }

        _abilityIdsByMonsterId = document.Creatures.ToDictionary(
            x => x.MonsterId,
            x => (IReadOnlyList<string>)[.. x.AbilityIds],
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetAbilityIds(string monsterDefinitionId) =>
        _abilityIdsByMonsterId.GetValueOrDefault(monsterDefinitionId) ?? [];

    private sealed class CreatureAbilityDocument
    {
        public List<CreatureAbilityProfile> Creatures { get; set; } = [];
    }

    private sealed class CreatureAbilityProfile
    {
        public string MonsterId { get; set; } = string.Empty;
        public List<string> AbilityIds { get; set; } = [];
    }
}
