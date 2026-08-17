using Domain.Models.CharacterActions.Sessions;

namespace Services.LL.CharacterActions;

/// <summary>
/// Compacts internal tempering batches into one client response. The complete
/// summary is retained while detailed outcomes are limited to the newest entries
/// consumed by the tempering UI.
/// </summary>
internal sealed class TemperingSessionAccumulator
{
    private const int MaximumDetailedOutcomes = 5;
    private TemperingSession? _session;

    public void Add(TemperingSession batch)
    {
        if (_session is null)
        {
            _session = new TemperingSession
            {
                From = batch.From,
                To = batch.To,
                TemperingSummary = CopySummary(batch.TemperingSummary),
                Outcomes = SelectLatestOutcomes(batch.Outcomes)
            };
            return;
        }

        _session.To = batch.To;
        _session.TemperingSummary = MergeSummaries(
            _session.TemperingSummary,
            batch.TemperingSummary);
        _session.Outcomes = SelectLatestOutcomes(
            _session.Outcomes.Concat(batch.Outcomes));
    }

    public TemperingSession Build() =>
        _session ?? throw new InvalidOperationException(
            "No tempering batch was accumulated.");

    private static TemperingSummary CopySummary(TemperingSummary source) =>
        new()
        {
            TotalItemsCrafted = source.TotalItemsCrafted,
            Masterpieces = source.Masterpieces,
            LevelingItems = source.LevelingItems,
            CursedOutcomes = source.CursedOutcomes,
            QualityIncreases = source.QualityIncreases,
            TotalActions = source.TotalActions,
            TotalSoulstones = source.TotalSoulstones,
            CraftingExperience = source.CraftingExperience
        };

    private static TemperingSummary MergeSummaries(
        TemperingSummary first,
        TemperingSummary second) =>
        new()
        {
            TotalItemsCrafted = checked(first.TotalItemsCrafted + second.TotalItemsCrafted),
            Masterpieces = checked(first.Masterpieces + second.Masterpieces),
            LevelingItems = checked(first.LevelingItems + second.LevelingItems),
            CursedOutcomes = checked(first.CursedOutcomes + second.CursedOutcomes),
            QualityIncreases = checked(first.QualityIncreases + second.QualityIncreases),
            TotalActions = checked(first.TotalActions + second.TotalActions),
            TotalSoulstones = checked(first.TotalSoulstones + second.TotalSoulstones),
            CraftingExperience = checked(first.CraftingExperience + second.CraftingExperience)
        };

    private static List<TemperingOutcomeEntry> SelectLatestOutcomes(
        IEnumerable<TemperingOutcomeEntry> outcomes) =>
        outcomes
            .OrderByDescending(outcome => outcome.OccurredAt)
            .ThenBy(outcome => outcome.Id)
            .Take(MaximumDetailedOutcomes)
            .ToList();
}
