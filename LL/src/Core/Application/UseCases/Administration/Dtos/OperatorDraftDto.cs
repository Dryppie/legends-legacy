namespace Application.UseCases.Administration.Dtos;

public sealed record OperatorDraftDto(string Key, Guid Version, string Content, DateTimeOffset UpdatedAt);
