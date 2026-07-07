namespace Application.Interfaces.Services.LL.Tutorials;

public interface ITutorialDefinitionProvider
{
    TutorialDefinition Get(string tutorialId);
    TutorialStepDefinition? GetStep(string tutorialId, string stepKey);
}

public sealed class TutorialDefinition
{
    public string TutorialId { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string InitialStepKey { get; set; } = string.Empty;
    public List<TutorialStepDefinition> Steps { get; set; } = [];
}

public sealed class TutorialStepDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public int RequiredAmount { get; set; } = 1;
    public string ActionLabel { get; set; } = string.Empty;
    public string DestinationRoute { get; set; } = string.Empty;
    public string? TourPageId { get; set; }
    public string? GuidePageId { get; set; }
    public string? NextStepKey { get; set; }
    public TutorialStepTriggerDefinition? Trigger { get; set; }
}

public sealed class TutorialStepTriggerDefinition
{
    public string Type { get; set; } = string.Empty;
    public string? AreaId { get; set; }
    public bool? RequiresVictory { get; set; }
    public string? EssenceDefinitionId { get; set; }
    public string? Route { get; set; }
    public int? RequiredCount { get; set; }
    public List<string> ItemBaseIds { get; set; } = [];
}
