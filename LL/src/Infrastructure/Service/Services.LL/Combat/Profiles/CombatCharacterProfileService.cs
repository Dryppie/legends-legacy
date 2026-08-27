using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Profiles;

public sealed class CombatCharacterProfileService(
    CanonicalEquipmentBuildFactory canonicalBuilds,
    CombatCharacterProfileMaterializer materializer,
    IEssenceDefinitionRepository essenceDefinitions,
    IAbilityCatalogProvider abilityCatalog,
    IWorldTowerProfileCandidateQualifier? worldTowerQualifier = null) : ICombatCharacterProfileService
{
    public const int SchemaVersion = 7;
    public const int GeneratorVersion = 23;

    public async Task<CombatCharacterProfileGenerationReport> GenerateAsync(
        CombatCharacterProfileGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        var candidates = GetEligibleCandidates(normalized);
        var directAnchorCandidates = new List<AbilityBalanceCombinationResult>();
        var contextEvidence =
            new Dictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>>(StringComparer.Ordinal);
        if (normalized.ParsedContentType == CombatContentType.WorldTower
            && normalized.FloorNumbers.Count > 0)
        {
            if (worldTowerQualifier is null)
                throw new InvalidOperationException("World Tower profile generation requires candidate qualification.");
            var finalistEvidence = await worldTowerQualifier.QualifyAsync(
                candidates,
                normalized.Scenario,
                normalized.ContextQualificationSampleCount,
                normalized.RandomSeed,
                cancellationToken);
            foreach (var pair in finalistEvidence)
                contextEvidence.Add(pair.Key, pair.Value);

            var targetCandidateSignatures = candidates
                .Where(candidate => finalistEvidence.GetValueOrDefault(candidate.Signature)?.Any(evidence =>
                    WorldTowerProfileTargetContract.Contains(evidence.WinRate)) == true)
                .Select(candidate => candidate.Signature)
                .ToHashSet(StringComparer.Ordinal);
            if (targetCandidateSignatures.Count > 0)
            {
                var confirmedEvidence = await worldTowerQualifier.QualifyAsync(
                    candidates.Where(candidate => targetCandidateSignatures.Contains(candidate.Signature)).ToArray(),
                    normalized.Scenario,
                    WorldTowerProfileTargetContract.SelectionConfirmationSampleCount,
                    normalized.RandomSeed,
                    cancellationToken);
                foreach (var pair in confirmedEvidence)
                    contextEvidence[pair.Key] = pair.Value;
            }

            var search = await worldTowerQualifier.SearchCalibrationAnchorsAsync(
                normalized.Audit.EssenceResults,
                normalized.Scenario,
                normalized.FloorNumbers,
                normalized.ContextQualificationSampleCount,
                normalized.RandomSeed,
                candidates.Select(candidate => candidate.Signature).ToHashSet(StringComparer.Ordinal),
                cancellationToken);
            directAnchorCandidates.AddRange(search.Candidates);
            foreach (var pair in search.ContextEvidence)
                contextEvidence.Add(pair.Key, pair.Value);

            if (normalized.PortfolioMode == ProfilePortfolioMode.Expanded)
            {
                var noEssence = CreateNoEssenceCombination(normalized.DiscoveryTeamSize, 0);
                var noEssenceEvidence = await worldTowerQualifier.QualifyAsync(
                    [noEssence],
                    normalized.Scenario,
                    WorldTowerProfileTargetContract.SelectionConfirmationSampleCount,
                    normalized.RandomSeed,
                    cancellationToken);
                foreach (var pair in noEssenceEvidence)
                    contextEvidence[pair.Key] = pair.Value;
            }
        }
        var selected = SelectTeams(normalized, candidates, directAnchorCandidates, contextEvidence);
        var expeditions = ComposeExpeditions(normalized, selected);
        var teams = new List<CombatCharacterProfileTeam>(expeditions.Count);

        foreach (var expedition in expeditions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            teams.Add(await MaterializeTeamAsync(normalized, expedition, cancellationToken));
        }

        return new CombatCharacterProfileGenerationReport(
            SchemaVersion,
            GeneratorVersion,
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            EquipmentStatBudgetCatalog.BalanceVersion,
            CanonicalCooperativeRosterCatalog.Version,
            normalized.AuditId,
            normalized.Audit.ContentHash,
            normalized.ContentType,
            normalized.RandomSeed,
            teams,
            normalized.PortfolioMode.ToString(),
            normalized.MinimumSourceBattles,
            normalized.MinimumMatchupBattles,
            normalized.MaximumConfidenceWidth95,
            normalized.MaximumSeedScoreSpread,
            normalized.MaximumEssenceOverlap,
            normalized.RequireMultiSeedStability,
            normalized.Scenario);
    }

    private async Task<CombatCharacterProfileTeam> MaterializeTeamAsync(
        NormalizedRequest request,
        SelectedExpedition expedition,
        CancellationToken cancellationToken)
    {
        var sourceSignatures = expedition.Parties
            .Select(party => party.Source.Signature)
            .ToArray();
        var teamId = CombatCharacterProfileIdentity.CreateStableId(
            "team",
            request.Audit.ContentHash,
            request.ContentType,
            request.Scenario.Id,
            expedition.Family,
            string.Join('|', sourceSignatures),
            request.RandomSeed.ToString());
        var profiles = await materializer.MaterializeTeamAsync(
            Enumerable.Range(0, request.TeamSize)
                .Select(slotIndex =>
                {
                    var partyNumber = slotIndex / request.PartySize + 1;
                    var partySlotIndex = slotIndex % request.PartySize;
                    var party = expedition.Parties[partyNumber - 1];
                    return CreateMaterializationRequest(
                        request,
                        expedition,
                        party,
                        teamId,
                        party.Source.Participants[partySlotIndex],
                        slotIndex,
                        partyNumber,
                        partySlotIndex,
                        CreateSourcePartyProfileId(request, party));
                })
                .ToArray(),
            cancellationToken);
        var parties = profiles
            .GroupBy(profile => profile.PartyNumber)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var selectedParty = expedition.Parties[group.Key - 1];
                return new CombatCharacterProfileParty(
                    CombatCharacterProfileIdentity.CreateStableId(
                        "expedition-party",
                        teamId,
                        group.Key.ToString()),
                    CreateSourcePartyProfileId(request, selectedParty),
                    group.Key,
                    group.OrderBy(profile => profile.PartySlotIndex)
                        .Select(profile => profile.Id)
                        .ToArray(),
                    CreatePartyEvidence(selectedParty));
            })
            .ToArray();
        var isComposed = request.PartyCount > 1;
        var direct = expedition.Parties[0];
        var source = direct.Source;
        var evidence = CreatePartyEvidence(direct);
        var compositionSignature = $"composition:{expedition.Family}:{string.Join('|', sourceSignatures)}";
        var claimsAuditedSource = !isComposed && !direct.UsesDirectContextEvidence;

        return new CombatCharacterProfileTeam(
            teamId,
            expedition.Family,
            isComposed ? compositionSignature : source.Signature,
            isComposed
                ? string.Join(" + ", expedition.Parties.Select(party => party.Source.DisplayName))
                : source.DisplayName,
            claimsAuditedSource ? source.Battles : 0,
            claimsAuditedSource ? source.Wins : 0,
            claimsAuditedSource ? source.Losses : 0,
            claimsAuditedSource ? source.Draws : 0,
            claimsAuditedSource ? evidence.SourceScore : 0d,
            claimsAuditedSource ? evidence.ConfidenceLower95 : 0d,
            claimsAuditedSource ? evidence.ConfidenceUpper95 : 1d,
            profiles,
            expedition.SelectionReason,
            expedition.Parties.All(party => party.IsSyntheticControl),
            claimsAuditedSource ? direct.AdversarySourceSignature : null,
            claimsAuditedSource ? evidence.SeedScoreMinimum : null,
            claimsAuditedSource ? evidence.SeedScoreMaximum : null,
            isComposed ? null : direct.NearestSelectedEssenceOverlap,
            claimsAuditedSource ? direct.AdversaryEvidence?.Battles : null,
            claimsAuditedSource ? direct.AdversaryEvidence?.Score : null,
            claimsAuditedSource ? direct.AdversaryEvidence?.ConfidenceLower95 : null,
            claimsAuditedSource ? direct.AdversaryEvidence?.ConfidenceUpper95 : null,
            parties,
            isComposed);
    }

    private static CombatCharacterProfileMaterializationRequest CreateMaterializationRequest(
        NormalizedRequest request,
        SelectedExpedition expedition,
        SelectedTeam selectedParty,
        string teamId,
        AbilityBalanceParticipantLoadout participant,
        int slotIndex,
        int partyNumber,
        int partySlotIndex,
        string sourcePartyProfileId)
    {
        var role = selectedParty.Roles[partySlotIndex];
        var equipmentProfile = CanonicalCooperativeRosterCatalog.EquipmentProfileFor(role);
        var profileId = CombatCharacterProfileIdentity.CreateStableId(
            "profile",
            teamId,
            slotIndex.ToString(),
            partyNumber.ToString(),
            partySlotIndex.ToString(),
            role.ToString(),
            string.Join(':', participant.EssenceIds));
        return new CombatCharacterProfileMaterializationRequest(
            profileId,
            teamId,
            slotIndex,
            $"{expedition.Family} {role} {slotIndex + 1}",
            expedition.Family,
            role.ToString(),
            request.ParsedContentType,
            equipmentProfile,
            request.ProgressionRung,
            participant.EssenceIds.ToArray(),
            partyNumber,
            partySlotIndex,
            sourcePartyProfileId);
    }

    private static string CreateSourcePartyProfileId(
        NormalizedRequest request,
        SelectedTeam party) => CombatCharacterProfileIdentity.CreateStableId(
        "party-profile",
        request.Audit.ContentHash,
        request.ContentType,
        party.Family,
        party.Source.Signature,
        request.RandomSeed.ToString());

    private static CombatCharacterProfilePartyEvidence CreatePartyEvidence(SelectedTeam party)
    {
        var score = CombinationScore(party.Source);
        var confidence = party.IsSyntheticControl || party.UsesDirectContextEvidence
            ? (Lower: 0d, Upper: 1d)
            : WilsonInterval(score, party.Source.Battles);
        var seedScores = party.Source.SeedResults?
            .Where(result => result.Battles > 0 && double.IsFinite(result.Score))
            .Select(result => result.Score)
            .ToArray() ?? [];
        return new CombatCharacterProfilePartyEvidence(
            party.Family,
            party.Source.Signature,
            party.Source.DisplayName,
            party.UsesDirectContextEvidence ? 0 : party.Source.Battles,
            party.UsesDirectContextEvidence ? 0 : party.Source.Wins,
            party.UsesDirectContextEvidence ? 0 : party.Source.Losses,
            party.UsesDirectContextEvidence ? 0 : party.Source.Draws,
            party.UsesDirectContextEvidence ? 0d : score,
            confidence.Lower,
            confidence.Upper,
            party.SelectionReason,
            party.IsSyntheticControl,
            party.AdversarySourceSignature,
            party.UsesDirectContextEvidence || seedScores.Length == 0 ? null : seedScores.Min(),
            party.UsesDirectContextEvidence || seedScores.Length == 0 ? null : seedScores.Max(),
            party.NearestSelectedEssenceOverlap,
            party.AdversaryEvidence?.Battles,
            party.AdversaryEvidence?.Score,
            party.AdversaryEvidence?.ConfidenceLower95,
            party.AdversaryEvidence?.ConfidenceUpper95,
            party.ContextEvidence ?? []);
    }

    private static IReadOnlyList<SelectedExpedition> ComposeExpeditions(
        NormalizedRequest request,
        IReadOnlyList<SelectedTeam> selected)
    {
        if (request.ParsedContentType != CombatContentType.WorldTower)
        {
            return selected.Select(party => new SelectedExpedition(
                party.Family,
                Enumerable.Repeat(party, request.PartyCount).ToArray(),
                party.SelectionReason)).ToArray();
        }

        if (request.PartyCount == 1)
        {
            if (request.PortfolioMode == ProfilePortfolioMode.Core)
            {
                return selected.Select(party =>
                    new SelectedExpedition(party.Family, [party], party.SelectionReason)).ToArray();
            }

            if (IsCalibrationOnlyPortfolio())
            {
                return selected.Select(party =>
                    new SelectedExpedition(party.Family, [party], party.SelectionReason)).ToArray();
            }

            var singlePartyPortfolio = new List<SelectedTeam>
            {
                Single("Meta"),
                Single("Typical"),
                Single("WeakButLegal"),
                Single("Budget"),
                Single("Counter"),
                Single("Countered")
            };
            singlePartyPortfolio.AddRange(Many("EqualPowerAdversarial").Take(2));
            singlePartyPortfolio.Add(Many("RoleSpecialist.Controller").First());
            singlePartyPortfolio.Add(Single("NoEssence"));
            singlePartyPortfolio.AddRange(Many("CalibrationAnchor"));
            return singlePartyPortfolio.Select(party =>
                new SelectedExpedition(party.Family, [party], party.SelectionReason)).ToArray();
        }

        if (request.PortfolioMode == ProfilePortfolioMode.Core)
        {
            return selected.Select(party => new SelectedExpedition(
                party.Family,
                Repeat(party),
                $"Homogeneous {party.Family} expedition composed from {request.PartyCount} independently bounded parties."))
                .ToArray();
        }

        if (IsCalibrationOnlyPortfolio())
        {
            return selected.Select(party => new SelectedExpedition(
                    party.Family,
                    Enumerable.Repeat(party, request.PartyCount).ToArray(),
                    party.SelectionReason))
                .ToArray();
        }

        var meta = Single("Meta");
        var typical = Single("Typical");
        var weak = Single("WeakButLegal");
        var budget = Single("Budget");
        var counter = Single("Counter");
        var countered = Single("Countered");
        var adversarial = Many("EqualPowerAdversarial").ToArray();
        var specialists = selected.Where(party =>
                party.Family.StartsWith("RoleSpecialist.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(party => party.Family, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var noEssence = Single("NoEssence");

        var portfolio = new List<SelectedExpedition>
        {
            Homogeneous("Meta", meta),
            Homogeneous("Typical", typical),
            Mixed("Mixed.MetaTypical", [meta, typical],
                "Alternating Meta and Typical party profiles model a strong but non-uniform expedition."),
            Mixed("Mixed.RoleSpecialist", specialists,
                "Distinct role-specialist parties test expedition-level complementarity and redundancy."),
            Homogeneous("WeakButLegal", weak),
            Homogeneous("Budget", budget),
            Homogeneous("Counter", counter),
            Homogeneous("Countered", countered),
            Mixed("EqualPowerAdversarial", adversarial,
                "Alternating equal-power adversarial party profiles preserves both audited sides of the matchup."),
            Homogeneous("NoEssence", noEssence)
        };
        portfolio.AddRange(Many("CalibrationAnchor")
            .Select(anchor => new SelectedExpedition(
                "CalibrationAnchor",
                Repeat(anchor),
                anchor.SelectionReason)));
        return portfolio;

        SelectedExpedition Homogeneous(string family, SelectedTeam party) => new(
            family,
            Repeat(party),
            $"Homogeneous {family} expedition composed from {request.PartyCount} independently bounded parties.");

        SelectedExpedition Mixed(
            string family,
            IReadOnlyList<SelectedTeam> parties,
            string reason) => new(family, Cycle(parties), reason);

        IReadOnlyList<SelectedTeam> Repeat(SelectedTeam party) =>
            Enumerable.Repeat(party, request.PartyCount).ToArray();

        IReadOnlyList<SelectedTeam> Cycle(IReadOnlyList<SelectedTeam> parties) =>
            Enumerable.Range(0, request.PartyCount)
                .Select(index => parties[index % parties.Count])
                .ToArray();

        SelectedTeam Single(string family) => selected.Single(party =>
            string.Equals(party.Family, family, StringComparison.OrdinalIgnoreCase));

        IEnumerable<SelectedTeam> Many(string family) => selected.Where(party =>
            string.Equals(party.Family, family, StringComparison.OrdinalIgnoreCase));

        bool IsCalibrationOnlyPortfolio() => selected.Count > 0
            && selected.All(party => party.Family is "CalibrationAnchor" or "CalibrationTeam" or "NoEssence");
    }

    private NormalizedRequest Normalize(CombatCharacterProfileGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Audit);
        if (string.IsNullOrWhiteSpace(request.AuditId))
            throw new ArgumentException("Character profile generation requires an audit ID.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Audit.ContentHash))
            throw new ArgumentException("Character profile generation requires an audit content hash.", nameof(request));
        var currentContentHash = AbilityBalanceContentFingerprint.Create(abilityCatalog, essenceDefinitions);
        if (!request.Audit.ContentHash.Equals(currentContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The selected balance audit is stale for the current combat content. Run a new audit before generating profiles.");
        }
        if (request.Audit.Finalists.Count < 3)
            throw new ArgumentException("At least three finalist teams are required to generate profile controls.", nameof(request));
        if (!Enum.TryParse<CombatContentType>(request.ContentType, true, out var contentType))
            throw new ArgumentException($"Unknown combat content type '{request.ContentType}'.", nameof(request));
        if (!Enum.TryParse<Rarity>(request.Audit.EquipmentRarity, true, out var sourceRarity)
            || sourceRarity > Rarity.Legendary)
            throw new ArgumentException($"Unknown equipment rarity '{request.Audit.EquipmentRarity}'.", nameof(request));
        var targetRarityName = request.TargetEquipmentRarity ?? request.Audit.EquipmentRarity;
        if (!Enum.TryParse<Rarity>(targetRarityName, true, out var targetRarity)
            || targetRarity > Rarity.Legendary)
            throw new ArgumentException($"Unknown target equipment rarity '{targetRarityName}'.", nameof(request));
        if (!Enum.TryParse<ItemQuality>(request.EquipmentQuality, true, out var quality))
            throw new ArgumentException($"Unknown equipment quality '{request.EquipmentQuality}'.", nameof(request));
        if (!Enum.TryParse<CanonicalPartyProfile>(request.Audit.EquipmentProfile, true, out var auditEquipmentProfile))
            throw new ArgumentException($"Unknown equipment profile '{request.Audit.EquipmentProfile}'.", nameof(request));
        if (!Enum.TryParse<ProfilePortfolioMode>(request.PortfolioMode, true, out var portfolioMode))
            throw new ArgumentException($"Unknown profile portfolio mode '{request.PortfolioMode}'.", nameof(request));

        var teamsPerFamily = Math.Clamp(request.TeamsPerFamily, 1, 10);
        var minimumSourceBattles = Math.Max(1, request.MinimumSourceBattles);
        var minimumMatchupBattles = Math.Max(1, request.MinimumMatchupBattles);
        var maximumConfidenceWidth95 = RequireUnitInterval(
            request.MaximumConfidenceWidth95,
            nameof(request.MaximumConfidenceWidth95));
        var maximumSeedScoreSpread = RequireUnitInterval(
            request.MaximumSeedScoreSpread,
            nameof(request.MaximumSeedScoreSpread));
        var maximumEssenceOverlap = RequireUnitInterval(
            request.MaximumEssenceOverlap,
            nameof(request.MaximumEssenceOverlap));
        var floorNumbers = (request.TargetFloorNumbers ?? [])
            .Where(floorNumber => floorNumber > 0)
            .Distinct()
            .Order()
            .ToArray();
        var contextQualificationSampleCount = Math.Clamp(
            request.ContextQualificationSampleCount,
            1,
            100);
        if (portfolioMode == ProfilePortfolioMode.Expanded
            && request.RequireMultiSeedStability
            && (request.Audit.RandomSeeds?.Distinct().Count() ?? 0) < 2)
        {
            throw new InvalidOperationException(
                "Expanded stable profile generation requires a new audit containing at least two finalist seeds.");
        }

        var targetEquipmentTier = request.TargetEquipmentTier ?? request.Audit.EquipmentTier;
        var rung = canonicalBuilds.GetProgressionLadder().SingleOrDefault(candidate =>
            candidate.Tier == targetEquipmentTier
            && candidate.Rarity == targetRarity
            && candidate.Quality == quality)
            ?? throw new ArgumentException(
                $"No canonical progression rung exists for Tier {targetEquipmentTier} {quality} {targetRarity}.",
                nameof(request));
        var discoveryTeamSize = request.Audit.Finalists
            .Where(finalist => finalist.Participants.Count > 0)
            .Select(finalist => finalist.Participants.Count)
            .FirstOrDefault();
        if (discoveryTeamSize <= 0
            || request.Audit.Finalists.Any(finalist => finalist.Participants.Count != discoveryTeamSize))
            throw new ArgumentException("Every audit finalist must contain the same non-empty team size.", nameof(request));
        if (discoveryTeamSize > 5)
            throw new ArgumentException("Profile discovery teams cannot exceed the five-character party limit.", nameof(request));
        var teamSize = request.TargetTeamSize ?? discoveryTeamSize;
        if (teamSize < discoveryTeamSize || teamSize % discoveryTeamSize != 0)
        {
            throw new ArgumentException(
                "Target team size must contain one or more complete source-discovery parties.",
                nameof(request));
        }
        if (contentType == CombatContentType.WorldTower
            && (discoveryTeamSize != 5 || teamSize is not (5 or 10 or 15)))
        {
            throw new ArgumentException(
                "World Tower expedition generation requires five-character discovery parties and 5, 10, or 15 target slots.",
                nameof(request));
        }
        var essenceCounts = request.Audit.Finalists
            .SelectMany(finalist => finalist.Participants)
            .Select(participant => participant.EssenceIds.Count)
            .Distinct()
            .ToArray();
        if (essenceCounts.Length != 1)
        {
            throw new ArgumentException(
                "Every audit finalist participant must contain the same Essence-slot count.",
                nameof(request));
        }
        var scenario = new CombatCharacterProfileScenario(
            CombatCharacterProfileScenario.CreateId(
                contentType.ToString(),
                teamSize,
                rung.Tier,
                rung.Rarity.ToString(),
                rung.Quality.ToString(),
                auditEquipmentProfile.ToString(),
                essenceCounts[0],
                contentType == CombatContentType.WorldTower && floorNumbers.Length == 1
                    ? floorNumbers[0]
                    : null),
            teamSize,
            rung.Tier,
            rung.Rarity.ToString(),
            rung.Quality.ToString(),
            auditEquipmentProfile.ToString(),
            essenceCounts[0],
            discoveryTeamSize,
            teamSize / discoveryTeamSize,
            discoveryTeamSize,
            floorNumbers);

        return new NormalizedRequest(
            request.AuditId.Trim(),
            request.Audit,
            contentType.ToString(),
            contentType,
            teamsPerFamily,
            request.RandomSeed == 0 ? 1337 : request.RandomSeed,
            portfolioMode,
            minimumSourceBattles,
            minimumMatchupBattles,
            maximumConfidenceWidth95,
            maximumSeedScoreSpread,
            maximumEssenceOverlap,
            request.RequireMultiSeedStability,
            teamSize,
            discoveryTeamSize,
            discoveryTeamSize,
            teamSize / discoveryTeamSize,
            rung,
            scenario,
            floorNumbers,
            contextQualificationSampleCount);
    }

    private static IReadOnlyList<AbilityBalanceCombinationResult> GetEligibleCandidates(
        NormalizedRequest request)
    {
        var candidates = request.Audit.Finalists
            .Where(candidate => candidate.Battles > 0
                                && candidate.Participants.Count == request.DiscoveryTeamSize)
            .GroupBy(candidate => candidate.Signature, StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(candidate => IsEvidenceEligible(candidate, request))
            .ToArray();
        if (candidates.Length < request.TeamsPerFamily * 3)
        {
            throw new InvalidOperationException(
                $"Only {candidates.Length} finalist teams meet the configured sample, confidence, and seed-stability safeguards; "
                + $"at least {request.TeamsPerFamily * 3} are required for the core profile families.");
        }

        return candidates;
    }

    private IReadOnlyList<SelectedTeam> SelectTeams(
        NormalizedRequest request,
        IReadOnlyList<AbilityBalanceCombinationResult> candidates,
        IReadOnlyList<AbilityBalanceCombinationResult> directAnchorCandidates,
        IReadOnlyDictionary<string, IReadOnlyList<CombatCharacterProfileContextEvidence>> contextEvidence)
    {

        var selectionCandidates = request.ParsedContentType == CombatContentType.WorldTower
                                  && request.FloorNumbers.Count > 0
            ? candidates.Where(IsBelowMaximumOnEveryFloor).ToArray()
            : candidates;
        var selected = new List<SelectedTeam>();
        var usedSources = new HashSet<string>(StringComparer.Ordinal);
        var standardRoles = CanonicalCooperativeRosterCatalog.CreateParty(request.PartySize)
            .Select(slot => slot.Role)
            .ToArray();
        if (request.PortfolioMode == ProfilePortfolioMode.Expanded
            && request.ParsedContentType == CombatContentType.WorldTower
            && request.FloorNumbers.Count > 0)
        {
            AddWorldTowerCalibrationPortfolio();
            return FinalizeSelections();
        }

        if (selectionCandidates.Count < request.TeamsPerFamily * 3)
        {
            throw new InvalidOperationException(
                $"Only {selectionCandidates.Count} finalist teams remain strictly below "
                + $"{WorldTowerProfileTargetContract.MaximumWinRate:P0} on every target floor; at least "
                + $"{request.TeamsPerFamily * 3} are required for the core profile families.");
        }

        var sortedScores = selectionCandidates.Select(CombinationScore).Order().ToArray();
        var median = sortedScores.Length % 2 == 0
            ? (sortedScores[sortedScores.Length / 2 - 1] + sortedScores[sortedScores.Length / 2]) / 2d
            : sortedScores[sortedScores.Length / 2];

        if (request.PortfolioMode == ProfilePortfolioMode.Core)
        {
            AddCoreFamilies();
            return FinalizeSelections();
        }

        // Constrained families are selected first so generic score bands cannot consume
        // the only evidence-qualified source needed by the expanded portfolio.
        AddFamily(
            "Budget",
            selectionCandidates.Where(IsBudgetCombination)
                .OrderByDescending(ContextMinimumScore)
                .ThenByDescending(ContextAverageScore)
                .ThenByDescending(CombinationScore)
                .ThenBy(candidate => StableTieBreaker(candidate.Signature, request.RandomSeed)),
            standardRoles,
            "Strongest stable finalist composed entirely of Common Essences.");

        AddCounterFamilies(selectionCandidates, standardRoles, request, selected, usedSources);
        AddEqualPowerAdversarialFamilies(selectionCandidates, standardRoles, request, selected, usedSources);
        AddCoreFamilies();
        AddWorldTowerCalibrationAnchors();

        foreach (var role in Enum.GetValues<CanonicalCooperativeRole>())
        {
            AddFamily(
                $"RoleSpecialist.{role}",
                selectionCandidates.OrderByDescending(CombinationScore)
                    .ThenBy(candidate => StableTieBreaker($"{role}:{candidate.Signature}", request.RandomSeed)),
                Enumerable.Repeat(role, request.PartySize).ToArray(),
                $"Stable Essence team materialized as an all-{role} specialist control.");
        }

        for (var index = 0; index < request.TeamsPerFamily; index++)
        {
            AddSelection(
                "NoEssence",
                CreateNoEssenceCombination(request.PartySize, index),
                standardRoles,
                "Synthetic legal control with canonical equipment and no equipped Essences.",
                true,
                null,
                null,
                selected,
                usedSources,
                request.MaximumEssenceOverlap,
                false);
        }

        return FinalizeSelections();

        IReadOnlyList<SelectedTeam> FinalizeSelections() => selected
            .OrderBy(selection => FamilyOrder(selection.Family))
            .ThenBy(selection => selection.Family, StringComparer.OrdinalIgnoreCase)
            .Select(selection => selection with
            {
                ContextEvidence = selection.Family.StartsWith(
                    "RoleSpecialist.",
                    StringComparison.OrdinalIgnoreCase)
                    ? []
                    : contextEvidence.GetValueOrDefault(selection.Source.Signature) ?? []
            })
            .ToArray();

        void AddCoreFamilies()
        {
            AddFamily(
                "Meta",
                selectionCandidates.OrderByDescending(ContextMinimumScore)
                    .ThenByDescending(ContextAverageScore)
                    .ThenByDescending(CombinationScore)
                    .ThenBy(candidate => StableTieBreaker(candidate.Signature, request.RandomSeed)),
                standardRoles,
                "Highest remaining evidence-qualified aggregate score within the portfolio constraints.");
            AddFamily(
                "Typical",
                selectionCandidates.OrderBy(candidate => Math.Abs(
                        ContextAverageScore(candidate) - TargetContextScore()))
                    .ThenBy(candidate => Math.Abs(CombinationScore(candidate) - median))
                    .ThenBy(candidate => StableTieBreaker(candidate.Signature, request.RandomSeed)),
                standardRoles,
                "Closest remaining evidence-qualified result to the finalist median within the portfolio constraints.");
            AddFamily(
                "WeakButLegal",
                selectionCandidates.OrderBy(ContextAverageScore)
                    .ThenBy(CombinationScore)
                    .ThenBy(candidate => StableTieBreaker(candidate.Signature, request.RandomSeed)),
                standardRoles,
                "Lowest remaining evidence-qualified legal finalist score within the portfolio constraints.");
        }

        void AddWorldTowerCalibrationPortfolio()
        {
            // Every discovery finalist has already been qualified against this exact scenario above.
            // Keep those legal teams in the calibration pool and use the direct search as a supplement;
            // otherwise a valid floor anchor can be discarded merely because it came from discovery.
            var available = selectionCandidates
                .Concat(directAnchorCandidates)
                .DistinctBy(candidate => candidate.Signature, StringComparer.Ordinal)
                .Where(IsBelowMaximumOnEveryFloor)
                .OrderBy(candidate => ContextEvidenceFor(candidate).Average(evidence => Math.Abs(
                    evidence.WinRate
                    - (WorldTowerProfileTargetContract.MinimumWinRate
                       + WorldTowerProfileTargetContract.MaximumWinRate) / 2d)))
                .ThenBy(candidate => candidate.Signature, StringComparer.Ordinal)
                .ToArray();
            if (available.Length < WorldTowerProfileCandidateQualifier.CalibrationPortfolioTeamCount)
            {
                throw new InvalidOperationException(
                    $"World Tower profile generation found only {available.Length} legal exact-context calibration "
                    + $"teams that stay below {WorldTowerProfileTargetContract.MaximumWinRate:P0} on every target floor; "
                    + $"{WorldTowerProfileCandidateQualifier.CalibrationPortfolioTeamCount} are required for floor(s) "
                    + $"{string.Join(", ", request.FloorNumbers)}.");
            }

            var calibrationTeams = new List<AbilityBalanceCombinationResult>();
            var uncoveredFloors = request.FloorNumbers.ToHashSet();
            while (uncoveredFloors.Count > 0)
            {
                var anchor = available
                    .Where(candidate => !calibrationTeams.Contains(candidate))
                    .Select(candidate => new
                    {
                        Candidate = candidate,
                        Floors = ContextEvidenceFor(candidate)
                            .Where(evidence => uncoveredFloors.Contains(evidence.FloorNumber)
                                               && WorldTowerProfileTargetContract.Contains(evidence.WinRate))
                            .Select(evidence => evidence.FloorNumber)
                            .Distinct()
                            .ToArray()
                    })
                    .Where(candidate => candidate.Floors.Length > 0)
                    .OrderByDescending(candidate => candidate.Floors.Length)
                    .ThenBy(candidate => candidate.Candidate.Signature, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (anchor is null)
                {
                    throw new InvalidOperationException(
                        "World Tower calibration portfolio has no strict >5% and <20% team for floor(s) "
                        + $"{string.Join(", ", uncoveredFloors.Order())}.");
                }
                calibrationTeams.Add(anchor.Candidate);
                foreach (var floorNumber in anchor.Floors)
                    uncoveredFloors.Remove(floorNumber);
            }
            calibrationTeams.AddRange(available
                .Where(candidate => !calibrationTeams.Contains(candidate))
                .Take(WorldTowerProfileCandidateQualifier.CalibrationPortfolioTeamCount - calibrationTeams.Count));

            foreach (var calibrationTeam in calibrationTeams)
            {
                var evidence = ContextEvidenceFor(calibrationTeam);
                var rates = evidence
                    .OrderBy(evidence => evidence.FloorNumber)
                    .Select(evidence => FormattableString.Invariant(
                        $"floor {evidence.FloorNumber} ({evidence.WinRate * 100d:0}%)"));
                var qualifyingFloors = evidence
                    .Where(result => WorldTowerProfileTargetContract.Contains(result.WinRate))
                    .Select(result => result.FloorNumber)
                    .Order()
                    .ToArray();
                var anchorDescription = qualifyingFloors.Length == 0
                    ? "It remains below the lower anchor boundary on every target floor."
                    : $"It supplies the strict >5% and <20% anchor on floor(s) {string.Join(", ", qualifyingFloors)}.";
                AddSelection(
                    "CalibrationTeam",
                    calibrationTeam,
                    standardRoles,
                    "Exact 100-sample production qualification selected this legal calibration team below the cap on every target floor: "
                    + $"{string.Join(", ", rates)}. {anchorDescription}",
                    false,
                    null,
                    null,
                    selected,
                    usedSources,
                    request.MaximumEssenceOverlap,
                    enforceDiversity: false,
                    usesDirectContextEvidence: true);
            }

            var noEssence = CreateNoEssenceCombination(request.PartySize, 0);
            if (!IsBelowMaximumOnEveryFloor(noEssence))
            {
                throw new InvalidOperationException(
                    $"The exact-context NoEssence control did not remain strictly below "
                    + $"{WorldTowerProfileTargetContract.MaximumWinRate:P0} on every target floor.");
            }
            AddSelection(
                "NoEssence",
                noEssence,
                standardRoles,
                "Synthetic legal control with canonical equipment and no equipped Essences.",
                true,
                null,
                null,
                selected,
                usedSources,
                request.MaximumEssenceOverlap,
                enforceDiversity: false);
        }

        void AddWorldTowerCalibrationAnchors()
        {
            if (request.ParsedContentType != CombatContentType.WorldTower
                || request.FloorNumbers.Count == 0)
            {
                return;
            }

            var coveredFloors = new HashSet<int>();
            foreach (var selection in selected.Where(IsFinalHomogeneousExpedition))
            {
                foreach (var floorNumber in QualifiedFloors(selection.Source))
                    coveredFloors.Add(floorNumber);
            }

            var uncoveredFloors = request.FloorNumbers
                .Where(floorNumber => !coveredFloors.Contains(floorNumber))
                .ToHashSet();
            while (uncoveredFloors.Count > 0)
            {
                var anchor = selectionCandidates.Concat(directAnchorCandidates)
                    .Where(candidate => !usedSources.Contains(candidate.Signature))
                    .Where(IsBelowMaximumOnEveryFloor)
                    .Select(candidate => new
                    {
                        Candidate = candidate,
                        Floors = QualifiedFloors(candidate)
                            .Where(uncoveredFloors.Contains)
                            .ToArray()
                    })
                    .Where(candidate => candidate.Floors.Length > 0)
                    .OrderByDescending(candidate => candidate.Floors.Length)
                    .ThenBy(candidate => TargetDistance(candidate.Candidate, candidate.Floors))
                    .ThenBy(candidate => StableTieBreaker(candidate.Candidate.Signature, request.RandomSeed))
                    .FirstOrDefault();
                if (anchor is null)
                {
                    throw new InvalidOperationException(
                        "World Tower profile generation could not select a legal calibration team with an "
                        + FormattableString.Invariant(
                            $"estimated win rate strictly above {WorldTowerProfileTargetContract.MinimumWinRate * 100d:0}% and below {WorldTowerProfileTargetContract.MaximumWinRate * 100d:0}% for floor(s) ")
                        + $"{string.Join(", ", uncoveredFloors.Order())}.");
                }

                var rates = EvidenceFor(anchor.Candidate)
                    .Where(evidence => anchor.Floors.Contains(evidence.FloorNumber))
                    .OrderBy(evidence => evidence.FloorNumber)
                    .Select(evidence => FormattableString.Invariant(
                        $"floor {evidence.FloorNumber} ({evidence.WinRate * 100d:0}%)"));
                var usesDirectContextEvidence = IsDirectAnchor(anchor.Candidate);
                AddSelection(
                    "CalibrationAnchor",
                    anchor.Candidate,
                    standardRoles,
                    "Exact production qualification selected this legal expedition as the >5% and <20% calibration anchor for "
                    + $"{string.Join(", ", rates)}.",
                    false,
                    null,
                    null,
                    selected,
                    usedSources,
                    request.MaximumEssenceOverlap,
                    enforceDiversity: false,
                    usesDirectContextEvidence: usesDirectContextEvidence);
                foreach (var floorNumber in anchor.Floors)
                    uncoveredFloors.Remove(floorNumber);
            }

            bool IsFinalHomogeneousExpedition(SelectedTeam selection) =>
                selection.Family is "Meta" or "Typical" or "WeakButLegal"
                    or "Budget" or "Counter" or "Countered" or "CalibrationAnchor"
                || request.PartyCount == 1
                && string.Equals(
                    selection.Family,
                    "EqualPowerAdversarial",
                    StringComparison.OrdinalIgnoreCase);

            IReadOnlyList<int> QualifiedFloors(AbilityBalanceCombinationResult candidate) =>
                EvidenceFor(candidate)
                    .Where(evidence => evidence.SampleCount >= request.ContextQualificationSampleCount
                        && evidence.UsesProductionRuntime
                        && evidence.AbilitiesStartOnCooldown
                        && WorldTowerProfileTargetContract.Contains(evidence.WinRate))
                    .Select(evidence => evidence.FloorNumber)
                    .Distinct()
                    .ToArray();

            IReadOnlyList<CombatCharacterProfileContextEvidence> EvidenceFor(
                AbilityBalanceCombinationResult candidate) =>
                contextEvidence.GetValueOrDefault(candidate.Signature) ?? [];

            bool IsDirectAnchor(AbilityBalanceCombinationResult candidate) =>
                directAnchorCandidates.Any(direct =>
                    direct.Signature.Equals(candidate.Signature, StringComparison.Ordinal));

            double TargetDistance(
                AbilityBalanceCombinationResult candidate,
                IReadOnlyCollection<int> floors)
            {
                var midpoint = (WorldTowerProfileTargetContract.MinimumWinRate
                    + WorldTowerProfileTargetContract.MaximumWinRate) / 2d;
                return EvidenceFor(candidate)
                    .Where(evidence => floors.Contains(evidence.FloorNumber))
                    .Average(evidence => Math.Abs(evidence.WinRate - midpoint));
            }
        }

        double ContextAverageScore(AbilityBalanceCombinationResult candidate) =>
            contextEvidence.TryGetValue(candidate.Signature, out var evidence) && evidence.Count > 0
                ? evidence.Average(result => result.WinRate)
                : CombinationScore(candidate);

        double ContextMinimumScore(AbilityBalanceCombinationResult candidate) =>
            contextEvidence.TryGetValue(candidate.Signature, out var evidence) && evidence.Count > 0
                ? evidence.Min(result => result.WinRate)
                : CombinationScore(candidate);

        double TargetContextScore() => request.FloorNumbers.Count == 0
            ? median
            : request.FloorNumbers.Average(floorNumber => floorNumber <= 10 ? 0.90d : 0.55d);

        bool IsBelowMaximumOnEveryFloor(AbilityBalanceCombinationResult candidate)
        {
            var evidence = ContextEvidenceFor(candidate);
            return request.FloorNumbers.All(floorNumber => evidence.Any(result =>
                result.FloorNumber == floorNumber
                && result.SampleCount >= request.ContextQualificationSampleCount
                && result.UsesProductionRuntime
                && result.AbilitiesStartOnCooldown
                && WorldTowerProfileTargetContract.IsBelowMaximum(result.WinRate)));
        }

        IReadOnlyList<CombatCharacterProfileContextEvidence> ContextEvidenceFor(
            AbilityBalanceCombinationResult candidate) =>
            contextEvidence.GetValueOrDefault(candidate.Signature) ?? [];

        void AddFamily(
            string family,
            IEnumerable<AbilityBalanceCombinationResult> ordered,
            IReadOnlyList<CanonicalCooperativeRole> roles,
            string reason)
        {
            var added = 0;
            foreach (var candidate in ordered)
            {
                if (!CanSelect(candidate, selected, usedSources, request.MaximumEssenceOverlap))
                    continue;
                AddSelection(family, candidate, roles, reason, false, null, null, selected, usedSources,
                    request.MaximumEssenceOverlap, true);
                if (++added == request.TeamsPerFamily)
                    return;
            }

            throw new InvalidOperationException(
                $"Unable to select {request.TeamsPerFamily} distinct {family} teams within the configured Essence-overlap limit.");
        }
    }

    private static int FamilyOrder(string family) => family switch
    {
        "Meta" => 0,
        "Typical" => 1,
        "WeakButLegal" => 2,
        "Budget" => 3,
        "Counter" => 4,
        "Countered" => 5,
        "EqualPowerAdversarial" => 6,
        "CalibrationAnchor" => 7,
        "CalibrationTeam" => 7,
        "NoEssence" => 8,
        _ when family.StartsWith("RoleSpecialist.", StringComparison.OrdinalIgnoreCase) => 7,
        _ => 9
    };

    private static void AddCounterFamilies(
        IReadOnlyList<AbilityBalanceCombinationResult> candidates,
        IReadOnlyList<CanonicalCooperativeRole> roles,
        NormalizedRequest request,
        ICollection<SelectedTeam> selected,
        ISet<string> usedSources)
    {
        const double minimumWinningScore = 0.60d;
        var bySignature = candidates.ToDictionary(candidate => candidate.Signature, StringComparer.Ordinal);
        var added = 0;
        foreach (var matchup in (request.Audit.FinalistMatchups ?? [])
                     .Where(matchup => IsDirectionalMatchupQualified(
                         matchup,
                         request.MinimumMatchupBattles,
                         minimumWinningScore))
                     .OrderByDescending(matchup => Math.Abs(matchup.FirstScore - 0.5d))
                     .ThenByDescending(matchup => matchup.Battles))
        {
            if (!bySignature.TryGetValue(matchup.FirstSignature, out var first)
                || !bySignature.TryGetValue(matchup.SecondSignature, out var second))
                continue;
            var winner = matchup.FirstScore >= 0.5d ? first : second;
            var loser = matchup.FirstScore >= 0.5d ? second : first;
            if (!CanSelectPair(winner, loser, selected, usedSources, request.MaximumEssenceOverlap))
                continue;
            var winningScore = Math.Max(matchup.FirstScore, 1d - matchup.FirstScore);
            var winningConfidence = WilsonInterval(winningScore, matchup.Battles);
            var losingScore = 1d - winningScore;
            var losingConfidence = (Lower: 1d - winningConfidence.Upper, Upper: 1d - winningConfidence.Lower);

            AddSelection("Counter", winner, roles,
                $"Won the audited head-to-head matchup with {winningScore:P1} score and a directional 95% confidence interval.",
                false, loser.Signature,
                new MatchupEvidence(matchup.Battles, winningScore, winningConfidence.Lower, winningConfidence.Upper),
                selected, usedSources, request.MaximumEssenceOverlap, true);
            AddSelection("Countered", loser, roles,
                $"Lost the audited head-to-head matchup with {losingScore:P1} score and a directional 95% confidence interval.",
                false, winner.Signature,
                new MatchupEvidence(matchup.Battles, losingScore, losingConfidence.Lower, losingConfidence.Upper),
                selected, usedSources, request.MaximumEssenceOverlap, false);
            if (++added == request.TeamsPerFamily)
                return;
        }

        throw new InvalidOperationException(
            "The audit does not contain enough diverse, evidence-qualified head-to-head matchups for Counter and Countered profiles.");
    }

    private static void AddEqualPowerAdversarialFamilies(
        IReadOnlyList<AbilityBalanceCombinationResult> candidates,
        IReadOnlyList<CanonicalCooperativeRole> roles,
        NormalizedRequest request,
        ICollection<SelectedTeam> selected,
        ISet<string> usedSources)
    {
        const double maximumAggregateScoreDifference = 0.05d;
        const double minimumDirectScoreDifference = 0.10d;
        var bySignature = candidates.ToDictionary(candidate => candidate.Signature, StringComparer.Ordinal);
        var addedPairs = 0;
        foreach (var matchup in (request.Audit.FinalistMatchups ?? [])
                     .Where(matchup => IsDirectionalMatchupQualified(
                         matchup,
                         request.MinimumMatchupBattles,
                         0.5d + minimumDirectScoreDifference))
                     .OrderByDescending(matchup => Math.Abs(matchup.FirstScore - 0.5d))
                     .ThenByDescending(matchup => matchup.Battles))
        {
            if (!bySignature.TryGetValue(matchup.FirstSignature, out var first)
                || !bySignature.TryGetValue(matchup.SecondSignature, out var second)
                || Math.Abs(CombinationScore(first) - CombinationScore(second)) > maximumAggregateScoreDifference
                || !CanSelectPair(first, second, selected, usedSources, request.MaximumEssenceOverlap))
                continue;
            var firstConfidence = WilsonInterval(matchup.FirstScore, matchup.Battles);
            var secondConfidence = (Lower: 1d - firstConfidence.Upper, Upper: 1d - firstConfidence.Lower);

            const string reason = "Aggregate finalist scores are within five percentage points, but the direct matchup is intentionally adversarial.";
            AddSelection("EqualPowerAdversarial", first, roles, reason, false, second.Signature,
                new MatchupEvidence(matchup.Battles, matchup.FirstScore, firstConfidence.Lower, firstConfidence.Upper),
                selected, usedSources, request.MaximumEssenceOverlap, true);
            AddSelection("EqualPowerAdversarial", second, roles, reason, false, first.Signature,
                new MatchupEvidence(matchup.Battles, 1d - matchup.FirstScore, secondConfidence.Lower, secondConfidence.Upper),
                selected, usedSources, request.MaximumEssenceOverlap, false);
            if (++addedPairs == request.TeamsPerFamily)
                return;
        }

        throw new InvalidOperationException(
            "The audit does not contain enough diverse equal-score adversarial matchup pairs.");
    }

    private static void AddSelection(
        string family,
        AbilityBalanceCombinationResult source,
        IReadOnlyList<CanonicalCooperativeRole> roles,
        string reason,
        bool isSyntheticControl,
        string? adversarySourceSignature,
        MatchupEvidence? adversaryEvidence,
        ICollection<SelectedTeam> selected,
        ISet<string> usedSources,
        double maximumEssenceOverlap,
        bool enforceDiversity,
        bool usesDirectContextEvidence = false)
    {
        var nearestOverlap = NearestOverlap(source, selected);
        if (enforceDiversity && nearestOverlap > maximumEssenceOverlap)
            throw new InvalidOperationException($"Profile '{source.Signature}' exceeds the configured Essence-overlap limit.");
        selected.Add(new SelectedTeam(family, source, roles, reason, isSyntheticControl,
            adversarySourceSignature, nearestOverlap, adversaryEvidence,
            UsesDirectContextEvidence: usesDirectContextEvidence));
        usedSources.Add(source.Signature);
    }

    private static bool CanSelect(
        AbilityBalanceCombinationResult candidate,
        IEnumerable<SelectedTeam> selected,
        ISet<string> usedSources,
        double maximumEssenceOverlap) =>
        !usedSources.Contains(candidate.Signature)
        && NearestOverlap(candidate, selected) <= maximumEssenceOverlap;

    private static bool CanSelectPair(
        AbilityBalanceCombinationResult first,
        AbilityBalanceCombinationResult second,
        IEnumerable<SelectedTeam> selected,
        ISet<string> usedSources,
        double maximumEssenceOverlap) =>
        CanSelect(first, selected, usedSources, maximumEssenceOverlap)
        && CanSelect(second, selected, usedSources, maximumEssenceOverlap)
        && EssenceOverlap(first, second) <= maximumEssenceOverlap;

    private bool IsBudgetCombination(AbilityBalanceCombinationResult combination) =>
        combination.Participants
            .SelectMany(participant => participant.EssenceIds)
            .All(essenceId => essenceDefinitions.GetById(essenceId)?.Rarity == Rarity.Common);

    private static bool IsDirectionalMatchupQualified(
        AbilityBalanceMatchupResult matchup,
        int minimumBattles,
        double minimumWinningScore)
    {
        if (matchup.Battles < minimumBattles)
            return false;
        var winningScore = Math.Max(matchup.FirstScore, 1d - matchup.FirstScore);
        if (winningScore < minimumWinningScore)
            return false;
        return WilsonInterval(winningScore, matchup.Battles).Lower > 0.5d;
    }

    private static bool IsEvidenceEligible(
        AbilityBalanceCombinationResult candidate,
        NormalizedRequest request)
    {
        if (candidate.Battles < request.MinimumSourceBattles)
            return false;
        var confidence = WilsonInterval(CombinationScore(candidate), candidate.Battles);
        if (confidence.Upper - confidence.Lower > request.MaximumConfidenceWidth95)
            return false;
        if (!request.RequireMultiSeedStability)
            return true;

        var requiredSeeds = request.Audit.RandomSeeds?.Distinct().Count() ?? 0;
        var seedResults = candidate.SeedResults?
            .Where(result => result.Battles > 0 && double.IsFinite(result.Score))
            .GroupBy(result => result.RandomSeed)
            .Select(group => group.First())
            .ToArray() ?? [];
        return seedResults.Length == requiredSeeds
            && seedResults.Max(result => result.Score) - seedResults.Min(result => result.Score)
            <= request.MaximumSeedScoreSpread;
    }

    private static AbilityBalanceCombinationResult CreateNoEssenceCombination(int teamSize, int index) =>
        new(
            $"synthetic:no-essence:{teamSize}:{index}",
            "No Essence control",
            CanonicalCooperativeRosterCatalog.CreateParty(teamSize)
                .Select(slot => new AbilityBalanceParticipantLoadout([], slot.Role.ToString()))
                .ToArray(),
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private static double NearestOverlap(
        AbilityBalanceCombinationResult candidate,
        IEnumerable<SelectedTeam> selected)
    {
        var values = selected
            .Where(selection => !selection.IsSyntheticControl)
            .Select(selection => EssenceOverlap(candidate, selection.Source))
            .ToArray();
        return values.Length == 0 ? 0d : values.Max();
    }

    private static double EssenceOverlap(
        AbilityBalanceCombinationResult first,
        AbilityBalanceCombinationResult second)
    {
        var firstIds = first.Participants.SelectMany(participant => participant.EssenceIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secondIds = second.Participants.SelectMany(participant => participant.EssenceIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var union = firstIds.Union(secondIds, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0
            ? 0d
            : firstIds.Intersect(secondIds, StringComparer.OrdinalIgnoreCase).Count() / (double)union;
    }

    private static double CombinationScore(AbilityBalanceCombinationResult combination) =>
        combination.Battles == 0 ? 0d : (combination.Wins + combination.Draws * 0.5d) / combination.Battles;

    private static (double Lower, double Upper) WilsonInterval(double score, int battles)
    {
        if (battles <= 0)
            return (0d, 1d);
        const double z = 1.959963984540054d;
        var denominator = 1d + z * z / battles;
        var center = (score + z * z / (2d * battles)) / denominator;
        var margin = z * Math.Sqrt((score * (1d - score) + z * z / (4d * battles)) / battles) / denominator;
        var lower = Math.Max(0d, center - margin);
        var upper = Math.Min(1d, center + margin);
        return (Math.Min(score, lower), Math.Max(score, upper));
    }

    private static double RequireUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be between zero and one.");
        return value;
    }

    private static ulong StableTieBreaker(string value, int randomSeed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{randomSeed}:{value}"));
        return BitConverter.ToUInt64(hash);
    }

    private enum ProfilePortfolioMode
    {
        Core,
        Expanded
    }

    private sealed record SelectedTeam(
        string Family,
        AbilityBalanceCombinationResult Source,
        IReadOnlyList<CanonicalCooperativeRole> Roles,
        string SelectionReason,
        bool IsSyntheticControl,
        string? AdversarySourceSignature,
        double NearestSelectedEssenceOverlap,
        MatchupEvidence? AdversaryEvidence,
        IReadOnlyList<CombatCharacterProfileContextEvidence>? ContextEvidence = null,
        bool UsesDirectContextEvidence = false);

    private sealed record SelectedExpedition(
        string Family,
        IReadOnlyList<SelectedTeam> Parties,
        string SelectionReason);

    private sealed record MatchupEvidence(
        int Battles,
        double Score,
        double ConfidenceLower95,
        double ConfidenceUpper95);

    private sealed record NormalizedRequest(
        string AuditId,
        AbilityBalanceAuditReport Audit,
        string ContentType,
        CombatContentType ParsedContentType,
        int TeamsPerFamily,
        int RandomSeed,
        ProfilePortfolioMode PortfolioMode,
        int MinimumSourceBattles,
        int MinimumMatchupBattles,
        double MaximumConfidenceWidth95,
        double MaximumSeedScoreSpread,
        double MaximumEssenceOverlap,
        bool RequireMultiSeedStability,
        int TeamSize,
        int DiscoveryTeamSize,
        int PartySize,
        int PartyCount,
        CanonicalEquipmentProgressionRung ProgressionRung,
        CombatCharacterProfileScenario Scenario,
        IReadOnlyList<int> FloorNumbers,
        int ContextQualificationSampleCount);
}
