using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.Tutorials.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Tutorials.Commands.SkipTutorial;

public sealed record SkipTutorialCommand(Guid CharacterId) : ICommand<TutorialCompletionDto>;

public sealed class SkipTutorialCommandHandler(
    ITutorialService tutorialService,
    IMapper mapper)
    : IRequestHandler<SkipTutorialCommand, TutorialCompletionDto>
{
    public async Task<TutorialCompletionDto> Handle(
        SkipTutorialCommand request,
        CancellationToken cancellationToken)
    {
        var completion = await tutorialService.SkipAsync(
            request.CharacterId,
            cancellationToken);
        return mapper.Map<TutorialCompletionDto>(completion);
    }
}
