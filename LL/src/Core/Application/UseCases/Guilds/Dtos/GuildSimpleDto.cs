using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos;
public class GuildSimpleDto : IMapFrom<Guild>
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 10;
    public int MemberCount { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Guild, GuildSimpleDto>()
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Owner.Name))
            .ForMember(dto => dto.MemberCount, opt => opt.MapFrom(src => src.Members.Count()));
    }
}