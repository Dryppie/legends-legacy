using Common.Options;
using Microsoft.Extensions.Options;

namespace Common.Helpers.JsonFiles;
public sealed class JsonFileResolver
{
    private readonly string _basePath;

    public JsonFileResolver(IOptions<DataFilePathOptions> options)
    {
        var dataPath = options.Value.DataPath;

        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;

        _basePath = Path.Combine(apiDirectory, dataPath, "Data");
    }

    public string Resolve(string fileName) => Path.Combine(_basePath, fileName);
}