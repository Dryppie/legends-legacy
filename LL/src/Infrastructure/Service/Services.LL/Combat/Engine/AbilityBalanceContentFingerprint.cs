using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Engine;

public static class AbilityBalanceContentFingerprint
{
    public const int FingerprintContractVersion = 3;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string Create(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions)
    {
        var catalog = catalogProvider.GetCatalog();
        var content = JsonSerializer.Serialize(new
        {
            FingerprintContractVersion,
            Combat = CreateCombatProjection(catalogProvider, essenceDefinitions),
            EquipmentStatBudgetCatalog.BalanceVersion
        }, JsonOptions);
        return Hash(content);
    }

    private static object CreateCombatProjection(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions)
    {
        var catalog = catalogProvider.GetCatalog();
        return new
        {
            catalog.Abilities,
            catalog.Statuses,
            catalog.Summons,
            catalog.AbilityIdsByOwningEssence,
            Essences = essenceDefinitions?.GetAll()
                .OrderBy(definition => definition.Id, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
