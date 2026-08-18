using Domain.Models.Combat.Abilities;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Combat.Engine;

public sealed class JsonAbilityCatalogProvider : ICompiledAbilityCatalogProvider
{
    private readonly AbilityCatalog _catalog;
    private readonly Lazy<CompiledAbilityCatalog> _compiledCatalog;

    public JsonAbilityCatalogProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var abilityPath = Path.Combine(contentRootPath, contentRoot, "combat", "abilities.json");
        var statusPath = Path.Combine(contentRootPath, contentRoot, "combat", "statuses.json");
        var summonPath = Path.Combine(contentRootPath, contentRoot, "combat", "summons.json");

        var abilities = ReadList<AbilitySpec>(abilityPath, options);
        var statuses = ReadList<StatusSpec>(statusPath, options);
        var summons = ReadList<SummonSpec>(summonPath, options);
        var owningEssences = abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.OwningEssenceId))
            .ToDictionary(x => x.Id, x => x.OwningEssenceId!, StringComparer.OrdinalIgnoreCase);

        _catalog = AbilityCatalogValidator.CreateCatalog(abilities, statuses, owningEssences, summons);
        _compiledCatalog = new Lazy<CompiledAbilityCatalog>(
            () => new CompiledAbilityCatalog(
                AbilityCompiler.CompileAbilities(_catalog.Abilities),
                AbilityCompiler.CompileStatuses(_catalog.Statuses),
                AbilityCompiler.CompileSummons(_catalog.Summons)));
    }

    public AbilityCatalog GetCatalog() => _catalog;

    public CompiledAbilityCatalog GetCompiledCatalog() => _compiledCatalog.Value;

    private static IReadOnlyList<T> ReadList<T>(string path, JsonSerializerOptions options)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find ability catalog file '{path}'.", path);

        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), options) ?? [];
    }
}
