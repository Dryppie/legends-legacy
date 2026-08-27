using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Profiles;

public sealed class JsonCombatCharacterProfileCatalogService(
    string catalogPath,
    JsonSerializerOptions jsonOptions,
    CanonicalEquipmentBuildFactory canonicalBuilds,
    CombatCharacterProfileMaterializer materializer,
    IEssenceDefinitionRepository essenceDefinitions,
    IAbilityCatalogProvider abilityCatalog) : ICombatCharacterProfileCatalogService
{
    public const int SchemaVersion = 1;
    public const int CatalogVersion = 1;

    public async Task<CombatCharacterProfileCatalogValidationReport> GetApprovedAsync(
        CancellationToken cancellationToken)
    {
        var empty = EmptyCatalog();
        if (!File.Exists(catalogPath))
        {
            return InvalidLoadResult(
                empty,
                "CatalogNotFound",
                $"The approved combat character profile catalog was not found at '{catalogPath}'.");
        }

        try
        {
            await using var stream = File.OpenRead(catalogPath);
            var catalog = await JsonSerializer.DeserializeAsync<CombatCharacterProfileCatalogDocument>(
                stream,
                jsonOptions,
                cancellationToken);
            return catalog is null
                ? InvalidLoadResult(empty, "CatalogEmpty", "The approved profile catalog is empty.")
                : await ValidateAsync(catalog, cancellationToken);
        }
        catch (JsonException exception)
        {
            return InvalidLoadResult(
                empty,
                "CatalogJsonInvalid",
                $"The approved profile catalog contains invalid JSON: {exception.Message}");
        }
        catch (IOException exception)
        {
            return InvalidLoadResult(
                empty,
                "CatalogReadFailed",
                $"The approved profile catalog could not be read: {exception.Message}");
        }
    }

    public async Task<CombatCharacterProfileCatalogValidationReport> ValidateAsync(
        CombatCharacterProfileCatalogDocument catalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var issues = new List<CombatCharacterProfileCatalogValidationIssue>();
        var currentContentHash = AbilityBalanceContentFingerprint.Create(
            abilityCatalog,
            essenceDefinitions);

        CheckVersion(
            catalog.SchemaVersion,
            SchemaVersion,
            "SchemaVersionMismatch",
            "$.schemaVersion",
            "catalog schema",
            issues);
        CheckVersion(
            catalog.CatalogVersion,
            CatalogVersion,
            "CatalogVersionMismatch",
            "$.catalogVersion",
            "catalog",
            issues);

        var profileSets = catalog.ProfileSets ?? [];
        if (profileSets.Count == 0)
        {
            issues.Add(Issue(
                "Warning",
                "CatalogHasNoProfiles",
                "$.profileSets",
                "The catalog is structurally valid but does not contain any approved profile sets."));
        }
        if (profileSets.Any(set => set is null))
        {
            issues.Add(Issue(
                "Error",
                "ProfileSetNull",
                "$.profileSets",
                "Profile sets cannot contain null entries."));
        }
        var populatedSets = profileSets.Where(set => set is not null).ToArray();

        AddDuplicateIssues(
            populatedSets.Select(ProfileSetScenarioKey).OfType<string>(),
            "DuplicateProfileScenario",
            "$.profileSets",
            "content and equipment scenario",
            issues);
        AddDuplicateIssues(
            populatedSets.SelectMany(set => set.Teams ?? []).Where(team => team is not null).Select(team => team.Id),
            "DuplicateTeamId",
            "$.profileSets",
            "team ID",
            issues);
        AddDuplicateIssues(
            populatedSets
                .SelectMany(set => set.Teams ?? [])
                .Where(team => team is not null)
                .SelectMany(team => team.Profiles ?? [])
                .Where(profile => profile is not null)
                .Select(profile => profile.Id),
            "DuplicateProfileId",
            "$.profileSets",
            "profile ID",
            issues);

        var normalizedSets = new List<CombatCharacterProfileGenerationReport>(populatedSets.Length);
        for (var setIndex = 0; setIndex < populatedSets.Length; setIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            normalizedSets.Add(await ValidateSetAsync(
                populatedSets[setIndex],
                setIndex,
                currentContentHash,
                issues,
                cancellationToken));
        }

        var normalized = new CombatCharacterProfileCatalogDocument(
            SchemaVersion,
            CatalogVersion,
            normalizedSets
                .OrderBy(set => set.ContentType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(set => set.Scenario?.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(set => set.AuditId, StringComparer.OrdinalIgnoreCase)
                .ToArray());
        return new CombatCharacterProfileCatalogValidationReport(
            issues.All(issue => !issue.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase)),
            currentContentHash,
            normalized,
            issues);
    }

    private async Task<CombatCharacterProfileGenerationReport> ValidateSetAsync(
        CombatCharacterProfileGenerationReport set,
        int setIndex,
        string currentContentHash,
        List<CombatCharacterProfileCatalogValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        var path = $"$.profileSets[{setIndex}]";
        CheckVersion(set.SchemaVersion, CombatCharacterProfileService.SchemaVersion,
            "ProfileSchemaVersionMismatch", $"{path}.schemaVersion", "profile schema", issues);
        CheckVersion(set.GeneratorVersion, CombatCharacterProfileService.GeneratorVersion,
            "GeneratorVersionMismatch", $"{path}.generatorVersion", "profile generator", issues);
        CheckVersion(set.PowerRatingAlgorithmVersion, PowerRatingAlgorithm.Version,
            "PowerRatingVersionMismatch", $"{path}.powerRatingAlgorithmVersion", "Power Rating algorithm", issues);
        CheckVersion(set.CombatRulesVersion, PowerRatingAlgorithm.CombatRulesVersion,
            "CombatRulesVersionMismatch", $"{path}.combatRulesVersion", "combat rules", issues);
        CheckVersion(set.EquipmentBalanceVersion, EquipmentStatBudgetCatalog.BalanceVersion,
            "EquipmentBalanceVersionMismatch", $"{path}.equipmentBalanceVersion", "equipment balance", issues);
        CheckVersion(set.CanonicalRosterVersion, CanonicalCooperativeRosterCatalog.Version,
            "CanonicalRosterVersionMismatch", $"{path}.canonicalRosterVersion", "canonical roster", issues);

        if (string.IsNullOrWhiteSpace(set.AuditId))
            issues.Add(Issue("Error", "AuditIdMissing", $"{path}.auditId", "An audit ID is required."));
        if (!string.Equals(set.SourceContentHash, currentContentHash, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Issue(
                "Error",
                "ContentHashStale",
                $"{path}.sourceContentHash",
                "The profile set was generated from different combat content. Run a new audit and replace this set."));
        }
        if (!Enum.TryParse<CombatContentType>(set.ContentType, true, out var contentType))
        {
            issues.Add(Issue(
                "Error",
                "ContentTypeInvalid",
                $"{path}.contentType",
                $"'{set.ContentType}' is not a supported combat content type."));
            return set;
        }
        if (!string.Equals(set.PortfolioMode, "Core", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(set.PortfolioMode, "Expanded", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Issue(
                "Error",
                "PortfolioModeInvalid",
                $"{path}.portfolioMode",
                $"'{set.PortfolioMode}' is not a supported profile portfolio mode."));
        }
        if (set.MinimumSourceBattles <= 0
            || set.MinimumMatchupBattles < 0
            || !IsUnitInterval(set.MaximumConfidenceWidth95)
            || !IsUnitInterval(set.MaximumSeedScoreSpread)
            || !IsUnitInterval(set.MaximumEssenceOverlap))
        {
            issues.Add(Issue(
                "Error",
                "PortfolioSafeguardsInvalid",
                path,
                "Portfolio evidence and diversity safeguards are missing or outside their supported ranges."));
        }

        var suppliedTeams = set.Teams ?? [];
        if (suppliedTeams.Any(team => team is null))
            issues.Add(Issue("Error", "TeamNull", $"{path}.teams", "Teams cannot contain null entries."));
        var teams = suppliedTeams.Where(team => team is not null).ToArray();
        var isWorldTowerCalibrationPortfolio = contentType == CombatContentType.WorldTower
            && teams.Any(team => IsDirectContextFamily(team.Family))
            && teams.All(team => IsDirectContextFamily(team.Family)
                               || string.Equals(
                                   team.Family,
                                   "NoEssence",
                                   StringComparison.OrdinalIgnoreCase));
        var requiredFamilies = new[] { "Meta", "Typical", "WeakButLegal" };
        foreach (var family in requiredFamilies.Where(family =>
                     !isWorldTowerCalibrationPortfolio
                     && teams.All(team => !string.Equals(team.Family, family, StringComparison.OrdinalIgnoreCase))))
        {
            issues.Add(Issue(
                "Error",
                "RequiredFamilyMissing",
                $"{path}.teams",
                $"The profile set does not contain a {family} control family."));
        }
        if (string.Equals(set.PortfolioMode, "Expanded", StringComparison.OrdinalIgnoreCase))
        {
            string[] expandedFamilies = contentType == CombatContentType.WorldTower
                ? set.Scenario?.PartyCount > 1
                    ?
                    [
                        "Budget",
                        "Counter",
                        "Countered",
                        "EqualPowerAdversarial",
                        "NoEssence",
                        "Mixed.MetaTypical",
                        "Mixed.RoleSpecialist"
                    ]
                    :
                    [
                        "Budget",
                        "Counter",
                        "Countered",
                        "EqualPowerAdversarial",
                        "NoEssence",
                        "RoleSpecialist.Controller"
                    ]
                :
                [
                    "Budget",
                    "Counter",
                    "Countered",
                    "EqualPowerAdversarial",
                    "NoEssence",
                    .. Enum.GetNames<CanonicalCooperativeRole>().Select(role => $"RoleSpecialist.{role}")
                ];
            foreach (var family in expandedFamilies.Where(family =>
                         !isWorldTowerCalibrationPortfolio
                         && teams.All(team => !string.Equals(team.Family, family, StringComparison.OrdinalIgnoreCase))))
            {
                issues.Add(Issue(
                    "Error",
                    "ExpandedFamilyMissing",
                    $"{path}.teams",
                    $"The expanded portfolio does not contain the {family} family."));
            }
            if (contentType == CombatContentType.WorldTower && teams.Length is < 5 or > 12)
            {
                issues.Add(Issue(
                    "Error",
                    "WorldTowerPortfolioSizeInvalid",
                    $"{path}.teams",
                    "An expanded World Tower scenario must contain between five and twelve bounded expedition profiles."));
            }
        }

        var sourceSignatures = teams
            .SelectMany(team => (team.Parties ?? [])
                .Select(party => party.Evidence?.SourceSignature)
                .Append(team.IsComposedExpedition ? null : team.SourceSignature))
            .Where(signature => !string.IsNullOrWhiteSpace(signature))
            .Select(signature => signature!)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var duplicate in teams
                     .GroupBy(team => team.SourceSignature, StringComparer.Ordinal)
                     .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1))
        {
            issues.Add(Issue(
                "Error",
                "SourceSignatureDuplicate",
                $"{path}.teams",
                $"Source signature '{duplicate.Key}' is selected more than once."));
        }
        foreach (var team in teams.Where(team => !string.IsNullOrWhiteSpace(team.AdversarySourceSignature)
                                                 && !sourceSignatures.Contains(team.AdversarySourceSignature!)))
        {
            issues.Add(Issue(
                "Error",
                "AdversaryMissing",
                $"{path}.teams",
                $"Team '{team.Id}' references adversary source '{team.AdversarySourceSignature}', which is not included in the profile set."));
        }
        foreach (var party in teams.SelectMany(team => team.Parties ?? [])
                     .Where(party => !string.IsNullOrWhiteSpace(party.Evidence?.AdversarySourceSignature)
                                     && !sourceSignatures.Contains(party.Evidence.AdversarySourceSignature!)))
        {
            issues.Add(Issue(
                "Error",
                "PartyAdversaryMissing",
                $"{path}.teams",
                $"Party source '{party.Evidence.SourceSignature}' references adversary '{party.Evidence.AdversarySourceSignature}', which is not included in the profile set."));
        }

        var normalizedTeams = new List<CombatCharacterProfileTeam>(teams.Length);
        for (var teamIndex = 0; teamIndex < teams.Length; teamIndex++)
        {
            var team = teams[teamIndex];
            var teamPath = $"{path}.teams[{teamIndex}]";
            normalizedTeams.Add(await ValidateTeamAsync(
                team,
                teamPath,
                contentType,
                set,
                issues,
                cancellationToken));
        }
        var normalizedScenario = ValidateScenario(
            set.Scenario,
            contentType,
            normalizedTeams,
            path,
            issues);

        return set with
        {
            SchemaVersion = CombatCharacterProfileService.SchemaVersion,
            GeneratorVersion = CombatCharacterProfileService.GeneratorVersion,
            PowerRatingAlgorithmVersion = PowerRatingAlgorithm.Version,
            CombatRulesVersion = PowerRatingAlgorithm.CombatRulesVersion,
            EquipmentBalanceVersion = EquipmentStatBudgetCatalog.BalanceVersion,
            CanonicalRosterVersion = CanonicalCooperativeRosterCatalog.Version,
            ContentType = contentType.ToString(),
            Scenario = normalizedScenario,
            Teams = normalizedTeams
                .OrderBy(team => FamilyOrder(team.Family))
                .ThenBy(team => team.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static CombatCharacterProfileScenario? ValidateScenario(
        CombatCharacterProfileScenario? scenario,
        CombatContentType contentType,
        IReadOnlyList<CombatCharacterProfileTeam> teams,
        string path,
        List<CombatCharacterProfileCatalogValidationIssue> issues)
    {
        var scenarioPath = $"{path}.scenario";
        if (scenario is null)
        {
            issues.Add(Issue(
                "Error",
                "ScenarioMissing",
                scenarioPath,
                "An explicit profile calibration scenario is required."));
            return null;
        }
        if (scenario.TeamSize is < 1 or > 100
            || scenario.EquipmentTier < 1
            || scenario.EssencesPerParticipant is < 0 or > 10
            || scenario.PartySize is < 1 or > 5
            || scenario.PartyCount < 1
            || scenario.DiscoveryTeamSize != scenario.PartySize
            || scenario.TeamSize != scenario.PartySize * scenario.PartyCount)
        {
            issues.Add(Issue(
                "Error",
                "ScenarioRangeInvalid",
                scenarioPath,
                "Scenario team size, party layout, equipment tier, or Essence-slot count is outside the supported range."));
        }
        if (contentType == CombatContentType.WorldTower
            && (scenario.PartySize != 5 || scenario.TeamSize is not (5 or 10 or 15)))
        {
            issues.Add(Issue(
                "Error",
                "WorldTowerPartyLayoutInvalid",
                scenarioPath,
                "World Tower scenarios require one, two, or three complete five-character parties."));
        }
        if ((scenario.FloorNumbers ?? []).Any(floorNumber => floorNumber <= 0)
            || (scenario.FloorNumbers ?? []).Distinct().Count() != (scenario.FloorNumbers ?? []).Count)
        {
            issues.Add(Issue(
                "Error",
                "ScenarioFloorCoverageInvalid",
                $"{scenarioPath}.floorNumbers",
                "Scenario floor coverage must contain distinct positive floor numbers."));
        }
        if (!Enum.TryParse<Rarity>(scenario.EquipmentRarity, true, out var rarity)
            || !Enum.TryParse<ItemQuality>(scenario.EquipmentQuality, true, out var quality)
            || !Enum.TryParse<CanonicalPartyProfile>(scenario.AuditEquipmentProfile, true, out var auditProfile))
        {
            issues.Add(Issue(
                "Error",
                "ScenarioEquipmentInvalid",
                scenarioPath,
                "Scenario rarity, quality, or source-audit equipment profile is invalid."));
            return scenario;
        }

        var expectedId = CombatCharacterProfileScenario.CreateId(
            contentType.ToString(),
            scenario.TeamSize,
            scenario.EquipmentTier,
            rarity.ToString(),
            quality.ToString(),
            auditProfile.ToString(),
            scenario.EssencesPerParticipant,
            contentType == CombatContentType.WorldTower && (scenario.FloorNumbers ?? []).Count == 1
                ? scenario.FloorNumbers![0]
                : null);
        if (!string.Equals(scenario.Id, expectedId, StringComparison.Ordinal))
        {
            issues.Add(Issue(
                "Error",
                "ScenarioIdInvalid",
                $"{scenarioPath}.id",
                $"Scenario ID must be '{expectedId}'."));
        }

        foreach (var team in teams)
        {
            if (team.Profiles.Count != scenario.TeamSize)
            {
                issues.Add(Issue(
                    "Error",
                    "ScenarioTeamSizeMismatch",
                    $"{path}.teams",
                    $"Team '{team.Id}' contains {team.Profiles.Count} profiles; scenario '{expectedId}' requires {scenario.TeamSize}."));
            }
            foreach (var profile in team.Profiles)
            {
                if (profile.EquipmentTier != scenario.EquipmentTier
                    || !string.Equals(profile.EquipmentRarity, rarity.ToString(), StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(profile.EquipmentQuality, quality.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(Issue(
                        "Error",
                        "ScenarioEquipmentMismatch",
                        $"{path}.teams",
                        $"Profile '{profile.Id}' does not match scenario '{expectedId}' equipment."));
                }
                var expectedEssenceCount = string.Equals(team.Family, "NoEssence", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : scenario.EssencesPerParticipant;
                if (profile.EssenceIds.Count != expectedEssenceCount)
                {
                    issues.Add(Issue(
                        "Error",
                        "ScenarioEssenceCountMismatch",
                        $"{path}.teams",
                        $"Profile '{profile.Id}' has {profile.EssenceIds.Count} Essences; family '{team.Family}' requires {expectedEssenceCount} for scenario '{expectedId}'."));
                }
                var expectedPartyNumber = profile.SlotIndex / scenario.PartySize + 1;
                var expectedPartySlotIndex = profile.SlotIndex % scenario.PartySize;
                if (profile.PartyNumber != expectedPartyNumber
                    || profile.PartySlotIndex != expectedPartySlotIndex)
                {
                    issues.Add(Issue(
                        "Error",
                        "ScenarioPartyAssignmentMismatch",
                        $"{path}.teams",
                        $"Profile '{profile.Id}' must be party {expectedPartyNumber}, slot {expectedPartySlotIndex}."));
                }
            }
        }

        return scenario with
        {
            Id = expectedId,
            EquipmentRarity = rarity.ToString(),
            EquipmentQuality = quality.ToString(),
            AuditEquipmentProfile = auditProfile.ToString()
        };
    }

    private async Task<CombatCharacterProfileTeam> ValidateTeamAsync(
        CombatCharacterProfileTeam team,
        string path,
        CombatContentType contentType,
        CombatCharacterProfileGenerationReport profileSet,
        List<CombatCharacterProfileCatalogValidationIssue> issues,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(team.Id))
            issues.Add(Issue("Error", "TeamIdMissing", $"{path}.id", "A team ID is required."));
        if (string.IsNullOrWhiteSpace(team.Family))
            issues.Add(Issue("Error", "FamilyMissing", $"{path}.family", "A profile family is required."));
        if (string.IsNullOrWhiteSpace(team.SelectionReason))
            issues.Add(Issue("Error", "SelectionReasonMissing", $"{path}.selectionReason", "A selection reason is required."));
        if (string.IsNullOrWhiteSpace(team.SourceSignature)
            || string.IsNullOrWhiteSpace(team.SourceDisplayName))
        {
            issues.Add(Issue(
                "Error",
                "SourceEvidenceMissing",
                path,
                "Source signature and display name are required for audit provenance."));
        }
        var suppliedProfiles = team.Profiles ?? [];
        if (suppliedProfiles.Any(profile => profile is null))
            issues.Add(Issue("Error", "ProfileNull", $"{path}.profiles", "Profiles cannot contain null entries."));
        var profiles = suppliedProfiles.Where(profile => profile is not null).ToArray();
        if (profiles.Length == 0)
        {
            issues.Add(Issue("Error", "TeamEmpty", $"{path}.profiles", "A profile team cannot be empty."));
            return team;
        }
        ValidateFamilySemantics(team, profiles, profileSet, path, issues);
        ValidateParties(team, profiles, profileSet, path, issues);
        if (team.IsComposedExpedition)
        {
            if (profileSet.Scenario?.PartyCount is null or <= 1)
            {
                issues.Add(Issue(
                    "Error",
                    "ComposedExpeditionLayoutInvalid",
                    path,
                    "A composed expedition requires a scenario containing at least two parties."));
            }
            if (team.SourceBattles != 0
                || team.SourceWins != 0
                || team.SourceLosses != 0
                || team.SourceDraws != 0
                || team.SourceScore != 0d
                || team.ConfidenceLower95 != 0d
                || team.ConfidenceUpper95 != 1d
                || team.SeedScoreMinimum is not null
                || team.SeedScoreMaximum is not null
                || team.NearestSelectedEssenceOverlap is not null
                || team.AdversaryBattles is not null
                || team.AdversaryScore is not null
                || team.AdversaryConfidenceLower95 is not null
                || team.AdversaryConfidenceUpper95 is not null)
            {
                issues.Add(Issue(
                    "Error",
                    "ComposedExpeditionClaimsDirectEvidence",
                    path,
                    "A composed expedition must use neutral team evidence; its audited evidence belongs to the constituent parties."));
            }
        }
        else if (team.IsSyntheticControl)
        {
            if (team.SourceBattles != 0
                || team.SourceWins != 0
                || team.SourceLosses != 0
                || team.SourceDraws != 0)
            {
                issues.Add(Issue(
                    "Error",
                    "SyntheticEvidenceInvalid",
                    path,
                    "Synthetic controls cannot claim source battle outcomes."));
            }
        }
        else if (IsDirectContextTeam(team))
        {
            if (team.SourceWins != 0
                || team.SourceLosses != 0
                || team.SourceDraws != 0
                || team.SourceScore != 0d
                || team.ConfidenceLower95 != 0d
                || team.ConfidenceUpper95 != 1d
                || team.SeedScoreMinimum is not null
                || team.SeedScoreMaximum is not null
                || team.AdversaryBattles is not null
                || team.AdversaryScore is not null
                || team.AdversaryConfidenceLower95 is not null
                || team.AdversaryConfidenceUpper95 is not null)
            {
                issues.Add(Issue(
                    "Error",
                    "DirectContextAnchorClaimsAuditEvidence",
                    path,
                    "A direct Tower calibration anchor must keep PvP audit evidence neutral; its evidence belongs to the exact Tower context."));
            }
            if (team.NearestSelectedEssenceOverlap is null
                || !double.IsFinite(team.NearestSelectedEssenceOverlap.Value)
                || team.NearestSelectedEssenceOverlap is < 0d or > 1d)
            {
                issues.Add(Issue(
                    "Error",
                    "EssenceDiversityInvalid",
                    path,
                    "The direct Tower anchor's recorded Essence overlap is invalid."));
            }
        }
        else if (team.SourceBattles <= 0
            || team.SourceWins < 0
            || team.SourceLosses < 0
            || team.SourceDraws < 0
            || team.SourceWins + team.SourceLosses + team.SourceDraws != team.SourceBattles)
        {
            issues.Add(Issue(
                "Error",
                "SourceBattleCountsInvalid",
                path,
                "Source wins, losses, and draws must be non-negative and add up to source battles."));
        }
        else
        {
            var expectedScore = (team.SourceWins + team.SourceDraws * 0.5d) / team.SourceBattles;
            if (!double.IsFinite(team.SourceScore) || Math.Abs(team.SourceScore - expectedScore) > 0.0000001d)
            {
                issues.Add(Issue(
                    "Error",
                    "SourceScoreInvalid",
                    $"{path}.sourceScore",
                    "The source score does not match the recorded battle outcome counts."));
            }
            if (!double.IsFinite(team.ConfidenceLower95)
                || !double.IsFinite(team.ConfidenceUpper95)
                || team.ConfidenceLower95 < 0d
                || team.ConfidenceUpper95 > 1d
                || team.ConfidenceLower95 > expectedScore
                || team.ConfidenceUpper95 < expectedScore)
            {
                issues.Add(Issue(
                    "Error",
                    "SourceConfidenceInvalid",
                    path,
                    "The source confidence interval must be finite, bounded, and contain the source score."));
            }
            if (team.SourceBattles < profileSet.MinimumSourceBattles
                || team.ConfidenceUpper95 - team.ConfidenceLower95 > profileSet.MaximumConfidenceWidth95)
            {
                issues.Add(Issue(
                    "Error",
                    "SourceEvidenceBelowThreshold",
                    path,
                    "The source evidence does not meet the profile set's recorded sample and confidence safeguards."));
            }
            if (profileSet.RequireMultiSeedStability
                && (team.SeedScoreMinimum is null
                    || team.SeedScoreMaximum is null
                    || !double.IsFinite(team.SeedScoreMinimum.Value)
                    || !double.IsFinite(team.SeedScoreMaximum.Value)
                    || team.SeedScoreMinimum is < 0d
                    || team.SeedScoreMaximum is > 1d
                    || team.SeedScoreMinimum > team.SeedScoreMaximum
                    || team.SeedScoreMaximum - team.SeedScoreMinimum > profileSet.MaximumSeedScoreSpread))
            {
                issues.Add(Issue(
                    "Error",
                    "SeedStabilityInvalid",
                    path,
                    "The source does not meet the profile set's recorded multi-seed stability safeguard."));
            }
            if (team.NearestSelectedEssenceOverlap is null
                || !double.IsFinite(team.NearestSelectedEssenceOverlap.Value)
                || team.NearestSelectedEssenceOverlap is < 0d or > 1d
                || (!IsDirectContextFamily(team.Family)
                    && team.NearestSelectedEssenceOverlap > profileSet.MaximumEssenceOverlap))
            {
                issues.Add(Issue(
                    "Error",
                    "EssenceDiversityInvalid",
                    path,
                    "The source exceeds the profile set's recorded Essence-overlap safeguard."));
            }
        }

        var expectedSlots = Enumerable.Range(0, profiles.Length);
        if (!profiles.Select(profile => profile.SlotIndex).Order().SequenceEqual(expectedSlots))
        {
            issues.Add(Issue(
                "Error",
                "ProfileSlotsInvalid",
                $"{path}.profiles",
                "Profile slot indices must be unique, contiguous, and start at zero."));
        }

        var requests = new List<CombatCharacterProfileMaterializationRequest>(profiles.Length);
        for (var profileIndex = 0; profileIndex < profiles.Length; profileIndex++)
        {
            var profile = profiles[profileIndex];
            var profilePath = $"{path}.profiles[{profileIndex}]";
            if (!string.Equals(profile.TeamId, team.Id, StringComparison.Ordinal))
                issues.Add(Issue("Error", "TeamReferenceMismatch", $"{profilePath}.teamId", "The profile references a different team ID."));
            if (!string.Equals(profile.Family, team.Family, StringComparison.Ordinal))
                issues.Add(Issue("Error", "FamilyMismatch", $"{profilePath}.family", "The profile family does not match its team."));
            if (!string.Equals(profile.ContentType, contentType.ToString(), StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue("Error", "ProfileContentTypeMismatch", $"{profilePath}.contentType", "The profile content type does not match its profile set."));

            if (!TryCreateRequest(profile, team, contentType, profilePath, issues, out var request))
                continue;
            requests.Add(request);
        }

        if (requests.Count != profiles.Length)
            return team;

        try
        {
            var rebuilt = await materializer.MaterializeTeamAsync(requests, cancellationToken);
            for (var index = 0; index < rebuilt.Count; index++)
            {
                var existing = profiles[index];
                var current = rebuilt[index];
                var profilePath = $"{path}.profiles[{index}]";
                if (existing.RawPowerRating != current.RawPowerRating
                    || existing.DisplayPowerRating != current.DisplayPowerRating)
                {
                    issues.Add(Issue(
                        "Error",
                        "PowerRatingDrift",
                        profilePath,
                        $"Stored Power {existing.DisplayPowerRating} no longer matches rebuilt Power {current.DisplayPowerRating}."));
                }
                if (!JsonNode.DeepEquals(
                        JsonSerializer.SerializeToNode(existing.Prepared, jsonOptions),
                        JsonSerializer.SerializeToNode(current.Prepared, jsonOptions)))
                {
                    issues.Add(Issue(
                        "Error",
                        "PreparedCombatantDrift",
                        $"{profilePath}.prepared",
                        "The stored preparation preview differs from the current production preparation result."));
                }
            }

            return team with { Profiles = rebuilt.OrderBy(profile => profile.SlotIndex).ToArray() };
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            issues.Add(Issue(
                "Error",
                "ProductionPreparationFailed",
                $"{path}.profiles",
                $"The team could not pass production combat preparation: {exception.Message}"));
            return team;
        }
    }

    private static bool IsDirectContextTeam(CombatCharacterProfileTeam team) =>
        IsDirectContextFamily(team.Family)
        && team.SourceBattles == 0
        && (team.Parties ?? []).Count == 1
        && team.Parties![0].Evidence is { SourceBattles: 0 }
        && (team.Parties[0].Evidence.ContextEvidence?.Count ?? 0) > 0;

    private static bool IsDirectContextFamily(string family) =>
        string.Equals(family, "CalibrationAnchor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "CalibrationTeam", StringComparison.OrdinalIgnoreCase);

    private void ValidateFamilySemantics(
        CombatCharacterProfileTeam team,
        IReadOnlyList<CombatCharacterProfile> profiles,
        CombatCharacterProfileGenerationReport profileSet,
        string path,
        List<CombatCharacterProfileCatalogValidationIssue> issues)
    {
        var isNoEssence = string.Equals(team.Family, "NoEssence", StringComparison.OrdinalIgnoreCase);
        if (team.IsSyntheticControl != isNoEssence)
        {
            issues.Add(Issue(
                "Error",
                "SyntheticFamilyInvalid",
                path,
                "Only the NoEssence family may be synthetic, and NoEssence must be marked synthetic."));
        }
        if (isNoEssence && profiles.Any(profile => profile.EssenceIds.Count != 0))
        {
            issues.Add(Issue(
                "Error",
                "NoEssenceControlInvalid",
                $"{path}.profiles",
                "NoEssence control profiles cannot equip Essences."));
        }
        if (string.Equals(team.Family, "Budget", StringComparison.OrdinalIgnoreCase)
            && profiles.SelectMany(profile => profile.EssenceIds)
                .Any(essenceId => essenceDefinitions.GetById(essenceId)?.Rarity != Rarity.Common))
        {
            issues.Add(Issue(
                "Error",
                "BudgetControlInvalid",
                $"{path}.profiles",
                "Budget control profiles may only equip Common Essences."));
        }
        var isCounter = string.Equals(team.Family, "Counter", StringComparison.OrdinalIgnoreCase);
        var isCountered = string.Equals(team.Family, "Countered", StringComparison.OrdinalIgnoreCase);
        var isEqualPowerAdversarial = string.Equals(
            team.Family,
            "EqualPowerAdversarial",
            StringComparison.OrdinalIgnoreCase);
        if (!team.IsComposedExpedition && (isCounter || isCountered || isEqualPowerAdversarial))
        {
            if (string.IsNullOrWhiteSpace(team.AdversarySourceSignature)
                || team.AdversaryBattles is null
                || team.AdversaryBattles < EffectiveMinimumMatchupBattles(profileSet)
                || team.AdversaryScore is null
                || !double.IsFinite(team.AdversaryScore.Value)
                || team.AdversaryScore is < 0d or > 1d
                || team.AdversaryConfidenceLower95 is null
                || team.AdversaryConfidenceUpper95 is null
                || !double.IsFinite(team.AdversaryConfidenceLower95.Value)
                || !double.IsFinite(team.AdversaryConfidenceUpper95.Value)
                || team.AdversaryConfidenceLower95 is < 0d
                || team.AdversaryConfidenceUpper95 is > 1d
                || team.AdversaryConfidenceLower95 > team.AdversaryScore
                || team.AdversaryConfidenceUpper95 < team.AdversaryScore)
            {
                issues.Add(Issue(
                    "Error",
                    "AdversaryEvidenceMissing",
                    path,
                    "Matchup-derived profile families require an audited adversary, sample count, and direct score."));
            }
            else if ((isCounter && (team.AdversaryScore < 0.60d
                                    || team.AdversaryConfidenceLower95 <= 0.5d))
                     || (isCountered && (team.AdversaryScore > 0.40d
                                         || team.AdversaryConfidenceUpper95 >= 0.5d))
                     || (isEqualPowerAdversarial
                         && (Math.Abs(team.AdversaryScore.Value - 0.5d) < 0.10d
                             || team.AdversaryConfidenceLower95 <= 0.5d
                                && team.AdversaryConfidenceUpper95 >= 0.5d)))
            {
                issues.Add(Issue(
                    "Error",
                    "AdversaryEvidenceInsufficient",
                    path,
                    "The recorded direct matchup is not sufficiently adversarial for this profile family."));
            }
        }

        const string specialistPrefix = "RoleSpecialist.";
        if (team.Family.StartsWith(specialistPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var roleName = team.Family[specialistPrefix.Length..];
            if (!Enum.TryParse<CanonicalCooperativeRole>(roleName, true, out var specialistRole)
                || profiles.Any(profile => !string.Equals(
                    profile.Role,
                    specialistRole.ToString(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(Issue(
                    "Error",
                    "RoleSpecialistInvalid",
                    $"{path}.profiles",
                    "A role-specialist team must use its declared canonical role in every slot."));
            }
            return;
        }
        if (team.IsComposedExpedition
            && string.Equals(team.Family, "Mixed.RoleSpecialist", StringComparison.OrdinalIgnoreCase))
        {
            // Each constituent party is checked against its own specialist role evidence.
            return;
        }

        var expectedRoles = CanonicalCooperativeRosterCatalog.CreateParty(profiles.Count)
            .Select(slot => slot.Role.ToString())
            .ToArray();
        if (profiles.OrderBy(profile => profile.SlotIndex)
            .Select(profile => profile.Role)
            .Where((role, index) => !string.Equals(role, expectedRoles[index], StringComparison.OrdinalIgnoreCase))
            .Any())
        {
            issues.Add(Issue(
                "Error",
                "CanonicalRoleCompositionInvalid",
                $"{path}.profiles",
                "The team does not use the canonical heterogeneous role composition for its size."));
        }
    }

    private bool TryCreateRequest(
        CombatCharacterProfile profile,
        CombatCharacterProfileTeam team,
        CombatContentType contentType,
        string path,
        List<CombatCharacterProfileCatalogValidationIssue> issues,
        out CombatCharacterProfileMaterializationRequest request)
    {
        request = default!;
        if (string.IsNullOrWhiteSpace(profile.Id)
            || string.IsNullOrWhiteSpace(profile.Name)
            || string.IsNullOrWhiteSpace(profile.Role))
        {
            issues.Add(Issue("Error", "ProfileIdentityMissing", path, "Profile ID, name, and role are required."));
            return false;
        }
        if (!Enum.TryParse<CanonicalPartyProfile>(profile.EquipmentProfile, true, out var equipmentProfile))
        {
            issues.Add(Issue("Error", "EquipmentProfileInvalid", $"{path}.equipmentProfile", $"'{profile.EquipmentProfile}' is not a canonical equipment profile."));
            return false;
        }
        if (!Enum.TryParse<CanonicalCooperativeRole>(profile.Role, true, out var role)
            || CanonicalCooperativeRosterCatalog.EquipmentProfileFor(role) != equipmentProfile)
        {
            issues.Add(Issue(
                "Error",
                "RoleEquipmentMismatch",
                path,
                "The profile role is invalid or does not use its canonical role equipment profile."));
            return false;
        }
        if (!Enum.TryParse<Rarity>(profile.EquipmentRarity, true, out var rarity)
            || !Enum.TryParse<ItemQuality>(profile.EquipmentQuality, true, out var quality))
        {
            issues.Add(Issue("Error", "EquipmentRungInvalid", path, "The equipment rarity or quality is invalid."));
            return false;
        }

        var rung = canonicalBuilds.GetProgressionLadder().SingleOrDefault(candidate =>
            candidate.Tier == profile.EquipmentTier
            && candidate.Rarity == rarity
            && candidate.Quality == quality);
        if (rung is null)
        {
            issues.Add(Issue(
                "Error",
                "EquipmentRungUnavailable",
                path,
                $"No canonical progression rung exists for Tier {profile.EquipmentTier} {quality} {rarity}."));
            return false;
        }

        request = new CombatCharacterProfileMaterializationRequest(
            profile.Id,
            team.Id,
            profile.SlotIndex,
            profile.Name,
            team.Family,
            profile.Role,
            contentType,
            equipmentProfile,
            rung,
            profile.EssenceIds,
            profile.PartyNumber,
            profile.PartySlotIndex,
            profile.SourcePartyProfileId);
        return true;
    }

    private void ValidateParties(
        CombatCharacterProfileTeam team,
        IReadOnlyList<CombatCharacterProfile> profiles,
        CombatCharacterProfileGenerationReport profileSet,
        string path,
        List<CombatCharacterProfileCatalogValidationIssue> issues)
    {
        var parties = team.Parties ?? [];
        var scenario = profileSet.Scenario;
        if (scenario is null)
            return;
        if (parties.Count != scenario.PartyCount)
        {
            issues.Add(Issue(
                "Error",
                "ExpeditionPartyCountMismatch",
                $"{path}.parties",
                $"The expedition contains {parties.Count} parties; scenario '{scenario.Id}' requires {scenario.PartyCount}."));
            return;
        }

        var profileIds = profiles.Select(profile => profile.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var referencedProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var party in parties.OrderBy(party => party.PartyNumber))
        {
            if (string.IsNullOrWhiteSpace(party.Id)
                || string.IsNullOrWhiteSpace(party.SourcePartyProfileId)
                || party.Evidence is null
                || string.IsNullOrWhiteSpace(party.Evidence.SourceSignature))
            {
                issues.Add(Issue(
                    "Error",
                    "ExpeditionPartyIdentityMissing",
                    $"{path}.parties",
                    "Every expedition party requires an instance ID, reusable source-party ID, and source signature."));
            }
            if (party.PartyNumber is < 1 || party.PartyNumber > scenario.PartyCount
                || party.ProfileIds.Count != scenario.PartySize)
            {
                issues.Add(Issue(
                    "Error",
                    "ExpeditionPartyLayoutInvalid",
                    $"{path}.parties",
                    $"Party {party.PartyNumber} must contain exactly {scenario.PartySize} profiles."));
            }
            foreach (var profileId in party.ProfileIds)
            {
                if (!profileIds.Contains(profileId) || !referencedProfileIds.Add(profileId))
                {
                    issues.Add(Issue(
                        "Error",
                        "ExpeditionPartyReferenceInvalid",
                        $"{path}.parties",
                        $"Profile reference '{profileId}' is missing or duplicated across expedition parties."));
                }
            }
            var assigned = profiles.Where(profile => profile.PartyNumber == party.PartyNumber)
                .OrderBy(profile => profile.PartySlotIndex)
                .ToArray();
            if (!assigned.Select(profile => profile.Id)
                    .SequenceEqual(party.ProfileIds, StringComparer.OrdinalIgnoreCase)
                || assigned.Any(profile => !string.Equals(
                    profile.SourcePartyProfileId,
                    party.SourcePartyProfileId,
                    StringComparison.Ordinal)))
            {
                issues.Add(Issue(
                    "Error",
                    "ExpeditionPartyProfileMismatch",
                    $"{path}.parties",
                    $"Party {party.PartyNumber} does not match its ordered profiles or reusable source-party identity."));
            }
            if (party.Evidence is not null)
                ValidatePartyEvidence(party.Evidence, assigned, profileSet, $"{path}.parties[{party.PartyNumber - 1}]", issues);
        }
        if (referencedProfileIds.Count != profiles.Count)
        {
            issues.Add(Issue(
                "Error",
                "ExpeditionPartyCoverageIncomplete",
                $"{path}.parties",
                "Every expedition profile must belong to exactly one party."));
        }

        var allSynthetic = parties.Count > 0
                           && parties.All(party => party.Evidence?.IsSyntheticControl == true);
        if (team.IsSyntheticControl != allSynthetic)
        {
            issues.Add(Issue(
                "Error",
                "ExpeditionSyntheticMismatch",
                path,
                "The expedition synthetic-control flag must match all of its constituent parties."));
        }

        if (!team.IsComposedExpedition && parties.Count == 1 && parties[0].Evidence is { } evidence
            && (!string.Equals(team.Family, evidence.Family, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(team.SourceSignature, evidence.SourceSignature, StringComparison.Ordinal)
                || team.SourceBattles != evidence.SourceBattles
                || team.SourceWins != evidence.SourceWins
                || team.SourceLosses != evidence.SourceLosses
                || team.SourceDraws != evidence.SourceDraws
                || Math.Abs(team.SourceScore - evidence.SourceScore) > 0.0000001d
                || team.IsSyntheticControl != evidence.IsSyntheticControl))
        {
            issues.Add(Issue(
                "Error",
                "SinglePartyEvidenceMismatch",
                $"{path}.parties[0].evidence",
                "A single-party expedition must expose the same evidence at party and team level."));
        }
    }

    private void ValidatePartyEvidence(
        CombatCharacterProfilePartyEvidence evidence,
        IReadOnlyList<CombatCharacterProfile> profiles,
        CombatCharacterProfileGenerationReport profileSet,
        string path,
        List<CombatCharacterProfileCatalogValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(evidence.Family)
            || string.IsNullOrWhiteSpace(evidence.SourceSignature)
            || string.IsNullOrWhiteSpace(evidence.SourceDisplayName)
            || string.IsNullOrWhiteSpace(evidence.SelectionReason))
        {
            issues.Add(Issue(
                "Error",
                "PartySourceEvidenceMissing",
                $"{path}.evidence",
                "Every party requires its family, source identity, display name, and selection rationale."));
        }

        var isNoEssence = string.Equals(evidence.Family, "NoEssence", StringComparison.OrdinalIgnoreCase);
        if (evidence.IsSyntheticControl != isNoEssence)
        {
            issues.Add(Issue(
                "Error",
                "PartySyntheticFamilyInvalid",
                $"{path}.evidence",
                "Only a NoEssence party may be synthetic, and a NoEssence party must be synthetic."));
        }
        if (isNoEssence && profiles.Any(profile => profile.EssenceIds.Count != 0))
        {
            issues.Add(Issue(
                "Error",
                "PartyNoEssenceControlInvalid",
                path,
                "A NoEssence party cannot equip Essences."));
        }
        if (string.Equals(evidence.Family, "Budget", StringComparison.OrdinalIgnoreCase)
            && profiles.SelectMany(profile => profile.EssenceIds)
                .Any(essenceId => essenceDefinitions.GetById(essenceId)?.Rarity != Rarity.Common))
        {
            issues.Add(Issue(
                "Error",
                "PartyBudgetControlInvalid",
                path,
                "A Budget party may only equip Common Essences."));
        }
        const string specialistPrefix = "RoleSpecialist.";
        if (evidence.Family.StartsWith(specialistPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var roleName = evidence.Family[specialistPrefix.Length..];
            if (!Enum.TryParse<CanonicalCooperativeRole>(roleName, true, out var specialistRole)
                || profiles.Any(profile => !string.Equals(
                    profile.Role,
                    specialistRole.ToString(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(Issue(
                    "Error",
                    "PartyRoleSpecialistInvalid",
                    path,
                "A role-specialist party must use its declared canonical role in every slot."));
            }
        }

        var requiresContextEvidence = profileSet.ContentType.Equals(
                                          CombatContentType.WorldTower.ToString(),
                                          StringComparison.OrdinalIgnoreCase)
                                      && (profileSet.Scenario?.FloorNumbers?.Count ?? 0) > 0
                                      && !isNoEssence
                                      && !evidence.Family.StartsWith(
                                          specialistPrefix,
                                          StringComparison.OrdinalIgnoreCase);
        var contextEvidence = evidence.ContextEvidence ?? [];
        if (requiresContextEvidence
            && !contextEvidence.Select(item => item.FloorNumber).Order()
                .SequenceEqual(profileSet.Scenario!.FloorNumbers!.Order()))
        {
            issues.Add(Issue(
                "Error",
                "PartyContextCoverageIncomplete",
                $"{path}.evidence.contextEvidence",
                "The party must contain production-runtime qualification for every scenario floor."));
        }
        foreach (var context in contextEvidence)
        {
            if (!string.Equals(context.ScenarioId, profileSet.Scenario?.Id, StringComparison.Ordinal)
                || context.TargetTeamSize != profileSet.Scenario?.TeamSize
                || context.SampleCount <= 0
                || context.Wins < 0
                || context.Losses < 0
                || context.Draws < 0
                || context.Wins + context.Losses + context.Draws != context.SampleCount
                || !double.IsFinite(context.WinRate)
                || Math.Abs(context.WinRate - context.Wins / (double)context.SampleCount) > 0.0000001d
                || !double.IsFinite(context.TimeoutRate)
                || context.TimeoutRate is < 0d or > 1d
                || Math.Abs(context.TimeoutRate - context.Draws / (double)context.SampleCount) > 0.0000001d
                || !double.IsFinite(context.AverageDurationTicks)
                || context.AverageDurationTicks < 0d
                || string.IsNullOrWhiteSpace(context.SeedManifestId)
                || context.SeedManifestHash?.Length != 64
                || !context.SeedManifestHash.All(Uri.IsHexDigit)
                || !context.UsesProductionRuntime
                || !context.AbilitiesStartOnCooldown)
            {
                issues.Add(Issue(
                    "Error",
                    "PartyContextEvidenceInvalid",
                    $"{path}.evidence.contextEvidence",
                    "Tower context evidence must contain valid production-runtime outcomes and deterministic seed provenance."));
            }
        }

        if (evidence.IsSyntheticControl)
        {
            if (evidence.SourceBattles != 0
                || evidence.SourceWins != 0
                || evidence.SourceLosses != 0
                || evidence.SourceDraws != 0)
            {
                issues.Add(Issue(
                    "Error",
                    "PartySyntheticEvidenceInvalid",
                    $"{path}.evidence",
                    "A synthetic party cannot claim source battle outcomes."));
            }
            return;
        }

        var isDirectContextTeam = IsDirectContextFamily(evidence.Family)
                                  && evidence.SourceBattles == 0;
        if (isDirectContextTeam)
        {
            var requiresAnchorResult = string.Equals(
                evidence.Family,
                "CalibrationAnchor",
                StringComparison.OrdinalIgnoreCase);
            if (evidence.SourceWins != 0
                || evidence.SourceLosses != 0
                || evidence.SourceDraws != 0
                || evidence.SourceScore != 0d
                || evidence.ConfidenceLower95 != 0d
                || evidence.ConfidenceUpper95 != 1d
                || evidence.SeedScoreMinimum is not null
                || evidence.SeedScoreMaximum is not null
                || evidence.AdversarySourceSignature is not null
                || evidence.AdversaryBattles is not null
                || evidence.AdversaryScore is not null
                || evidence.AdversaryConfidenceLower95 is not null
                || evidence.AdversaryConfidenceUpper95 is not null
                || requiresAnchorResult
                && !contextEvidence.Any(context =>
                    WorldTowerProfileTargetContract.Contains(context.WinRate))
                || contextEvidence.Any(context =>
                    !WorldTowerProfileTargetContract.IsBelowMaximum(context.WinRate)))
            {
                issues.Add(Issue(
                    "Error",
                    "PartyDirectContextTeamEvidenceInvalid",
                    $"{path}.evidence",
                    "A direct Tower calibration team requires neutral PvP fields and no exact-context estimate at or above 20%; an anchor family also requires at least one strict >5% and <20% result."));
            }
            if (!double.IsFinite(evidence.NearestSelectedEssenceOverlap)
                || evidence.NearestSelectedEssenceOverlap is < 0d or > 1d)
            {
                issues.Add(Issue(
                    "Error",
                    "PartyEssenceDiversityInvalid",
                    $"{path}.evidence",
                    "The direct Tower anchor's recorded Essence overlap is invalid."));
            }
            return;
        }

        if (evidence.SourceBattles <= 0
            || evidence.SourceWins < 0
            || evidence.SourceLosses < 0
            || evidence.SourceDraws < 0
            || evidence.SourceWins + evidence.SourceLosses + evidence.SourceDraws != evidence.SourceBattles)
        {
            issues.Add(Issue(
                "Error",
                "PartySourceBattleCountsInvalid",
                $"{path}.evidence",
                "Party source wins, losses, and draws must be non-negative and add up to source battles."));
            return;
        }

        var expectedScore = (evidence.SourceWins + evidence.SourceDraws * 0.5d) / evidence.SourceBattles;
        if (!double.IsFinite(evidence.SourceScore)
            || Math.Abs(evidence.SourceScore - expectedScore) > 0.0000001d)
        {
            issues.Add(Issue(
                "Error",
                "PartySourceScoreInvalid",
                $"{path}.evidence.sourceScore",
                "The party source score does not match its battle outcomes."));
        }
        if (!double.IsFinite(evidence.ConfidenceLower95)
            || !double.IsFinite(evidence.ConfidenceUpper95)
            || evidence.ConfidenceLower95 < 0d
            || evidence.ConfidenceUpper95 > 1d
            || evidence.ConfidenceLower95 > expectedScore
            || evidence.ConfidenceUpper95 < expectedScore
            || evidence.SourceBattles < profileSet.MinimumSourceBattles
            || evidence.ConfidenceUpper95 - evidence.ConfidenceLower95 > profileSet.MaximumConfidenceWidth95)
        {
            issues.Add(Issue(
                "Error",
                "PartySourceEvidenceBelowThreshold",
                $"{path}.evidence",
                "The party source does not meet the recorded sample and confidence safeguards."));
        }
        if (profileSet.RequireMultiSeedStability
            && (evidence.SeedScoreMinimum is null
                || evidence.SeedScoreMaximum is null
                || !double.IsFinite(evidence.SeedScoreMinimum.Value)
                || !double.IsFinite(evidence.SeedScoreMaximum.Value)
                || evidence.SeedScoreMinimum is < 0d
                || evidence.SeedScoreMaximum is > 1d
                || evidence.SeedScoreMinimum > evidence.SeedScoreMaximum
                || evidence.SeedScoreMaximum - evidence.SeedScoreMinimum > profileSet.MaximumSeedScoreSpread))
        {
            issues.Add(Issue(
                "Error",
                "PartySeedStabilityInvalid",
                $"{path}.evidence",
                "The party source does not meet the recorded multi-seed stability safeguard."));
        }
        if (!double.IsFinite(evidence.NearestSelectedEssenceOverlap)
            || evidence.NearestSelectedEssenceOverlap is < 0d or > 1d
            || (!IsDirectContextFamily(evidence.Family)
                && evidence.NearestSelectedEssenceOverlap > profileSet.MaximumEssenceOverlap))
        {
            issues.Add(Issue(
                "Error",
                "PartyEssenceDiversityInvalid",
                $"{path}.evidence",
                "The party source exceeds the recorded Essence-overlap safeguard."));
        }

        var isCounter = string.Equals(evidence.Family, "Counter", StringComparison.OrdinalIgnoreCase);
        var isCountered = string.Equals(evidence.Family, "Countered", StringComparison.OrdinalIgnoreCase);
        var isAdversarial = string.Equals(
            evidence.Family,
            "EqualPowerAdversarial",
            StringComparison.OrdinalIgnoreCase);
        if ((isCounter || isCountered || isAdversarial)
            && (string.IsNullOrWhiteSpace(evidence.AdversarySourceSignature)
                || evidence.AdversaryBattles is null
                || evidence.AdversaryBattles < EffectiveMinimumMatchupBattles(profileSet)
                || evidence.AdversaryScore is null
                || !double.IsFinite(evidence.AdversaryScore.Value)
                || evidence.AdversaryScore is < 0d or > 1d
                || evidence.AdversaryConfidenceLower95 is null
                || evidence.AdversaryConfidenceUpper95 is null
                || !double.IsFinite(evidence.AdversaryConfidenceLower95.Value)
                || !double.IsFinite(evidence.AdversaryConfidenceUpper95.Value)
                || evidence.AdversaryConfidenceLower95 is < 0d
                || evidence.AdversaryConfidenceUpper95 is > 1d
                || evidence.AdversaryConfidenceLower95 > evidence.AdversaryScore
                || evidence.AdversaryConfidenceUpper95 < evidence.AdversaryScore
                || (isCounter && (evidence.AdversaryScore < 0.60d
                                  || evidence.AdversaryConfidenceLower95 <= 0.5d))
                || (isCountered && (evidence.AdversaryScore > 0.40d
                                    || evidence.AdversaryConfidenceUpper95 >= 0.5d))
                || (isAdversarial
                    && (Math.Abs(evidence.AdversaryScore.Value - 0.5d) < 0.10d
                        || evidence.AdversaryConfidenceLower95 <= 0.5d
                           && evidence.AdversaryConfidenceUpper95 >= 0.5d))))
        {
            issues.Add(Issue(
                "Error",
                "PartyAdversaryEvidenceInvalid",
                $"{path}.evidence",
                "A matchup-derived party requires sufficient audited adversary evidence."));
        }
    }

    private CombatCharacterProfileCatalogValidationReport InvalidLoadResult(
        CombatCharacterProfileCatalogDocument catalog,
        string code,
        string message) =>
        new(
            false,
            AbilityBalanceContentFingerprint.Create(abilityCatalog, essenceDefinitions),
            catalog,
            [Issue("Error", code, "$", message)]);

    private static int EffectiveMinimumMatchupBattles(
        CombatCharacterProfileGenerationReport profileSet) =>
        profileSet.MinimumMatchupBattles > 0
            ? profileSet.MinimumMatchupBattles
            : profileSet.MinimumSourceBattles;

    private static CombatCharacterProfileCatalogDocument EmptyCatalog() =>
        new(SchemaVersion, CatalogVersion, []);

    private static void CheckVersion(
        int actual,
        int expected,
        string code,
        string path,
        string label,
        ICollection<CombatCharacterProfileCatalogValidationIssue> issues)
    {
        if (actual != expected)
        {
            issues.Add(Issue(
                "Error",
                code,
                path,
                $"The {label} version is {actual}; the current version is {expected}."));
        }
    }

    private static void AddDuplicateIssues(
        IEnumerable<string> values,
        string code,
        string path,
        string label,
        ICollection<CombatCharacterProfileCatalogValidationIssue> issues)
    {
        foreach (var duplicate in values
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(Issue("Error", code, path, $"The {label} '{duplicate.Key}' is duplicated."));
        }
    }

    private static int FamilyOrder(string family) => family switch
    {
        "Meta" => 0,
        "Typical" => 1,
        "WeakButLegal" => 2,
        _ => 3
    };

    private static bool IsUnitInterval(double value) =>
        double.IsFinite(value) && value is >= 0d and <= 1d;

    private static string? ProfileSetScenarioKey(CombatCharacterProfileGenerationReport set)
        => set.Scenario?.Id;

    private static CombatCharacterProfileCatalogValidationIssue Issue(
        string severity,
        string code,
        string path,
        string message) =>
        new(severity, code, path, message);
}
