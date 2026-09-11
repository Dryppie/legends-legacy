namespace Application.UseCases.Nobility.Dtos;

public sealed record SignetPreviewDto(Guid MembershipVersion, Guid[] UnitIds, DateTimeOffset ExpiresAt, bool ExpiryIsEstimate);
