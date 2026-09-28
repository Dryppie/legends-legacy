namespace Domain.Models.Analytics;

public sealed record ItemizationOpportunity(string Source, string Slot, int Tier, int StatVersion,
    int Awarded, int DistinctRecipients, int EquippedWithinSevenDays, int SustainedUse,
    int DismantledWithinSevenDays, double? MedianHoursToFirstEquip, double EquippedHours)
{
    public int AwardsWithCompleteChoices { get; init; }
    public int EligibleAwards { get; init; }
    public int EligibleAwardsWithAlternatives { get; init; }
    public int EligibleAwardsEquipped { get; init; }
    public int EligibleAwardsWithAlternativesEquipped { get; init; }
    public string Denominator => "Award eligibility is evaluated at acquisition, with same-batch rewards included among alternatives. Eligible rates exclude missing or incomplete snapshots; aggregate award counts retain them. Alternatives are compatible single-item replacements, including equipped gear and valid guild loans. Sustained use requires at least 60 seconds of an observed equip interval. Auto-selected activity builds are represented in battle cohorts, not these manual equip durations.";

    public static IReadOnlyList<ItemizationOpportunity> Create(DateOnly reportDay, IEnumerable<ItemizationObservation> observations)
    {
        var events = observations.DistinctBy(x => x.Id).OrderBy(x => x.OccurredAtUtc).ToArray();
        var cohortDay = reportDay.AddDays(-7);
        var offers = events.Where(x => x.Kind == "awarded" && x.ItemId.HasValue && x.Equipment is not null
            && DateOnly.FromDateTime(x.OccurredAtUtc.UtcDateTime) == cohortDay).ToArray();
        var trajectories = offers.Select(offer =>
        {
            var end = offer.OccurredAtUtc.AddDays(7);
            var actions = events.Where(x => x.CharacterId == offer.CharacterId && x.ItemId == offer.ItemId
                && x.OccurredAtUtc >= offer.OccurredAtUtc && x.OccurredAtUtc <= end).ToArray();
            DateTimeOffset? equippedAt = null, first = null;
            var seconds = 0d;
            foreach (var action in actions)
            {
                if (action.Kind == "equipped" && equippedAt is null) { equippedAt = action.OccurredAtUtc; first ??= equippedAt; }
                if (action.Kind is "unequipped" or "Dismantle" or "transferred" or "listed" && equippedAt.HasValue)
                { seconds += (action.OccurredAtUtc - equippedAt.Value).TotalSeconds; equippedAt = null; }
            }
            if (equippedAt.HasValue) seconds += (end - equippedAt.Value).TotalSeconds;
            return new { Offer = offer, FirstHours = first.HasValue ? (double?)(first.Value - offer.OccurredAtUtc).TotalHours : null,
                Seconds = seconds, Dismantled = actions.Any(x => x.Kind == "Dismantle") };
        });
        return trajectories.GroupBy(x => new { Source = x.Offer.Context, Slot = x.Offer.Equipment!.EquipmentType.ToString(),
            x.Offer.Equipment.State.Tier, StatVersion = x.Offer.Equipment.StatVersion }).Select(g =>
        {
            var delays = g.Where(x => x.FirstHours.HasValue).Select(x => x.FirstHours!.Value).Order().ToArray();
            return new ItemizationOpportunity(g.Key.Source, g.Key.Slot, g.Key.Tier, g.Key.StatVersion, g.Count(),
                g.Select(x => x.Offer.CharacterId).Distinct().Count(), delays.Length, g.Count(x => x.Seconds >= 60),
                g.Count(x => x.Dismantled), delays.Length == 0 ? null : delays[(delays.Length - 1) / 2], g.Sum(x => x.Seconds) / 3600d)
            {
                AwardsWithCompleteChoices = g.Count(x => x.Offer.Choices is { Complete: true }),
                EligibleAwards = g.Count(x => x.Offer.Choices is { Complete: true, CandidateEligible: true }),
                EligibleAwardsWithAlternatives = g.Count(x => x.Offer.Choices is { Complete: true, CandidateEligible: true, EligibleAlternatives: > 0 }),
                EligibleAwardsEquipped = g.Count(x => x.FirstHours.HasValue && x.Offer.Choices is { Complete: true, CandidateEligible: true }),
                EligibleAwardsWithAlternativesEquipped = g.Count(x => x.FirstHours.HasValue && x.Offer.Choices is { Complete: true, CandidateEligible: true, EligibleAlternatives: > 0 })
            };
        }).OrderBy(x => x.Source).ThenBy(x => x.Slot).ThenBy(x => x.Tier).ToArray();
    }
}
