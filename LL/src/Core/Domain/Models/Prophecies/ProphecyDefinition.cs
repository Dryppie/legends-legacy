namespace Domain.Models.Prophecies;

public sealed class ProphecyDefinition
{
    public string Id { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string FlavorText { get; set; } = default!;
    public string ObjectiveText { get; set; } = default!;

    public ProphecyScope Scope { get; set; }
    public ProphecyCategory Category { get; set; }
    public ProphecyDifficulty Difficulty { get; set; }

    public string ObjectiveType { get; set; } = default!;
    public string ObjectiveParameterJson { get; set; } = "{}";

    public string RewardProfileId { get; set; } = default!;

    public int Weight { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;

    public List<string> AllowedSlots { get; set; } = [];
    public List<string> RequiredFeatures { get; set; } = [];
    public List<string> RequiredTags { get; set; } = [];
    public List<string> ExcludedTags { get; set; } = [];

    public int MinPlayerLevel { get; set; } = 1;
    public int? MaxPlayerLevel { get; set; }
}
