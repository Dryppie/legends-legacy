namespace BalanceHarness;

public sealed partial class TowerDashboardService
{
    private static string ValidationLabel(TowerBossValidationEvidence evidence) => evidence.Role switch {
        "anchor" => "Historical anchor", "previous-primary" => "Previous discovery primary",
        "previous-exploratory" => "Previously exploratory recipe", _ => evidence.Label };

    private TowerBossValidationSet? BossValidationSet(int floor, int slots)
    {
        if (floor is < 1 or > 15 || slots is < 4 or > 10)
            throw new InvalidDataException("Choose a released floor and a four-to-ten Essence budget.");
        var catalog = TowerBossValidationReferences.Read(catalogsRoot);
        if (catalog is null || catalog.Floor != floor || catalog.Budget.EssenceSlots != slots) return null;
        return TowerBossValidationReferences.Load(apiRoot, catalogsRoot);
    }

    public object BossValidationList(int floor, int slots)
    {
        var validation = BossValidationSet(floor, slots);
        if (validation is null) return new { Available = false };
        var catalog = validation.Catalog;
        return new { Available = true, catalog.Id, catalog.Floor, catalog.Budget, validation.EvidenceStatus,
            catalog.Provenance.PackageId, Recipes = catalog.Evidence.Select(row => new {
                row.Id, row.Role, Label = ValidationLabel(row), row.Wins, Samples = row.Outcomes.Count }).ToArray() };
    }

    public object BossValidationDetails(int floor, int slots, string id)
    {
        var validation = BossValidationSet(floor, slots)
            ?? throw new InvalidDataException("No fixed validation exists for this boss and budget.");
        var catalog = validation.Catalog;
        var evidence = catalog.Evidence.SingleOrDefault(row => row.Id == id)
            ?? throw new InvalidDataException("Unknown fixed validation recipe.");
        var summary = TowerBossValidationReferences.Summary(evidence);
        var comparisons = TowerBossValidationReferences.PairedComparisons(catalog)
            .Where(pair => pair.CandidateId == id || pair.ReferenceId == id).Select(pair =>
            {
                var candidate = pair.CandidateId == id;
                var other = catalog.Evidence.Single(row => row.Id == (candidate ? pair.ReferenceId : pair.CandidateId));
                return new { other.Id, Label = ValidationLabel(other), Gained = candidate ? pair.Gained : pair.Lost,
                    Lost = candidate ? pair.Lost : pair.Gained, pair.BothWin, pair.Samples };
            }).ToArray();
        return new { catalog.Id, catalog.Floor, catalog.Budget, catalog.Provenance, validation.EvidenceStatus,
            Evidence = new { evidence.Id, evidence.Role, Label = ValidationLabel(evidence), HistoricalLabel = evidence.Label,
                evidence.TargetRecipe, evidence.TargetRecipeHash,
                evidence.SourceHashes, Summary = new { summary.Samples, summary.Wins, summary.Defeats, summary.Draws,
                    summary.Wilson95, GuardianHealth = summary.MeanGuardianHealthPercent, Survival = summary.MeanPartySurvivalPercent,
                    FriendlyHealing = summary.MeanRecovery.FriendlyReportedHealing,
                    FriendlyRegeneration = summary.MeanRecovery.FriendlyEffectiveRegeneration,
                    GuardianHealing = summary.MeanRecovery.GuardianReportedHealing,
                    GuardianRegeneration = summary.MeanRecovery.GuardianEffectiveRegeneration },
                Observations = evidence.Outcomes.Select(row => new { row.Trial, row.Seed, row.Outcome, row.DurationSeconds,
                    GuardianHealth = row.GuardianHealthPercent, Survival = row.PartySurvivalPercent,
                    row.FriendlyReportedHealing, row.FriendlyEffectiveRegeneration, row.GuardianReportedHealing,
                    row.GuardianEffectiveRegeneration }).ToArray() }, Comparisons = comparisons };
    }

    public TowerScenario BossValidationRecipe(int floor, int slots, string id)
    {
        var validation = BossValidationSet(floor, slots)
            ?? throw new InvalidDataException("No fixed validation exists for this boss and budget.");
        return validation.Catalog.Evidence.SingleOrDefault(row => row.Id == id)?.TargetRecipe
            ?? throw new InvalidDataException("Unknown fixed validation recipe.");
    }
}
