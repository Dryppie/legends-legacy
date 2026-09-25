using Domain.Models.Combat.Abilities;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Services.LL.Content;

namespace Services.LL.Combat.Engine;

public sealed class JsonAbilityCatalogProvider : ICompiledAbilityCatalogProvider
{
    private readonly AbilityCatalog _catalog;
    private readonly Lazy<CompiledAbilityCatalog> _compiledCatalog;

    public JsonAbilityCatalogProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        ThreatAndTankingOptions? threatAndTankingOptions = null,
        ContentJsonReader? reader = null)
    {
        reader ??= ContentJsonReader.Default;
        var contentRoot = config["Content:Root"] ?? "Data";
        var abilityPath = Path.Combine(contentRootPath, contentRoot, "combat", "abilities.json");
        var statusPath = Path.Combine(contentRootPath, contentRoot, "combat", "statuses.json");
        var summonPath = Path.Combine(contentRootPath, contentRoot, "combat", "summons.json");

        var abilities = ReadList<AbilitySpec>(abilityPath, options, reader);
        var statuses = ReadList<StatusSpec>(statusPath, options, reader);
        var summons = ReadList<SummonSpec>(summonPath, options, reader);
        var owningEssences = abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.OwningEssenceId))
            .ToDictionary(x => x.Id, x => x.OwningEssenceId!, StringComparer.OrdinalIgnoreCase);

        _catalog = AbilityCatalogValidator.CreateCatalog(abilities, statuses, owningEssences, summons);
        var threatTuning = (threatAndTankingOptions ?? new ThreatAndTankingOptions()).ToAbilityThreatTuning();
        _compiledCatalog = new Lazy<CompiledAbilityCatalog>(
            () => new CompiledAbilityCatalog(
                AbilityCompiler.CompileAbilities(_catalog.Abilities, threatTuning),
                AbilityCompiler.CompileStatuses(_catalog.Statuses),
                AbilityCompiler.CompileSummons(_catalog.Summons, threatTuning)));
    }

    public AbilityCatalog GetCatalog() => _catalog;

    public CompiledAbilityCatalog GetCompiledCatalog() => _compiledCatalog.Value;

    private static IReadOnlyList<T> ReadList<T>(string path, JsonSerializerOptions options, ContentJsonReader reader)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find ability catalog file '{path}'.", path);

        return reader.Deserialize<List<T>>(reader.ReadAllText(path), options) ?? [];
    }
}
