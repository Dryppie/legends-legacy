namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceLoadoutDto(Guid Id, string Name, bool IsActive, IReadOnlyList<EssenceLoadoutSlotDto> Slots);
