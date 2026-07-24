using Application.Common.Mappings;
using AutoMapper;

namespace Application.UseCases.Crafting.Dtos;

public sealed record LearnBlueprintResult(
    string BlueprintId,
    string BlueprintName,
    string RecipeId,
    string RecipeName);

public sealed class LearnBlueprintResultDto : IMapFrom<LearnBlueprintResult>
{
    public string BlueprintId { get; init; } = string.Empty;
    public string BlueprintName { get; init; } = string.Empty;
    public string RecipeId { get; init; } = string.Empty;
    public string RecipeName { get; init; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<LearnBlueprintResult, LearnBlueprintResultDto>();
    }
}
