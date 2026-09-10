using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;

public sealed class GuildPublicMemberDto : IMapFrom<GuildMember>
{
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public GuildRole Role { get; set; }

    public void Mapping(Profile profile) => profile.CreateMap<GuildMember, GuildPublicMemberDto>()
        .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Character.Name))
        .ForMember(dto => dto.Level, opt => opt.MapFrom(src => src.Character.Level));
}
