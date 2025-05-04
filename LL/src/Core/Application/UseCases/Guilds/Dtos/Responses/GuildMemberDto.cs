using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;
public class GuildMemberDto : IMapFrom<GuildMember>
{
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildMember, GuildMemberDto>()
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Character.Name))
            .ForMember(dto => dto.Level, opt => opt.MapFrom(src => src.Character.Level));
    }
}