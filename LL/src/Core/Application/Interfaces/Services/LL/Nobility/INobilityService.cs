using Common.Primitives;
using Domain.Models.Nobility;

namespace Application.Interfaces.Services.LL.Nobility;

public interface INobilityService
{
    DateTimeOffset? CombatEvaluationTime { get; }
    IDisposable EvaluateCombatAt(DateTimeOffset at);
    Task<NobilityStatus> GetStatusAsync(Guid accountId, Guid characterId, CancellationToken ct);
    Task<NobilityBenefits> GetBenefitsAsync(Guid characterId, DateTimeOffset at, CancellationToken ct);
    Task<IReadOnlyList<NobilityCoverage>> GetCoverageAsync(Guid characterId, CancellationToken ct);
    Task<Response<SignetPreview>> PreviewAsync(Guid accountId, Guid characterId, int quantity, CancellationToken ct);
    Task<Response<SignetRedemption>> RedeemAsync(Guid accountId, Guid characterId, Guid operationId,
        Guid membershipVersion, Guid[] unitIds, DateOnly expectedExpiryDate, CancellationToken ct);
    Task<Response<SignetIssuance>> GrantAlphaAsync(string actorSubject, Guid characterId, Guid operationId,
        int quantity, string reason, CancellationToken ct);
    Task<Response<bool>> SetAppearanceAsync(Guid accountId, Guid characterId, bool showBadge, CancellationToken ct);
}

public sealed record NobilityStatus(bool IsNoble, DateTimeOffset ServerTime, DateTimeOffset? ExpiresAt,
    Guid MembershipVersion, int AvailableSignets, int ListedSignets, bool HasSupportHistory,
    bool ShowBadge,
    NobilityBenefits Benefits);

public sealed record SignetPreview(Guid MembershipVersion, Guid[] UnitIds, DateTimeOffset ExpiresAt,
    bool ExpiryIsEstimate);
