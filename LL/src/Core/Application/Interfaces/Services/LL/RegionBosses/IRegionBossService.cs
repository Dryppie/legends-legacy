using Application.UseCases.RegionBosses.Dtos;
using Domain.Models.RegionBosses;

namespace Application.Interfaces.Services.LL.RegionBosses;

public interface IRegionBossDefinitionProvider
{
    IReadOnlyList<RegionBossDefinition> GetAll();
    RegionBossDefinition? Get(string definitionId);
}

public interface IRegionBossService
{
    Task<IReadOnlyList<RegionBossStatusDto>> GetStatusAsync(Guid characterId, int? regionId, CancellationToken cancellationToken);
    Task<RegionBossStatusDto?> GetEventAsync(Guid characterId, Guid eventId, CancellationToken cancellationToken);
    Task<RegionBossOperationResult<RegionBossStatusDto>> SignupAsync(Guid characterId, Guid eventId, CancellationToken cancellationToken);
    Task<RegionBossOperationResult<RegionBossStatusDto>> WithdrawAsync(Guid characterId, Guid eventId, CancellationToken cancellationToken);
    Task<RegionBossOperationResult<RegionBossClaimResultDto>> ClaimAsync(Guid characterId, Guid grantId, CancellationToken cancellationToken);
    Task<RegionBossOperationResult<RegionBossStatusDto>> SpawnDevelopmentEventAsync(
        Guid characterId,
        int regionId,
        int additionalSignupCount,
        CancellationToken cancellationToken);
    Task<RegionBossPlaybackDto?> GetPlaybackAsync(Guid characterId, Guid runId, CancellationToken cancellationToken);
    Task<RegionBossPlaybackBundleContentDto?> GetPlaybackBundleAsync(Guid characterId, Guid runId, CancellationToken cancellationToken);
    Task EnsureScheduledEventsAsync(CancellationToken cancellationToken);
    Task ProgressEventsAsync(string workerId, CancellationToken cancellationToken);
}
