using System.Text.Json;

namespace Domain.Models.Prophecies;

public sealed class ProphecyObjectiveParameters
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public int? MinimumEnemyCount { get; set; }

    public static bool TryParse(string? json, out ProphecyObjectiveParameters parameters)
    {
        try
        {
            parameters = JsonSerializer.Deserialize<ProphecyObjectiveParameters>(
                string.IsNullOrWhiteSpace(json) ? "{}" : json,
                JsonOptions) ?? new ProphecyObjectiveParameters();
            return true;
        }
        catch (JsonException)
        {
            parameters = new ProphecyObjectiveParameters();
            return false;
        }
    }
}
