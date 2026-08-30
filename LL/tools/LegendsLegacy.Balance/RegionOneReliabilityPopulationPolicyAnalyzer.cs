namespace LegendsLegacy.Balance;

public enum RegionOneReliabilityPopulationPolicyVerdict
{
    Confirmed = 0,
    PopulationSensitive = 1,
    Rejected = 2,
    InsufficientEvidence = 3
}

public sealed record RegionOneReliabilityPopulationPolicyOptions
{
    public int MinimumDistinctPopulations { get; init; } = 3;

    public RegionOneReliabilityPopulationPolicyOptions Validate()
    {
        if (MinimumDistinctPopulations is < 2 or > 10)
            throw new ArgumentOutOfRangeException(nameof(MinimumDistinctPopulations));

        return this;
    }
}

public sealed record RegionOneReliabilityPopulationFaultSnapshot(
    RegionOneReliabilityFaultKind Fault,
    int RequiredPopulationCount,
    int ObservedPopulationCount,
    int PassCount,
    int InconclusiveCount,
    int FailCount,
    int UnavailableCount,
    bool PhysicalReachReplicated,
    bool DiagnosticRecoveryReplicated,
    RegionOneReliabilityPopulationPolicyVerdict DiagnosticVerdict,
    RegionOneReliabilityPopulationPolicyVerdict? FamilyContractVerdict,
    RegionOneReliabilityPopulationPolicyVerdict Verdict,
    string Assessment);

public sealed record RegionOneReliabilityUnsupportedPopulationFaultSnapshot(
    string Fault,
    int ObservedPopulationCount,
    int UnavailableCount,
    RegionOneReliabilityPopulationPolicyVerdict Verdict,
    string Assessment);

