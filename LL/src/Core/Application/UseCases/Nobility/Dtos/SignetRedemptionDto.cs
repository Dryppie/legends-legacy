namespace Application.UseCases.Nobility.Dtos;

public sealed record SignetRedemptionDto(Guid Id, Guid AccountId, Guid CharacterId, Guid[] UnitIds,
    Guid MembershipVersion, DateTimeOffset RedeemedAt, DateTimeOffset? PreviousExpiry, DateTimeOffset ExpiresAt);
