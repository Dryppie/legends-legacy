namespace Domain.Models.CombatStyles;

public sealed class CombatStyleRuntimeState
{
    public string StyleId { get; init; } = string.Empty;
    public int StyleLevel { get; init; }
    public string? FocusId { get; init; }
    public bool AppliesToFriendlyTeam { get; init; } = true;
    public Dictionary<string, decimal> Resources { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> NodeRanks { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> TriggerCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<PendingStyleEmpowerment> PendingEmpowerments { get; } = [];
}

public sealed record PendingStyleEmpowerment(
    string Id,
    EffectPredicate AppliesTo,
    decimal AdditivePercent,
    bool ConsumeOnUse);

public sealed record CombatStyleSnapshot(
    string StyleId,
    string StyleName,
    int Level,
    long Experience,
    string? SelectedFocusId,
    string? SelectedFocusName,
    IReadOnlyDictionary<string, int>? NodeRanks = null);
