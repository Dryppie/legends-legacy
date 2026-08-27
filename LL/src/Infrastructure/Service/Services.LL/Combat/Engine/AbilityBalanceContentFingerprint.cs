using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.WorldTower;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Profiles;
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

    public static string CreateDiscovery(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository essenceDefinitions,
        CanonicalEquipmentBuildFactory canonicalBuilds,
        int equipmentTier,
        string equipmentRarity,
        string equipmentProfile)
    {
        if (!Enum.TryParse<Rarity>(equipmentRarity, true, out var rarity)
            || rarity > Rarity.Legendary)
            throw new ArgumentException($"Unknown discovery rarity '{equipmentRarity}'.", nameof(equipmentRarity));
        if (!Enum.TryParse<CanonicalPartyProfile>(equipmentProfile, true, out var profile))
            throw new ArgumentException($"Unknown discovery profile '{equipmentProfile}'.", nameof(equipmentProfile));
        var rung = canonicalBuilds.GetProgressionLadder().Single(candidate =>
            candidate.Tier == equipmentTier
            && candidate.Rarity == rarity
            && candidate.Quality == ItemQuality.Standard);
        var roleBuilds = CanonicalCooperativeRosterCatalog.CreateParty(5)
            .Select(slot => new
            {
                slot.SlotIndex,
                slot.Role,
                Build = CreateBuildProjection(canonicalBuilds.CreateBuild(
                    slot.Role,
                    rung,
                    essenceCount: 0))
            })
            .ToArray();
        return Hash(JsonSerializer.Serialize(new
        {
            FingerprintContractVersion,
            Purpose = "EssenceDiscovery",
            Combat = CreateCombatProjection(catalogProvider, essenceDefinitions),
            AuditAlgorithmVersion = AbilityBalanceAuditService.AlgorithmVersion,
            SimulatorAlgorithmVersion = AbilityBalanceSimulator.AlgorithmVersion,
            PowerRatingAlgorithm.CombatRulesVersion,
            CombatPreparationPipeline.SchemaVersion,
            CanonicalRosterVersion = CanonicalCooperativeRosterCatalog.Version,
            RequestedEquipmentProfile = profile,
            CanonicalRoleEquipment = roleBuilds
        }, JsonOptions));
    }

    public static string CreateMaterialization(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository essenceDefinitions,
        CanonicalEquipmentBuildFactory canonicalBuilds,
        IReadOnlyList<WorldTowerProfileScenarioRequirement> requirements,
        IReadOnlyList<TowerFloorDefinition> floors,
        IReadOnlyList<Creature> guardians,
        ICreatureAbilityDefinitionProvider creatureAbilities,
        ICreatureEssenceLootTableRepository creatureEssences,
        IRegionCreatureScalingProvider regionScaling)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(floors);
        ArgumentNullException.ThrowIfNull(guardians);
        var builds = requirements
            .OrderBy(requirement => requirement.ScenarioId, StringComparer.Ordinal)
            .Select(requirement =>
            {
                var rarity = Enum.Parse<Rarity>(requirement.EquipmentRarity, true);
                var quality = Enum.Parse<ItemQuality>(requirement.EquipmentQuality, true);
                var rung = canonicalBuilds.GetProgressionLadder().Single(candidate =>
                    candidate.Tier == requirement.EquipmentTier
                    && candidate.Rarity == rarity
                    && candidate.Quality == quality);
                return new
                {
                    requirement.ScenarioId,
                    requirement.FloorNumbers,
                    requirement.TeamSize,
                    requirement.EssencesPerParticipant,
                    Roles = CanonicalCooperativeRosterCatalog.CreateParty(requirement.TeamSize)
                        .Select(slot => new
                        {
                            slot.SlotIndex,
                            slot.PartyNumber,
                            slot.Role,
                            Build = CreateBuildProjection(canonicalBuilds.CreateBuild(
                                slot.Role,
                                rung,
                                requirement.EssencesPerParticipant))
                        })
                        .ToArray()
                };
            })
            .ToArray();
        return Hash(JsonSerializer.Serialize(new
        {
            FingerprintContractVersion,
            Purpose = "ProfileMaterialization",
            Combat = CreateCombatProjection(catalogProvider, essenceDefinitions),
            ProfileSchemaVersion = CombatCharacterProfileService.SchemaVersion,
            ProfileGeneratorVersion = CombatCharacterProfileService.GeneratorVersion,
            ProfileTargetContractVersion = WorldTowerProfileTargetContract.Version,
            TowerQualificationContractVersion = WorldTowerProfileCandidateQualifier.ContractVersion,
            TowerQualificationSampleCount = 10,
            PowerRatingVersion = PowerRatingAlgorithm.Version,
            CombatRulesVersion = PowerRatingAlgorithm.CombatRulesVersion,
            PreparationSchemaVersion = CombatPreparationPipeline.SchemaVersion,
            CanonicalRosterVersion = CanonicalCooperativeRosterCatalog.Version,
            Builds = builds,
            TowerFloors = floors.OrderBy(floor => floor.FloorNumber).ToArray(),
            Guardians = guardians
                .OrderBy(guardian => guardian.Id)
                .Select(guardian =>
                {
                    var monsterDefinitionId = CreatureEssenceSource.GetMonsterDefinitionId(guardian);
                    return new
                    {
                        guardian.Id,
                        guardian.Name,
                        guardian.Level,
                        guardian.BaseLevel,
                        guardian.Tier,
                        guardian.Archetype,
                        guardian.DamageProfile,
                        guardian.DefenseProfile,
                        guardian.RewardTableId,
                        BaseAttributes = guardian.BaseAttributes
                            .OrderBy(attribute => attribute.AttributeType)
                            .Select(attribute => new { attribute.AttributeType, attribute.Value })
                            .ToArray(),
                        StatOverrides = guardian.StatOverrides
                            .OrderBy(stat => stat.AttributeType)
                            .ThenBy(stat => stat.Id)
                            .Select(stat => new
                            {
                                stat.Id,
                                stat.AttributeType,
                                stat.Multiplier,
                                stat.Additive
                            })
                            .ToArray(),
                        MonsterDefinitionId = monsterDefinitionId,
                        NativeAbilityIds = creatureAbilities.GetAbilityIds(monsterDefinitionId)
                            .Order(StringComparer.Ordinal)
                            .ToArray(),
                        EssenceLootTable = creatureEssences.GetByCreatureId(monsterDefinitionId)
                    };
                })
                .ToArray(),
            RegionScaling = regionScaling.GetCatalog()
        }, JsonOptions));
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

    private static object CreateBuildProjection(CanonicalEquipmentBuild build) => new
    {
        build.Rung,
        build.Profile,
        build.EquipmentBalanceVersion,
        build.MainHandRecipeId,
        Character = new
        {
            build.Character.Level,
            BaseAttributes = build.Character.BaseAttributes
                .OrderBy(attribute => attribute.AttributeType)
                .Select(attribute => new { attribute.AttributeType, attribute.Value })
                .ToArray()
        },
        Equipment = build.Equipment
            .OrderBy(item => item.EquipmentBase.EquipmentType)
            .ThenBy(item => item.ItemBaseId, StringComparer.Ordinal)
            .Select(item => new
            {
                item.ItemBaseId,
                item.EquipmentBase.EquipmentType,
                item.BaseRecipeId,
                item.BlueprintId,
                item.EquipmentSetId,
                item.Tier,
                item.Rarity,
                item.Quality,
                item.StatModelVersion,
                item.Potential,
                item.MaxPotential,
                item.TemperingProgress,
                AffinityTags = item.AffinityTags.Order(StringComparer.Ordinal).ToArray(),
                Modifiers = item.AttributeModifiers
                    .OrderBy(modifier => modifier.AttributeType)
                    .ThenBy(modifier => modifier.ModifierType)
                    .Select(modifier => new
                    {
                        modifier.AttributeType,
                        modifier.ModifierType,
                        modifier.Amount
                    })
                    .ToArray()
            })
            .ToArray(),
        build.Rating
    };

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
