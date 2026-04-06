using System.Text.Json;

namespace Services.LL.JsonDefinitions.Reader;

public sealed class JsonDefinitionReader<T>
{
    public IReadOnlyList<T> All { get; }

    public JsonDefinitionReader(string basePath, string relativePath, JsonSerializerOptions options)
    {
        var filePath = Path.Combine(basePath, relativePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Definition file not found.", filePath);

        var json = File.ReadAllText(filePath);
        All = JsonSerializer.Deserialize<List<T>>(json, options) ?? [];
    }
}
