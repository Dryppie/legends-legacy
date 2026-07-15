using Domain.Models.Dungeons.Definitions;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonSigilAssemblySettingsProvider
{
    DungeonSigilAssemblySettings GetSettings();
}
