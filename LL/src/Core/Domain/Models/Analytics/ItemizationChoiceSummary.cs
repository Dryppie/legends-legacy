namespace Domain.Models.Analytics;

public sealed record ItemizationStatChoice(string Attribute, int AvailableDecisions, int SelectedDecisions,
    int DecisionsWithStatFreeAlternative);

public sealed record ItemizationChoiceSummary(string Context, string Kind, string Slot, int Tier, int RulesVersion,
    int Decisions, int MissingOrIncomplete, int EligibleDecisions, int DecisionsWithAlternatives,
    int DistinctCharacters, IReadOnlyList<ItemizationStatChoice> Attributes)
{
    public string Interpretation => "Availability includes the candidate and eligible compatible replacements. Selected means equipped for equip events and inspected for comparisons. Counts describe choices, not causal preference; tiers, quality and other item stats may differ.";

    public static IReadOnlyList<ItemizationChoiceSummary> Create(IEnumerable<ItemizationObservation> observations) =>
        observations.DistinctBy(x => x.Id).Where(x => x.Kind is "equipped" or "compared")
            .GroupBy(x => new { x.Context, x.Kind, Slot = x.Comparison?.Slot ?? x.Equipment?.EquipmentType.ToString() ?? "unknown",
                Tier = x.Equipment?.State.Tier ?? x.Choices?.Candidate?.Tier ?? 0, x.RulesVersion })
            .Select(group =>
            {
                var complete = group.Where(x => x.Choices is { Complete: true, CandidateEligible: true, Candidate: not null }).ToArray();
                var stats = complete.SelectMany(x => x.Choices!.Alternatives.Where(a => a.Eligible)
                        .Append(x.Choices.Candidate!).SelectMany(a => a.Stats.Where(s => s.Value > 0).Select(s => s.Key)))
                    .Distinct().Order().Select(stat => new ItemizationStatChoice(stat.ToString(),
                        complete.Count(x => x.Choices!.Candidate!.Stats.GetValueOrDefault(stat) > 0
                            || x.Choices.Alternatives.Any(a => a.Eligible && a.Stats.GetValueOrDefault(stat) > 0)),
                        complete.Count(x => x.Choices!.Candidate!.Stats.GetValueOrDefault(stat) > 0),
                        complete.Count(x => x.Choices!.Candidate!.Stats.GetValueOrDefault(stat) > 0
                            && x.Choices.Alternatives.Any(a => a.Eligible && a.Stats.GetValueOrDefault(stat) == 0)))).ToArray();
                return new ItemizationChoiceSummary(group.Key.Context, group.Key.Kind, group.Key.Slot, group.Key.Tier,
                    group.Key.RulesVersion, group.Count(), group.Count(x => x.Choices is not { Complete: true, Candidate: not null }),
                    complete.Length, complete.Count(x => x.Choices!.EligibleAlternatives > 0),
                    complete.Select(x => x.CharacterId).Distinct().Count(), stats);
            }).OrderBy(x => x.Context).ThenBy(x => x.Kind).ThenBy(x => x.Slot).ThenBy(x => x.Tier).ToArray();
}
