using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Progression;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Levels;

public sealed class JsonCharacterExperienceProgressionProvider : ICharacterExperienceProgressionProvider
{
    private readonly CharacterExperienceCurveSettings _settings;

    public JsonCharacterExperienceProgressionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "progression", "character-experience.json");
        var document = JsonSerializer.Deserialize<CharacterExperienceDocument>(File.ReadAllText(path), options)
                       ?? throw new InvalidOperationException($"Character experience document '{path}' is empty.");

        ValidateSettings(document.CharacterLevelCurve);
        _settings = document.CharacterLevelCurve;
    }

    public long GetRequiredExperience(int level) =>
        CharacterExperienceCurve.CalculateRequiredExperience(level, _settings);

    private static void ValidateSettings(CharacterExperienceCurveSettings settings)
    {
        if (settings.BaseExperience < 0 ||
            settings.LinearExperiencePerLevel < 0 ||
            settings.QuadraticExperiencePerLevelSquared <= 0 ||
            settings.RoundingIncrement <= 0 ||
            (long)settings.LinearExperiencePerLevel +
            3L * settings.QuadraticExperiencePerLevelSquared < settings.RoundingIncrement)
        {
            throw new InvalidOperationException("Character experience curve settings are invalid.");
        }
    }

    private sealed class CharacterExperienceDocument
    {
        public CharacterExperienceCurveSettings CharacterLevelCurve { get; set; } = new();
    }
}
