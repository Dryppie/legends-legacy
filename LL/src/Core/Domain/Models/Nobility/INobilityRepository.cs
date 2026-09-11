using Domain.Models.Entities.Characters;

namespace Domain.Models.Nobility;

public interface INobilityRepository
{
    Task<Character?> GetCharacterAsync(Guid characterId, CancellationToken ct);
    Task LockAccountAsync(Guid accountId, Guid characterId, CancellationToken ct);
    Task<NobilityMembership?> GetMembershipAsync(Guid accountId, CancellationToken ct);
    Task<IReadOnlyList<NobilityCoverage>> GetCoverageAsync(Guid characterId, CancellationToken ct);
    Task<IReadOnlyList<SignetUnit>> GetUnitsAsync(Guid characterId, SignetState state, CancellationToken ct);
    Task<SignetIssuance?> GetIssuanceAsync(Guid operationId, CancellationToken ct);
    Task<SignetRedemption?> GetRedemptionAsync(Guid operationId, CancellationToken ct);
    Task<bool> HasCashSupportAsync(Guid accountId, CancellationToken ct);
    void AddMembership(NobilityMembership membership);
    void AddIssuance(SignetIssuance issuance, IReadOnlyCollection<SignetUnit> units);
    void AddMovement(SignetMovement movement);
    void AddRedemption(SignetRedemption redemption);
    Task SynchronizeInventoryAsync(Guid characterId, CancellationToken ct);
    Task<bool> HasDailyGrantAsync(Guid accountId, DateOnly date, CancellationToken ct);
    Task ApplyDailyGrantAsync(NobilityDailyGrant grant, CancellationToken ct);
    Task<IReadOnlyList<(Guid AccountId, Guid CharacterId)>> GetDueAccountsAsync(DateOnly before, int limit, CancellationToken ct);
}
