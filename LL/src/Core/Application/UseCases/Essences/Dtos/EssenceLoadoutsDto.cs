namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceLoadoutsDto(IReadOnlyList<EssenceLoadoutDto> Loadouts, int Limit, int UnlockedSlots);
