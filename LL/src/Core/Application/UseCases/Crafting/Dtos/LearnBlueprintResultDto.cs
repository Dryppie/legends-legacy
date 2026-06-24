using Application.Common.Mappings;
using AutoMapper;

namespace Application.UseCases.Crafting.Dtos;

public sealed record LearnBlueprintResult(string BlueprintId, string UnlockedRecipeId, string UnlockedRecipeName);

public sealed class LearnBlueprintResultDto : IMapFrom<LearnBlueprintResult>
{
    public string BlueprintId { get; init; } = string.Empty;
    public string UnlockedRecipeId { get; init; } = string.Empty;
    public string UnlockedRecipeName { get; init; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<LearnBlueprintResult, LearnBlueprintResultDto>();
    }
}
