namespace Application.UseCases.Essences.Dtos;

public sealed record SaveEssenceLoadoutDto(Guid? Id, string Name, IReadOnlyList<SaveEssenceLoadoutSlotDto> Slots);
