using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ColosseumArenaRankDto : IMapFrom<ColosseumArenaRank>
{
    public int Rank { get; set; }
    public string Name { get; set; } = null!;
    public int Rating { get; set; }
    public Guid SeasonId { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ColosseumArenaRank, ColosseumArenaRankDto>()
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Character.Name));
    }
}