using Application.Interfaces.Services.LL.Tutorials;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Tutorials;

public sealed class JsonTutorialDefinitionProvider : ITutorialDefinitionProvider
{
    private readonly IReadOnlyDictionary<string, TutorialDefinition> _definitions;

    public JsonTutorialDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "tutorials");

        var definitions = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json")
                .Select(filePath => ReadDefinition(filePath, options))
                .ToList()
            : [];

        ThrowIfInvalid(definitions);
        _definitions = definitions.ToDictionary(x => x.TutorialId, StringComparer.OrdinalIgnoreCase);
    }

    public TutorialDefinition Get(string tutorialId)
    {
        if (_definitions.TryGetValue(tutorialId, out var definition))
        {
            return definition;
        }

        throw new InvalidOperationException($"Unknown tutorial definition '{tutorialId}'.");
    }

    public TutorialStepDefinition? GetStep(string tutorialId, string stepKey) =>
        Get(tutorialId).Steps.FirstOrDefault(step =>
            step.Key.Equals(stepKey, StringComparison.OrdinalIgnoreCase));

    private static TutorialDefinition ReadDefinition(string path, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<TutorialDefinition>(File.ReadAllText(path), options)
        ?? throw new InvalidOperationException($"Tutorial definition file '{path}' was empty.");

    private static void ThrowIfInvalid(IReadOnlyList<TutorialDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("At least one tutorial definition is required.");
        }

        var duplicateTutorials = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.TutorialId))
            .GroupBy(x => x.TutorialId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateTutorials.Count > 0)
        {
            throw new InvalidOperationException("Duplicate tutorial definitions: " + string.Join(", ", duplicateTutorials));
        }

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.TutorialId) ||
                string.IsNullOrWhiteSpace(definition.Title) ||
                string.IsNullOrWhiteSpace(definition.InitialStepKey))
            {
                throw new InvalidOperationException("Tutorial definitions require tutorialId, title, and initialStepKey.");
            }

            var duplicateSteps = definition.Steps
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateSteps.Count > 0)
            {
                throw new InvalidOperationException($"Tutorial '{definition.TutorialId}' has duplicate steps: {string.Join(", ", duplicateSteps)}");
            }

            if (!definition.Steps.Any(step => step.Key.Equals(definition.InitialStepKey, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Tutorial '{definition.TutorialId}' initial step '{definition.InitialStepKey}' does not exist.");
            }

            foreach (var step in definition.Steps)
            {
                if (string.IsNullOrWhiteSpace(step.Key))
                {
                    throw new InvalidOperationException($"Tutorial '{definition.TutorialId}' contains a step without a key.");
                }

                if (step.Trigger is null)
                {
                    throw new InvalidOperationException($"Tutorial '{definition.TutorialId}' step '{step.Key}' requires a trigger.");
                }

                if (string.IsNullOrWhiteSpace(step.Trigger.Type))
                {
                    throw new InvalidOperationException($"Tutorial '{definition.TutorialId}' step '{step.Key}' trigger requires a type.");
                }

                if (!string.IsNullOrWhiteSpace(step.NextStepKey) &&
                    !step.NextStepKey.Equals("complete", StringComparison.OrdinalIgnoreCase) &&
                    !definition.Steps.Any(candidate => candidate.Key.Equals(step.NextStepKey, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Tutorial '{definition.TutorialId}' step '{step.Key}' points to missing next step '{step.NextStepKey}'.");
                }
            }
        }
    }
}
