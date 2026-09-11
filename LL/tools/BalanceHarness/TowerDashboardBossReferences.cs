namespace BalanceHarness;

public sealed partial class TowerDashboardService
{
    private TowerBossReferenceSet? BossReferenceSet(int floor, int slots)
    {
        if (floor is < 1 or > 15 || slots is < 4 or > 10)
            throw new InvalidDataException("Choose a released floor and a four-to-ten Essence budget.");
        return TowerBossReferences.Load(apiRoot, catalogsRoot, floor, slots);
    }

    // Keep the selector small; full historical matrices and recipes load only when selected.
    public object BossReferenceList(int floor, int slots)
    {
        var references = BossReferenceSet(floor, slots);
        if (references is null) return new { Available = false };
        return new
        {
            Available = true, references.ReferenceSetId, references.AnchorId, references.AnchorLabel,
            references.Budget, references.EvidenceStatus, references.Provenance.PackageId,
            Recipes = references.Evidence.Select(row =>
            {
                var target = row.Confirmation.Cells.First(cell => cell.Floor == floor);
                return new { row.Id, row.Label, IsAnchor = row.Id == references.AnchorId,
                    Wins = target.Clears.Count(clear => clear), Samples = target.Clears.Count };
            }).ToArray()
        };
    }

    public object BossReferenceDetails(int floor, int slots, string id)
    {
        var references = BossReferenceSet(floor, slots)
            ?? throw new InvalidDataException("No historical reference set exists for this budget.");
        var evidence = references.Evidence.SingleOrDefault(row => row.Id == id)
            ?? throw new InvalidDataException("Unknown historical recipe in this reference set.");
        return new { references.ReferenceSetId, references.AnchorId, references.Budget,
            references.EvidenceStatus, references.Provenance, references.PriorProvenance, Evidence = evidence };
    }

    public TowerScenario BossReferenceRecipe(int floor, int slots, string id)
    {
        var references = BossReferenceSet(floor, slots)
            ?? throw new InvalidDataException("No historical reference set exists for this budget.");
        return references.Evidence.SingleOrDefault(row => row.Id == id)?.TargetRecipe
            ?? throw new InvalidDataException("Unknown historical recipe in this reference set.");
    }
}
