using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed record SetEssenceLoadoutAutoUseActivitiesRequestDto(
    IReadOnlyList<EssenceCombatActivity> Activities);
