using System.Text.Json;

namespace Services.LL.Content;

/// <summary>
/// File and JSON boundaries for content providers. The default retains the file
/// encoding, serializer options and stream behavior chosen by each provider.
/// Offline callers can override these operations without changing validation.
/// </summary>
public class ContentJsonReader
{
    public static ContentJsonReader Default { get; } = new();
    public virtual string ReadAllText(string path) => File.ReadAllText(path);
    public virtual Stream OpenRead(string path) => File.OpenRead(path);
    public virtual T? Deserialize<T>(string json, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<T>(json, options);
    public virtual T? Deserialize<T>(Stream stream, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<T>(stream, options);
    public virtual JsonDocument ParseDocument(string json) => JsonDocument.Parse(json);
}
