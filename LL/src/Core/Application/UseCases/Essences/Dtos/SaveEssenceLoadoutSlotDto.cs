namespace Application.UseCases.Essences.Dtos;

public sealed record SaveEssenceLoadoutSlotDto(int SlotIndex, Guid? PlayerEssenceId);
