using Application.Common.Mappings;
using AutoMapper;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;

public sealed class GuildPublicDto : IMapFrom<Guild>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMembers { get; set; }
    public List<GuildPublicMemberDto> Members { get; set; } = [];
    public List<GuildPublicBuildingDto> Buildings { get; set; } = [];

    public void Mapping(Profile profile) => profile.CreateMap<Guild, GuildPublicDto>()
        .ForMember(dto => dto.MaxMembers, opt => opt.MapFrom(src => src.EffectiveMaxMembers()));
}
