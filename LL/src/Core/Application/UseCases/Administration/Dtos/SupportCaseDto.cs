using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record SupportCaseDto(Guid Id, Guid AccountId, Guid CharacterId, string CharacterName, string Title, string Category,
    string? ExternalReference, SupportCaseStatus Status, string? Resolution, int Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, SupportCasePriority Priority, DateTimeOffset? FollowUpAt, string? NextAction);
