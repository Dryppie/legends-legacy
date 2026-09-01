namespace Domain.Models.Colosseum;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Combat;

public class ColosseumMatchResult
{
    private static readonly JsonSerializerOptions CombatResultJsonOptions = CreateCombatResultJsonOptions();

    public Guid Id { get; set; }

    public Guid CharacterAId { get; set; }
    public string CharacterAName { get; set; } = string.Empty;
    public int CharacterARatingBefore { get; set; }
    public int CharacterARatingAfter { get; set; }

    public Guid CharacterBId { get; set; }
    public string CharacterBName { get; set; } = string.Empty;
    public int CharacterBRatingBefore { get; set; }
    public int CharacterBRatingAfter { get; set; }

    public Guid? WinnerId { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public DateTimeOffset PlayedAt { get; set; }

    public string Outcome { get; set; } = string.Empty;
    public int CharacterARatingDelta { get; set; }
    public int CharacterBRatingDelta { get; set; }
    public int CharacterAGloryEarned { get; set; }
    public int CharacterBGloryEarned { get; set; }
    public int CharacterAStreakBefore { get; set; }
    public int CharacterAStreakAfter { get; set; }
    public string? CombatResultJson { get; private set; }

    [NotMapped]
    public CombatResult? CombatResult => string.IsNullOrWhiteSpace(CombatResultJson)
        ? null
        : JsonSerializer.Deserialize<CombatResult>(CombatResultJson, CombatResultJsonOptions);

    public void SetCombatResult(CombatResult combatResult)
    {
        CombatResultJson = JsonSerializer.Serialize(combatResult, CombatResultJsonOptions);
    }

    private static JsonSerializerOptions CreateCombatResultJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
