using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.Tutorials.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Tutorials.Commands.AttuneStarterEssence;

public sealed record AttuneStarterEssenceCommand(Guid CharacterId) : ICommand<TutorialStateDto?>;

public sealed class AttuneStarterEssenceCommandHandler(
    ITutorialService tutorialService,
    IMapper mapper)
    : IRequestHandler<AttuneStarterEssenceCommand, TutorialStateDto?>
{
    public async Task<TutorialStateDto?> Handle(
        AttuneStarterEssenceCommand request,
        CancellationToken cancellationToken)
    {
        var state = await tutorialService.AttuneStarterEssenceAsync(
            request.CharacterId,
            cancellationToken);
        return mapper.Map<TutorialStateDto?>(state);
    }
}
