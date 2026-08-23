using Application.UseCases.Inventories.Dtos;
using Domain.Models.CharacterActions;

namespace Application.UseCases.Professions.Dtos;

public sealed class TemperingQueueMutationResponseDto
{
    public IReadOnlyList<Guid> RemovedInventoryItemIds { get; init; } = [];
    public IReadOnlyList<InventoryItemDto> ReturnedInventoryItems { get; init; } = [];
    public IReadOnlyList<Guid> RemovedQueueItemIds { get; init; } = [];
    public Guid? AddedQueueItemId { get; init; }
    public TemperingActionStateDto? Action { get; init; }
}

public sealed class TemperingActionStateDto
{
    public CharacterActionType CharacterActionType { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? NextResolutionAtUtc { get; init; }
    public DateTimeOffset? BlockedUntilUtc { get; init; }
    public long ScheduleGeneration { get; init; }
    public bool IsDeleted { get; init; }
    public int? ResolutionIntervalMs { get; init; }

    public string Revision => string.Join(':',
        ScheduleGeneration,
        NextResolutionAtUtc?.UtcDateTime.Ticks ?? UpdatedAt.UtcDateTime.Ticks,
        NextResolutionAtUtc?.UtcDateTime.Ticks ?? 0,
        BlockedUntilUtc?.UtcDateTime.Ticks ?? 0,
        UpdatedAt.UtcDateTime.Ticks,
        IsDeleted);

    public static TemperingActionStateDto? From(CharacterAction? action) =>
        action is null
            ? null
            : new TemperingActionStateDto
            {
                CharacterActionType = action.CharacterActionType,
                UpdatedAt = action.UpdatedAt,
                NextResolutionAtUtc = action.NextResolutionAtUtc,
                BlockedUntilUtc = action.BlockedUntilUtc,
                ScheduleGeneration = action.ScheduleGeneration,
                IsDeleted = action.IsDeleted,
                ResolutionIntervalMs = action.ResolutionIntervalMs
            };
}
