using Application.Common.Mappings;
using AutoMapper;

namespace Application.UseCases.Crafting.Dtos;

public sealed record LearnBlueprintResult(
    string BlueprintId,
    string BlueprintName,
    int CompatibleRecipeCount);

public sealed class LearnBlueprintResultDto : IMapFrom<LearnBlueprintResult>
{
    public string BlueprintId { get; init; } = string.Empty;
    public string BlueprintName { get; init; } = string.Empty;
    public int CompatibleRecipeCount { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<LearnBlueprintResult, LearnBlueprintResultDto>();
    }
}
