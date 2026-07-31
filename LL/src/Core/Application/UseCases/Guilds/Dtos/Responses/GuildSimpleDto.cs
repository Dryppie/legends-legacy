using Application.Common.Mappings;
using AutoMapper;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;
public class GuildSimpleDto : IMapFrom<Guild>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int MaxMembers { get; set; } = 10;
    public int MemberCount { get; set; }
    public int Upgrades { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Guild, GuildSimpleDto>()
            .ForMember(dto => dto.OwnerName, opt => opt.MapFrom(src => src.Owner.Name))
            .ForMember(dto => dto.MaxMembers, opt => opt.MapFrom(src => src.EffectiveMaxMembers()))
            .ForMember(dto => dto.MemberCount, opt => opt.MapFrom(src => src.Members.Count()))
            .ForMember(dto => dto.Upgrades, opt => opt.MapFrom(src => src.Buildings.Sum(b => b.Level)));
    }
}
