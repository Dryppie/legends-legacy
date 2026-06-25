using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Professions.Crafting;

namespace Application.UseCases.Crafting.Dtos;

public sealed class RecipeMasteryDto : IMapFrom<CharacterRecipeMastery>
{
    public string RecipeId { get; init; } = string.Empty;
    public int Level { get; init; }
    public int Experience { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterRecipeMastery, RecipeMasteryDto>();
    }
}
