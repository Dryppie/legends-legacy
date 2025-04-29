using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos;
public class GuildMemberDto : IMapFrom<GuildMember>
{
    public string Name { get; set; } = string.Empty;
    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildMember, GuildMemberDto>()
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Character.Name));
    }
}