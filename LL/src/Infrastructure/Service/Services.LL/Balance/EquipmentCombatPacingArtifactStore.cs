using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Balance;

namespace Services.LL.Balance;

public sealed class EquipmentCombatPacingArtifactStore : IEquipmentCombatPacingArtifactStore
{
    private readonly string _artifactDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public EquipmentCombatPacingArtifactStore(string contentRootPath)
    {
        _artifactDirectory = Path.Combine(contentRootPath, "balance-artifacts");
    }

    public async Task WriteAsync(
        EquipmentCombatPacingReport report,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_artifactDirectory);
        var fileName = $"equipment-combat-pacing-v{report.EquipmentBalanceVersion}-" +
            $"{report.ExecutionLevel.ToString().ToLowerInvariant()}.json";
        var path = Path.Combine(_artifactDirectory, fileName);
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        await JsonSerializer.SerializeAsync(stream, report, _jsonOptions, cancellationToken);
    }
}
