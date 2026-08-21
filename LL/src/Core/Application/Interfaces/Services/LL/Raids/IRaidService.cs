using Application.UseCases.Raids.Dtos;
using Domain.Models.Raids;

namespace Application.Interfaces.Services.LL.Raids;

public interface IRaidBossDefinitionProvider
{
    IReadOnlyList<RaidBossDefinition> GetAll();
    RaidBossDefinition? Get(string raidBossId);
}

public interface IRaidTrophyVendorCatalog
{
    IReadOnlyList<RaidTrophyVendorItemDefinition> GetForBoss(string raidBossId);
    RaidTrophyVendorItemDefinition? Get(string itemId);
}

public interface IRaidService
{
    Task<IReadOnlyList<RaidBossSummaryDto>> GetRaidBossesAsync(Guid characterId, int? region, CancellationToken cancellationToken);
    Task<IReadOnlyList<RaidRunSummaryDto>> GetOpenRaidsAsync(Guid characterId, string raidBossId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RaidHistoryEntryDto>> GetHistoryAsync(Guid characterId, string? raidBossId, int take, CancellationToken cancellationToken);
    Task<RaidRunDto?> GetRaidAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidRunDto?> GetActiveRaidAsync(Guid characterId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> CreateAsync(Guid characterId, string raidBossId, int tier, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> CreateDevelopmentAsync(Guid characterId, string raidBossId, int tier, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> FillDevelopmentTeamAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> JoinAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> ApproveSignupAsync(Guid characterId, Guid raidRunId, Guid targetCharacterId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> RemoveSignupAsync(Guid characterId, Guid raidRunId, Guid targetCharacterId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> LeaveAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> CancelAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> TransferLeadershipAsync(Guid characterId, Guid raidRunId, Guid targetCharacterId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> RefreshSnapshotAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> AssignAsync(Guid characterId, Guid raidRunId, Guid targetCharacterId, RaidLane lane, int slotIndex, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> UpdatePartiesAsync(Guid characterId, Guid raidRunId, IReadOnlyList<RaidPartyAssignment> assignments, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidBattlePlanPreviewDto>> PreviewBattlePlanAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRunDto>> CommenceAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidPlaybackDto?> GetPlaybackAsync(Guid characterId, Guid raidRunId, RaidLane lane, CancellationToken cancellationToken);
    Task<RaidPlaybackBundleContentDto?> GetPlaybackBundleAsync(Guid characterId, Guid raidRunId, RaidLane lane, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidRewardDto>> ClaimAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken);
    Task<RaidTrophyVendorDto?> GetTrophyVendorAsync(Guid characterId, string raidBossId, CancellationToken cancellationToken);
    Task<RaidOperationResult<RaidTrophyPurchaseDto>> PurchaseTrophyVendorItemAsync(Guid characterId, string raidBossId, string itemId, int quantity, CancellationToken cancellationToken);
    Task ProcessDueRaidsAsync(string workerId, int batchSize, CancellationToken cancellationToken);
}

public sealed record RaidOperationResult<T>(T? Value, string? Error)
{
    public bool Succeeded => Error is null;
    public static RaidOperationResult<T> Success(T value) => new(value, null);
    public static RaidOperationResult<T> Fail(string error) => new(default, error);
}
