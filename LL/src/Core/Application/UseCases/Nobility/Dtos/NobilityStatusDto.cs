using Domain.Models.Nobility;

namespace Application.UseCases.Nobility.Dtos;

public sealed record NobilityStatusDto(bool IsNoble, DateTimeOffset ServerTime, DateTimeOffset? ExpiresAt,
    Guid MembershipVersion, int AvailableSignets, int ListedSignets, bool HasSupportHistory,
    bool ShowBadge, DateOnly? DailyRewardsThrough, NobilityBenefits Benefits);
