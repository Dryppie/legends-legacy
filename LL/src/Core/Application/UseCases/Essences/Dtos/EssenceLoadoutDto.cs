using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceLoadoutDto(
    Guid Id,
    string Name,
    IReadOnlyList<EssenceCombatActivity> AutoUseActivities,
    IReadOnlyList<EssenceLoadoutSlotDto> Slots);