public sealed record RegionOneReliabilityPopulationPolicySnapshot(
    int AlgorithmVersion,
    RegionOneReliabilityPopulationPolicyOptions Options,
    int ObservedPopulationCount,
    IReadOnlyList<int> Seeds,
    bool ProtocolCompatible,
    bool ProductionContentUnmodified,
    RegionOneReliabilityPopulationPolicyVerdict Verdict,
    bool ExpansionEligible,
    IReadOnlyList<RegionOneReliabilityPopulationFaultSnapshot> Faults,
    IReadOnlyList<RegionOneReliabilityUnsupportedPopulationFaultSnapshot> UnsupportedFaults,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Applies the reviewed cross-population replication policy to already-completed
/// Region 1 reliability studies. It performs no combat and never changes a
/// per-population verdict.
/// </summary>
public sealed class RegionOneReliabilityPopulationPolicyAnalyzer
{
    public const int AlgorithmVersion = 3;

    public RegionOneReliabilityPopulationPolicySnapshot Analyze(
        IReadOnlyList<RegionOneReliabilityStudySnapshot> studies,
        RegionOneReliabilityPopulationPolicyOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(studies);
        var options = (requestedOptions ?? new RegionOneReliabilityPopulationPolicyOptions()).Validate();
        if (studies.Any(study => study is null))
            throw new ArgumentException("Population studies cannot contain null entries.", nameof(studies));

        var enabledStudies = studies
            .Where(study => study.Options.Enabled && study.Verdict != RegionOneReliabilityVerdict.Disabled)
            .OrderBy(study => study.Seed)
            .ToArray();
        var duplicateSeeds = enabledStudies
            .GroupBy(study => study.Seed)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateSeeds.Length > 0)
        {
            throw new ArgumentException(
                $"Population studies must use distinct master seeds; duplicate seeds: {string.Join(", ", duplicateSeeds)}.",
                nameof(studies));
        }

        var populationProtocolsSpecified = enabledStudies.All(study => study.PopulationProtocol is not null);
        var protocolCompatible = populationProtocolsSpecified
                                 && enabledStudies
                                     .Select(CreateProtocolKey)
                                     .Distinct(StringComparer.Ordinal)
                                     .Count() <= 1;
        var productionContentUnmodified = enabledStudies.All(study => !study.ProductionContentModified);
        var evidenceSetValid = protocolCompatible && productionContentUnmodified;

        var faults = Enum.GetValues<RegionOneReliabilityFaultKind>()
            .Select(fault => EvaluateFault(
                fault,
                enabledStudies,
                options.MinimumDistinctPopulations,
                evidenceSetValid))
            .ToArray();
        var unsupported = enabledStudies
            .SelectMany(study => study.UnsupportedFaults)
            .GroupBy(fault => fault.Fault, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var unavailableCount = group.Count(fault => fault.Verdict == RegionOneReliabilityVerdict.Unavailable);
                return new RegionOneReliabilityUnsupportedPopulationFaultSnapshot(
                    group.Key,
                    group.Count(),
                    unavailableCount,
                    RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence,
                    $"{group.Key} remains unsupported in {unavailableCount}/{group.Count()} observed populations.");
            })
            .ToArray();
        var verdict = faults.Any(fault => fault.Verdict == RegionOneReliabilityPopulationPolicyVerdict.Rejected)
            ? RegionOneReliabilityPopulationPolicyVerdict.Rejected
            : faults.Any(fault => fault.Verdict == RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence)
                ? RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence
                : faults.Any(fault => fault.Verdict == RegionOneReliabilityPopulationPolicyVerdict.PopulationSensitive)
                    ? RegionOneReliabilityPopulationPolicyVerdict.PopulationSensitive
                    : RegionOneReliabilityPopulationPolicyVerdict.Confirmed;
        var expansionEligible = evidenceSetValid
                                && verdict == RegionOneReliabilityPopulationPolicyVerdict.Confirmed
                                && unsupported.Length == 0;
        var warnings = new List<string>();
        if (enabledStudies.Length < options.MinimumDistinctPopulations)
        {
            warnings.Add(
                $"Only {enabledStudies.Length} distinct enabled populations were supplied; at least {options.MinimumDistinctPopulations} are required.");
        }
        if (studies.Count != enabledStudies.Length)
            warnings.Add($"Ignored {studies.Count - enabledStudies.Length} disabled population studies.");
        if (!populationProtocolsSpecified)
        {
            warnings.Add(
                "At least one population study lacks upstream optimizer/cohort protocol provenance and cannot form a replication panel.");
        }
        else if (!protocolCompatible)
        {
            warnings.Add(
                "Population studies use different analyzer versions, reliability-study options, or upstream optimizer/cohort protocols and cannot form one replication panel.");
        }
        if (!productionContentUnmodified)
            warnings.Add("At least one population study reports modified production content and cannot support the controlled replication claim.");
        if (unsupported.Length > 0)
            warnings.Add("Unsupported controlled faults remain outside the replicated supported-fault verdict and block expansion eligibility.");
        if (faults.Any(fault => fault.Verdict == RegionOneReliabilityPopulationPolicyVerdict.PopulationSensitive))
            warnings.Add("At least one supported fault changes conclusion across complete populations; do not replace that family-response contract with an aggregate majority pass.");
        if (faults.Any(fault => fault.FamilyContractVerdict == RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence))
            warnings.Add("At least one diagnostic replicates without an approved family contract; mechanic recovery must not be presented as affected-family identification.");

        return new RegionOneReliabilityPopulationPolicySnapshot(
            AlgorithmVersion,
            options,
            enabledStudies.Length,
            enabledStudies.Select(study => study.Seed).ToArray(),
            protocolCompatible,
            productionContentUnmodified,
            verdict,
            expansionEligible,
            faults,
            unsupported,
            warnings);
    }

    private static RegionOneReliabilityPopulationFaultSnapshot EvaluateFault(
        RegionOneReliabilityFaultKind fault,
        IReadOnlyList<RegionOneReliabilityStudySnapshot> studies,
        int requiredPopulationCount,
        bool evidenceSetValid)
    {
        var observations = studies
            .Select(study => study.Faults.SingleOrDefault(candidate => candidate.Fault == fault))
            .Where(observation => observation is not null)
            .Cast<RegionOneReliabilityFaultSnapshot>()
            .ToArray();
        var passCount = observations.Count(observation => observation.DiagnosticVerdict == RegionOneReliabilityVerdict.Pass);
        var inconclusiveCount = observations.Count(observation =>
            observation.DiagnosticVerdict == RegionOneReliabilityVerdict.Inconclusive);
        var failCount = observations.Count(observation => observation.DiagnosticVerdict == RegionOneReliabilityVerdict.Fail);
        var unavailableCount = observations.Count(observation =>
            observation.DiagnosticVerdict is RegionOneReliabilityVerdict.Unavailable or RegionOneReliabilityVerdict.Disabled);
        var physicalReachReplicated = observations.Length == studies.Count
                                      && observations.All(observation => observation.InjectionReachedPhysicalTelemetry);
        var diagnosticRecoveryReplicated = observations.Length == studies.Count
                                           && observations.All(observation => observation.DiagnosticRecoveryMatched);
        var diagnosticVerdict = AggregateDiagnosticVerdict(
            evidenceSetValid,
            studies.Count,
            requiredPopulationCount,
            observations.Length,
            passCount,
            inconclusiveCount,
            failCount,
            unavailableCount);
        var familyContracts = observations
            .Where(observation => observation.FamilyContractVerdict !=
                                  RegionOneReliabilityFamilyContractVerdict.NotApplicable)
            .Select(observation => observation.FamilyContractVerdict)
            .ToArray();
        RegionOneReliabilityPopulationPolicyVerdict? familyContractVerdict = familyContracts.Length == 0
            ? null
            : !evidenceSetValid || studies.Count < requiredPopulationCount
                                 || observations.Length != studies.Count
                                 || familyContracts.Length != observations.Length
                                 || familyContracts.Any(contract =>
                                     contract == RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence)
                ? RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence
                : familyContracts.All(contract => contract == RegionOneReliabilityFamilyContractVerdict.Pass)
                    ? RegionOneReliabilityPopulationPolicyVerdict.Confirmed
                    : familyContracts.Any(contract => contract == RegionOneReliabilityFamilyContractVerdict.Pass)
                        ? RegionOneReliabilityPopulationPolicyVerdict.PopulationSensitive
                        : RegionOneReliabilityPopulationPolicyVerdict.Rejected;
        var verdict = diagnosticVerdict != RegionOneReliabilityPopulationPolicyVerdict.Confirmed
            ? diagnosticVerdict
            : familyContractVerdict is null or RegionOneReliabilityPopulationPolicyVerdict.Confirmed
                ? RegionOneReliabilityPopulationPolicyVerdict.Confirmed
                : familyContractVerdict.Value;

        var assessment = $"Diagnostic: {Assessment(diagnosticVerdict, passCount, inconclusiveCount, failCount, unavailableCount, observations.Length, studies.Count, requiredPopulationCount)}";
        if (familyContractVerdict.HasValue)
            assessment += $" Family contract: {familyContractVerdict.Value}.";

        return new RegionOneReliabilityPopulationFaultSnapshot(
            fault,
            requiredPopulationCount,
            observations.Length,
            passCount,
            inconclusiveCount,
            failCount,
            unavailableCount,
            physicalReachReplicated,
            diagnosticRecoveryReplicated,
            diagnosticVerdict,
            familyContractVerdict,
            verdict,
            assessment);
    }

    private static RegionOneReliabilityPopulationPolicyVerdict AggregateDiagnosticVerdict(
        bool evidenceSetValid,
        int studyCount,
        int requiredPopulationCount,
        int observationCount,
        int passCount,
        int inconclusiveCount,
        int failCount,
        int unavailableCount)
    {
        if (!evidenceSetValid
            || studyCount < requiredPopulationCount
            || observationCount != studyCount
            || unavailableCount > 0)
        {
            return RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence;
        }
        if (failCount > 0 || passCount == 0)
            return RegionOneReliabilityPopulationPolicyVerdict.Rejected;
        return passCount != observationCount
            ? RegionOneReliabilityPopulationPolicyVerdict.PopulationSensitive
            : RegionOneReliabilityPopulationPolicyVerdict.Confirmed;
    }

    private static string Assessment(
        RegionOneReliabilityPopulationPolicyVerdict verdict,
        int passCount,
        int inconclusiveCount,
        int failCount,
        int unavailableCount,
        int observationCount,
        int studyCount,
        int requiredPopulationCount) =>
        verdict switch
        {
            RegionOneReliabilityPopulationPolicyVerdict.Confirmed =>
                $"confirmed in all {observationCount} supplied populations.",
            RegionOneReliabilityPopulationPolicyVerdict.PopulationSensitive =>
                $"population-sensitive with {passCount}/{observationCount} pass and {inconclusiveCount}/{observationCount} inconclusive.",
            RegionOneReliabilityPopulationPolicyVerdict.Rejected =>
                $"rejected with {passCount}/{observationCount - unavailableCount} complete populations passing, {inconclusiveCount} inconclusive, and {failCount} failing.",
            _ =>
                $"insufficient evidence from {observationCount}/{studyCount} supplied populations with {unavailableCount} unavailable; at least {requiredPopulationCount} distinct protocol-compatible, production-unmodified populations are required."
        };

    private static string CreateProtocolKey(RegionOneReliabilityStudySnapshot study) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            study.AlgorithmVersion,
            study.Options,
            study.PopulationProtocol
        });
}
