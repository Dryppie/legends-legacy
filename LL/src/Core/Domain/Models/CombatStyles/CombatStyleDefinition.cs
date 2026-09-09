namespace Domain.Models.CombatStyles;

public sealed record CombatStyleDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public CombatStyleKind Kind { get; init; }
    public CombatStyleTuning Tuning { get; init; } = new();
    public CombatStyleOpeningTechnique? OpeningTechnique { get; init; }
    public CombatStyleMilestoneTuning MilestoneTuning { get; init; } = new();
    public IReadOnlyList<CombatStyleChoiceDefinition> Refinements { get; init; } = [];
    public IReadOnlyList<CombatStyleChoiceDefinition> Upgrades { get; init; } = [];
}

public sealed record CombatStyleChoiceDefinition(string Id, string Name, string Description, CombatStyleTuning? Tuning = null,
    string MasteryDescription = "");

public sealed record CombatStyleOpeningTechnique(string Name, string Description);

public sealed record CombatStyleCatalog
{
    public string ContentVersion { get; init; } = string.Empty;
    public IReadOnlyList<long> XpRequirements { get; init; } = [];
    public IReadOnlyList<CombatStyleDefinition> Styles { get; init; } = [];
}
