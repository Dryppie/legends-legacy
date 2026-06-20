using Domain.Models.Combat.Abilities.V2;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Combat.V2;

public sealed class JsonAbilityCatalogV2Provider : IAbilityCatalogV2Provider
{
    private readonly AbilityCatalogV2 _catalog;

    public JsonAbilityCatalogV2Provider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var abilityPath = Path.Combine(contentRootPath, contentRoot, "abilities.v2.json");
        var statusPath = Path.Combine(contentRootPath, contentRoot, "statuses.v2.json");
        var summonPath = Path.Combine(contentRootPath, contentRoot, "summons.v2.json");

        var abilities = ReadList<AbilitySpec>(abilityPath, options);
        var statuses = ReadList<StatusSpec>(statusPath, options);
        var summons = ReadList<SummonSpec>(summonPath, options);
        var owningEssences = abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.OwningEssenceId))
            .ToDictionary(x => x.Id, x => x.OwningEssenceId!, StringComparer.OrdinalIgnoreCase);

        _catalog = AbilityCatalogV2Validator.CreateCatalog(abilities, statuses, owningEssences, summons);
    }

    public AbilityCatalogV2 GetCatalog() => _catalog;

    private static IReadOnlyList<T> ReadList<T>(string path, JsonSerializerOptions options)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find ability catalog v2 file '{path}'.", path);

        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), options) ?? [];
    }
}
