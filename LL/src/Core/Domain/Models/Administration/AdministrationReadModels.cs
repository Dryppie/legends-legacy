using Domain.Models.Items;

namespace Domain.Models.Administration;

public sealed record AdministrationItemCatalogEntry(
    string Id,
    string Name,
    string Description,
    ItemType ItemType,
    Rarity Rarity,
    bool Stackable,
    bool IsBound);

public sealed record AdministrationHistoryEntry(
    Guid OperationId,
    AdminActionType ActionType,
    string Permission,
    string ActorSubject,
    string ActorDisplayName,
    Guid? TargetAccountId,
    Guid? TargetCharacterId,
    Guid? TargetResourceId,
    string Reason,
    string? InternalNotes,
    string DetailsJson,
    AdministrationRiskLevel RiskLevel,
    DateTimeOffset OccurredAt);

public sealed record AdministrationAuditQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    AdminActionType? ActionType,
    string? Actor,
    string? Permission,
    string? Reference,
    bool IncludeInternalNotesInReference,
    AdministrationRiskLevel? RiskLevel,
    Guid? OperationId,
    IReadOnlyCollection<Guid> TargetAccountIds,
    IReadOnlyCollection<Guid> TargetCharacterIds,
    Guid? TargetResourceId,
    DateTimeOffset? BeforeOccurredAt,
    Guid? BeforeOperationId,
    int Limit);
