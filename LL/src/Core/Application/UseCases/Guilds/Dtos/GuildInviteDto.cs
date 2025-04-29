using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos;
public class GuildInviteDto : IMapFrom<GuildInvite>
{
    public string Name { get; set; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildInvite, GuildInviteDto>()
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Character.Name));
    }
}