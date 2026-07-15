using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonSigilAssemblySettingsProvider : IDungeonSigilAssemblySettingsProvider
{
    private readonly DungeonSigilAssemblySettings _settings;

    public JsonDungeonSigilAssemblySettingsProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeons", "sigil-assembly.json");
        _settings = JsonSerializer.Deserialize<DungeonSigilAssemblySettings>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Dungeon sigil assembly settings could not be loaded.");

        if (_settings.FragmentCost <= 0)
        {
            throw new InvalidOperationException("Dungeon sigil assembly fragment cost must be positive.");
        }
    }

    public DungeonSigilAssemblySettings GetSettings() => _settings;
}
