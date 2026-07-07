namespace Application.UseCases.Tutorials.Commands.CompleteClientTutorialStep;

public sealed record CompleteClientTutorialStepRequest(
    string StepKey,
    string TriggerType,
    string? Route = null);
