using System.Text.Json;

namespace Services.LL.JsonDefinitions.Reader;

public sealed class JsonDocumentReader<T> where T : class
{
    public T Value { get; }

    public JsonDocumentReader(string basePath, string relativePath, JsonSerializerOptions options)
    {
        var filePath = Path.Combine(basePath, relativePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Definition file not found.", filePath);

        var json = File.ReadAllText(filePath);
        Value = JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidOperationException($"Definition document '{filePath}' is empty.");
    }
}
