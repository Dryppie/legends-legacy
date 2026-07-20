using Application.Common.Mappings;
using Domain.Models.Dungeons;
using AutoMapper;

namespace Application.UseCases.Dungeons.Dtos;

public class DungeonActionDto : IMapFrom<DungeonAction>
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Style { get; set; } = "primary";
    public bool Disabled { get; set; }
    public string? Description { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonAction, DungeonActionDto>();
}
