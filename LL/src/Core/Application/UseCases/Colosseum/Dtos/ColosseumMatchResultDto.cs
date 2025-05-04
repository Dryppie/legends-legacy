using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ColosseumMatchResultDto : IMapFrom<ColosseumMatchResult>
{

    public Guid CharacterAId { get; set; }
    public string CharacterAName { get; set; } = string.Empty;
    public Guid CharacterBId { get; set; }
    public string CharacterBName { get; set; } = string.Empty;
    public Guid? WinnerId { get; set; } // null = draw
    public string WinnerName { get; set; } = string.Empty; // empty = draw
    public DateTimeOffset PlayedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ColosseumMatchResult, ColosseumMatchResultDto>();
    }
}