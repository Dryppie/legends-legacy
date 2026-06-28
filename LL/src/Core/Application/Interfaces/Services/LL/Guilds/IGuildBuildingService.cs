using Domain.Models.Guilds.Buildings;

namespace Application.Interfaces.Services.LL.Guilds;

public interface IGuildBuildingService
{
    Task<GuildBuildingOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<GuildOperationResult<GuildBuildingOverviewDto>> ConstructAsync(
        Guid characterId,
        GuildBuildingType buildingType,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<GuildOperationResult<GuildBuildingOverviewDto>> UpgradeAsync(
        Guid characterId,
        Guid buildingId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
