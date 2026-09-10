using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds.Buildings;

namespace Application.UseCases.Guilds.Dtos.Responses;

public sealed class GuildPublicBuildingDto : IMapFrom<GuildBuilding>
{
    public GuildBuildingType Type { get; set; }
    public int Level { get; set; }

    public void Mapping(Profile profile) => profile.CreateMap<GuildBuilding, GuildPublicBuildingDto>();
}
