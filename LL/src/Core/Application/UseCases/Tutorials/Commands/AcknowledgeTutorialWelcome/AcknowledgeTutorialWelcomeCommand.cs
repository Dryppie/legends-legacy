using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.Tutorials.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Tutorials.Commands.AcknowledgeTutorialWelcome;

public sealed record AcknowledgeTutorialWelcomeCommand(Guid CharacterId)
    : ICommand<TutorialStateDto?>;

public sealed class AcknowledgeTutorialWelcomeCommandHandler(
    ITutorialService tutorialService,
    IMapper mapper)
    : IRequestHandler<AcknowledgeTutorialWelcomeCommand, TutorialStateDto?>
{
    public async Task<TutorialStateDto?> Handle(
        AcknowledgeTutorialWelcomeCommand request,
        CancellationToken cancellationToken)
    {
        var state = await tutorialService.AcknowledgeWelcomeAsync(
            request.CharacterId,
            cancellationToken);
        return mapper.Map<TutorialStateDto?>(state);
    }
}
