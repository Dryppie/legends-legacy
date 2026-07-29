using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Tutorials;
using AutoMapper;

namespace Application.UseCases.Tutorials.Dtos;

public sealed class TutorialStateDto : IMapFrom<TutorialState>
{
    public string TutorialId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Version { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public int CurrentAmount { get; set; }
    public int RequiredAmount { get; set; }
    public int CurrentStepIndex { get; set; }
    public int TotalSteps { get; set; }
    public TutorialStepPresentationDto Presentation { get; set; } = new();
    public string ActionLabel { get; set; } = string.Empty;
    public string DestinationRoute { get; set; } = string.Empty;
    public string? GuidePageId { get; set; }
    public string? TourPageId { get; set; }
    public bool IsCompleted { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TutorialState, TutorialStateDto>();
    }
}

public sealed class TutorialStepPresentationDto : IMapFrom<TutorialStepPresentation>
{
    public string ActionLabel { get; set; } = string.Empty;
    public string DestinationRoute { get; set; } = string.Empty;
    public string? GuidePageId { get; set; }
    public string? TourPageId { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TutorialStepPresentation, TutorialStepPresentationDto>();
    }
}
