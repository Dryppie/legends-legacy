using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;
public class GuildDto : IMapFrom<Guild>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 10;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public List<GuildMemberDto> Members { get; set; } = [];
    public List<GuildInviteDto> Invites { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Guild, GuildDto>()
            .ForMember(dto => dto.OwnerName, opt => opt.MapFrom(src => src.Owner.Name))
            .ForMember(dto => dto.Invites, opt => opt.MapFrom(src => src.Invites.Where(i => !i.IsInvite)));
    }
}