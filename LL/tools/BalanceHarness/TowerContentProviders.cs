using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.CombatStyles;
using Services.LL.Essences;
using Services.LL.Items;
using Services.LL.Regions;
using Services.LL.WorldTower;

namespace BalanceHarness;

/// <summary>Keep the captured gameplay ABI usable without replacing its binaries.
/// Current providers receive the accounting reader; captured providers retain
/// their original constructors. Unsupported accounting fails before content IO.</summary>
internal static class TowerContentProviders
{
    private static readonly Type? ReaderType = typeof(JsonAbilityCatalogProvider).Assembly
        .GetType("Services.LL.Content.ContentJsonReader", throwOnError: false);
    private static readonly ConcurrentDictionary<(Type, string), MethodBase> Factories = new();
    internal static bool SupportsAccounting => ReaderType is not null;

    // The missing optional type must not occur in the caller's signature or JIT
    // body. Inlining this method would reintroduce the captured-runtime failure.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object AccountingReader() => TowerContentJsonReader.Instance;

    private static T Load<T>(Type provider, string? method, Type[] signature, params object?[] arguments)
    {
        if (ReaderType is null && TowerWorkAccounting.Enabled)
            throw new NotSupportedException("Captured content providers do not support content-read accounting. Use plain execution or qualify a compatible accounting runtime.");
        var factory = Factories.GetOrAdd((provider, method ?? ".ctor"), _ => {
            var parameters = ReaderType is null ? signature : [.. signature, ReaderType];
            return (MethodBase?) (method is null ? provider.GetConstructor(parameters) : provider.GetMethod(method, parameters))
                ?? throw new MissingMethodException(provider.FullName, method ?? ".ctor");
        });
        var values = ReaderType is null ? arguments : [.. arguments, AccountingReader()];
        return Invoke<T>(factory, values);
    }

    internal static T Invoke<T>(MethodBase factory, object?[] arguments)
    {
        try { return (T)(factory is ConstructorInfo ctor ? ctor.Invoke(arguments) : ((MethodInfo)factory).Invoke(null, arguments))!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static JsonEssenceDefinitionRepository Essences(IConfiguration config, string root, JsonSerializerOptions options,
        IEssenceDefinitionValidator validator) => Load<JsonEssenceDefinitionRepository>(typeof(JsonEssenceDefinitionRepository), null,
            [typeof(IConfiguration), typeof(string), typeof(JsonSerializerOptions), typeof(IEssenceDefinitionValidator)], config, root, options, validator);

    internal static JsonCreatureEssenceLootTableRepository Loot(IConfiguration config, string root, JsonSerializerOptions options,
        IEssenceDefinitionRepository essences) => Load<JsonCreatureEssenceLootTableRepository>(typeof(JsonCreatureEssenceLootTableRepository), null,
            [typeof(IConfiguration), typeof(string), typeof(JsonSerializerOptions), typeof(IEssenceDefinitionRepository)], config, root, options, essences);

    internal static JsonCreatureAbilityDefinitionProvider CreatureAbilities(IConfiguration config, string root, JsonSerializerOptions options)
        => Load<JsonCreatureAbilityDefinitionProvider>(typeof(JsonCreatureAbilityDefinitionProvider), null,
            [typeof(IConfiguration), typeof(string), typeof(JsonSerializerOptions)], config, root, options);

    internal static RegionCreatureScalingProvider Scaling(IConfiguration config, string root, JsonSerializerOptions options)
        => Load<RegionCreatureScalingProvider>(typeof(RegionCreatureScalingProvider), null,
            [typeof(IConfiguration), typeof(string), typeof(JsonSerializerOptions)], config, root, options);

    internal static JsonAbilityCatalogProvider Abilities(IConfiguration config, string root, JsonSerializerOptions options,
        ThreatAndTankingOptions threat) => Load<JsonAbilityCatalogProvider>(typeof(JsonAbilityCatalogProvider), null,
            [typeof(IConfiguration), typeof(string), typeof(JsonSerializerOptions), typeof(ThreatAndTankingOptions)], config, root, options, threat);

    internal static StarterEquipmentCatalog Equipment(string path) => Load<StarterEquipmentCatalog>(typeof(JsonStarterEquipmentCatalog), "Load", [typeof(string)], path);

    internal static JsonCombatStyleCatalogProvider Styles(string path) => Load<JsonCombatStyleCatalogProvider>(typeof(JsonCombatStyleCatalogProvider), null, [typeof(string)], path);

    internal static JsonWorldTowerDefinitionProvider Floors(string path, JsonSerializerOptions options)
        => Load<JsonWorldTowerDefinitionProvider>(typeof(JsonWorldTowerDefinitionProvider), null, [typeof(string), typeof(JsonSerializerOptions)], path, options);
}
