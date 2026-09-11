namespace Application.UseCases.Nobility.Dtos;

public sealed record SignetGrantDto(Guid Id, Guid AccountId, Guid CharacterId, int Quantity, string Reason, DateTimeOffset IssuedAt);
