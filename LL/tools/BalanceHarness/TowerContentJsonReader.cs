using System.Text.Json;
using Services.LL.Content;

namespace BalanceHarness;

/// <summary>Opt-in accounting supplied only by the offline composition root.</summary>
internal sealed class TowerContentJsonReader : ContentJsonReader
{
    public static TowerContentJsonReader Instance { get; } = new();
    private TowerContentJsonReader() { }
    public override string ReadAllText(string path) => TowerWorkAccounting.Enabled
        ? TowerWorkAccounting.ReadAllText(path) : base.ReadAllText(path);
    public override Stream OpenRead(string path) => TowerWorkAccounting.ReadStream(base.OpenRead(path), path);
    public override T? Deserialize<T>(string json, JsonSerializerOptions options) where T : default
        => TowerWorkAccounting.Parse<T>(json, options);
    public override T? Deserialize<T>(Stream stream, JsonSerializerOptions options) where T : default
        => TowerWorkAccounting.Parse<T>(stream, options);
    public override JsonDocument ParseDocument(string json) => TowerWorkAccounting.ParseDocument(json);
}
