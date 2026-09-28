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
        if (config["Combat:AbilityBalanceProfile"] is { Length: > 0 } profileId)
        {
            var profilePath = Path.Combine(contentRootPath, contentRoot, ProfileRelativePath(profileId));
            var profile = reader.Deserialize<AbilityBalanceProfile>(reader.ReadAllText(profilePath), options)
                ?? throw new InvalidOperationException("Missing ability balance profile.");
            Domain.Models.Attributes.AttributeRules.ValidateVersion(profile.AttributeRulesVersion);
            if (profile.SchemaVersion != 1 || profile.Id != profileId
                || profile.AttributeRulesVersion != (config.GetValue<int?>("AttributeRedesign:LiveVersion") ?? 17))
                throw new InvalidOperationException("Ability balance profile metadata or combat rules do not match the selected release.");
            if (profile.Abilities is null || profile.Abilities.Any(x => x is null)
                || profile.Abilities.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != profile.Abilities.Count)
                throw new InvalidOperationException("Ability balance profiles must contain unique ability replacements.");
            var replacements = profile.Abilities.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            if (replacements.Count == 0 || replacements.Keys.Except(abilities.Select(x => x.Id), StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("Ability balance profiles must replace existing abilities.");
            foreach (var original in abilities.Where(x => replacements.ContainsKey(x.Id)))
                if (replacements[original.Id].OwningEssenceId != original.OwningEssenceId || replacements[original.Id].Kind != original.Kind)
                    throw new InvalidOperationException("Ability balance profiles must preserve ability ownership and kind.");
            abilities = abilities.Select(x => replacements.GetValueOrDefault(x.Id, x)).ToArray();
        }
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

    public static string ProfileRelativePath(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId) || profileId.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new InvalidOperationException("Ability balance profile IDs must contain only ASCII letters, digits and hyphens.");
        return Path.Combine("combat", $"ability-balance.{profileId}.json");
    }

    private sealed record AbilityBalanceProfile(int SchemaVersion, string Id, int AttributeRulesVersion, IReadOnlyList<AbilitySpec> Abilities);

    private static IReadOnlyList<T> ReadList<T>(string path, JsonSerializerOptions options, ContentJsonReader reader)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find ability catalog file '{path}'.", path);

        return reader.Deserialize<List<T>>(reader.ReadAllText(path), options) ?? [];
    }
}
