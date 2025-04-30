using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos;
public class GuildInviteDto : IMapFrom<GuildInvite>
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public Guid GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;

    /// <summary>
    /// If true, it's an invite that has been sent to a player by the guild
    /// If false, it's an application that has been sent to the guild by the player
    /// </summary>
    public bool IsInvite { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildInvite, GuildInviteDto>()
            .ForMember(dto => dto.CharacterName, opt => opt.MapFrom(src => src.Character.Name))
            .ForMember(dto => dto.GuildName, opt => opt.MapFrom(src => src.Guild.Name));
    }
}