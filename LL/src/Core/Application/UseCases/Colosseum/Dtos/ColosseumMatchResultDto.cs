using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ColosseumMatchResultDto : IMapFrom<ColosseumMatchResult>
{
    public string CharacterAName { get; set; } = string.Empty;
    public string CharacterBName { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty; // empty = draw
    public DateTimeOffset PlayedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ColosseumMatchResult, ColosseumMatchResultDto>();
    }
}