using Application.Interfaces.Services.LL.Essences;

namespace Application.Interfaces.Services.LL.CombatProfiles;

public interface ICombatCharacterProfileService
{
    Task<CombatCharacterProfileGenerationReport> GenerateAsync(
        CombatCharacterProfileGenerationRequest request,
        CancellationToken cancellationToken);
}

public interface ICombatCharacterProfileBatchService
{
    Task<CombatCharacterProfileBatchGenerationReport> GenerateCatalogAsync(
        CombatCharacterProfileBatchGenerationRequest request,
        CancellationToken cancellationToken);
}

public sealed record CombatCharacterProfileGenerationRequest(
    string AuditId,
    AbilityBalanceAuditReport Audit,
    string ContentType = "WorldTower",
    string EquipmentQuality = "Standard",
    int TeamsPerFamily = 1,
    int RandomSeed = 1337,
    string PortfolioMode = "Expanded",
    int MinimumSourceBattles = 100,
    int MinimumMatchupBattles = 100,
    double MaximumConfidenceWidth95 = 0.25d,
    double MaximumSeedScoreSpread = 0.15d,
    double MaximumEssenceOverlap = 0.80d,
    bool RequireMultiSeedStability = true,
    int? TargetTeamSize = null,
    int? TargetEquipmentTier = null,
    string? TargetEquipmentRarity = null,
    IReadOnlyList<int>? TargetFloorNumbers = null,
    int ContextQualificationSampleCount = 10);

public sealed record CombatCharacterProfileGenerationReport(
    int SchemaVersion,
    int GeneratorVersion,
    int PowerRatingAlgorithmVersion,
    int CombatRulesVersion,
    int EquipmentBalanceVersion,
    int CanonicalRosterVersion,
    string AuditId,
    string SourceContentHash,
    string ContentType,
    int RandomSeed,
    IReadOnlyList<CombatCharacterProfileTeam> Teams,
    string PortfolioMode = "Core",
    int MinimumSourceBattles = 0,
    int MinimumMatchupBattles = 0,
    double MaximumConfidenceWidth95 = 1d,
    double MaximumSeedScoreSpread = 1d,
    double MaximumEssenceOverlap = 1d,
    bool RequireMultiSeedStability = false,
    CombatCharacterProfileScenario? Scenario = null);

public sealed record CombatCharacterProfileScenario(
    string Id,
    int TeamSize,
    int EquipmentTier,
    string EquipmentRarity,
    string EquipmentQuality,
    string AuditEquipmentProfile,
    int EssencesPerParticipant,
    int PartySize = 5,
    int PartyCount = 1,
    int DiscoveryTeamSize = 5,
    IReadOnlyList<int>? FloorNumbers = null)
{
    public static string CreateId(
        string contentType,
        int teamSize,
        int equipmentTier,
        string equipmentRarity,
        string equipmentQuality,
        string auditEquipmentProfile,
        int essencesPerParticipant) => string.Join('.',
        "scenario",
        Normalize(contentType),
        $"team-{teamSize}",
        $"tier-{equipmentTier}",
        Normalize(equipmentRarity),
        Normalize(equipmentQuality),
        Normalize(auditEquipmentProfile),
        $"essences-{essencesPerParticipant}");

    private static string Normalize(string value) =>
        new(value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
}

public sealed record CombatCharacterProfileBatchGenerationRequest(
    IReadOnlyList<CombatCharacterProfileGenerationRequest> Requests);

public sealed record CombatCharacterProfileBatchGenerationReport(
    int RequestedScenarioCount,
    CombatCharacterProfileCatalogValidationReport CatalogValidation);

public sealed record CombatCharacterProfileTeam(
    string Id,
    string Family,
    string SourceSignature,
    string SourceDisplayName,
    int SourceBattles,
    int SourceWins,
    int SourceLosses,
    int SourceDraws,
    double SourceScore,
    double ConfidenceLower95,
    double ConfidenceUpper95,
    IReadOnlyList<CombatCharacterProfile> Profiles,
    string SelectionReason = "",
    bool IsSyntheticControl = false,
    string? AdversarySourceSignature = null,
    double? SeedScoreMinimum = null,
    double? SeedScoreMaximum = null,
    double? NearestSelectedEssenceOverlap = null,
    int? AdversaryBattles = null,
    double? AdversaryScore = null,
    double? AdversaryConfidenceLower95 = null,
    double? AdversaryConfidenceUpper95 = null,
    IReadOnlyList<CombatCharacterProfileParty>? Parties = null,
    bool IsComposedExpedition = false);

public sealed record CombatCharacterProfileParty(
    string Id,
    string SourcePartyProfileId,
    int PartyNumber,
    IReadOnlyList<string> ProfileIds,
    CombatCharacterProfilePartyEvidence Evidence);

public sealed record CombatCharacterProfilePartyEvidence(
    string Family,
    string SourceSignature,
    string SourceDisplayName,
    int SourceBattles,
    int SourceWins,
    int SourceLosses,
    int SourceDraws,
    double SourceScore,
    double ConfidenceLower95,
    double ConfidenceUpper95,
    string SelectionReason,
    bool IsSyntheticControl,
    string? AdversarySourceSignature,
    double? SeedScoreMinimum,
    double? SeedScoreMaximum,
    double NearestSelectedEssenceOverlap,
    int? AdversaryBattles,
    double? AdversaryScore,
    double? AdversaryConfidenceLower95,
    double? AdversaryConfidenceUpper95,
    IReadOnlyList<CombatCharacterProfileContextEvidence>? ContextEvidence = null);

public sealed record CombatCharacterProfileContextEvidence(
    string ScenarioId,
    int FloorNumber,
    int TargetTeamSize,
    int SampleCount,
    int Wins,
    int Losses,
    int Draws,
    double WinRate,
    double TimeoutRate,
    double AverageDurationTicks,
    string SeedManifestId,
    string SeedManifestHash,
    bool UsesProductionRuntime,
    bool AbilitiesStartOnCooldown);

public sealed record CombatCharacterProfile(
    string Id,
    string TeamId,
    int SlotIndex,
    string Name,
    string Family,
    string Role,
    string ContentType,
    int EquipmentTier,
    string EquipmentRarity,
    string EquipmentQuality,
    string EquipmentProfile,
    IReadOnlyList<string> EssenceIds,
    int RawPowerRating,
    int DisplayPowerRating,
    CombatCharacterPreparedPreview Prepared,
    int PartyNumber = 1,
    int PartySlotIndex = 0,
    string? SourcePartyProfileId = null);

public sealed record CombatCharacterPreparedPreview(
    bool IsProductionReady,
    int Level,
    int CurrentHealth,
    int MaxHealth,
    IReadOnlyDictionary<string, int> Attributes,
    IReadOnlyList<string> AbilityIds,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> EssenceIds,
    IReadOnlyList<CombatCharacterPreparedEquipment> Equipment);

public sealed record CombatCharacterPreparedEquipment(
    string ItemBaseId,
    string Slot,
    int Tier,
    string Rarity,
    string Quality,
    string? RecipeId,
    string? BlueprintId,
    string? EquipmentSetId);
