namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceLoadoutSlotDto(int SlotIndex, Guid? PlayerEssenceId, string? EssenceDefinitionId, string? EssenceName);
