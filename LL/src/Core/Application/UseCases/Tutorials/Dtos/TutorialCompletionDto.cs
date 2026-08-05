using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Tutorials;
using AutoMapper;

namespace Application.UseCases.Tutorials.Dtos;

public sealed class TutorialCompletionDto : IMapFrom<TutorialCompletion>
{
    public string TutorialId { get; set; } = string.Empty;
    public int RewardCinders { get; set; }
    public string NextRoute { get; set; } = string.Empty;
    public bool WasSkipped { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TutorialCompletion, TutorialCompletionDto>();
    }
}
