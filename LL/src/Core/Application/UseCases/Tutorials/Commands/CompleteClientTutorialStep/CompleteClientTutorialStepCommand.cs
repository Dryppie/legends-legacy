using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.Tutorials.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Tutorials.Commands.CompleteClientTutorialStep;

public sealed record CompleteClientTutorialStepCommand(
    Guid CharacterId,
    CompleteClientTutorialStepRequest Request) : ICommand<TutorialStateDto?>;

public sealed class CompleteClientTutorialStepCommandHandler
    : IRequestHandler<CompleteClientTutorialStepCommand, TutorialStateDto?>
{
    private readonly IMapper _mapper;
    private readonly ITutorialService _tutorialService;
    private readonly ITutorialProgressionService _progressionService;

    public CompleteClientTutorialStepCommandHandler(
        IMapper mapper,
        ITutorialService tutorialService,
        ITutorialProgressionService progressionService)
    {
        _mapper = mapper;
        _tutorialService = tutorialService;
        _progressionService = progressionService;
    }

    public async Task<TutorialStateDto?> Handle(
        CompleteClientTutorialStepCommand request,
        CancellationToken cancellationToken)
    {
        var activeState = await _tutorialService.GetStateAsync(request.CharacterId, cancellationToken);
        if (activeState is null || activeState.CurrentStep != request.Request.StepKey)
        {
            return _mapper.Map<TutorialStateDto?>(activeState);
        }

        if (!request.Request.TriggerType.StartsWith("Client", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The requested tutorial step cannot be completed by the client.");
        }

        var result = await _progressionService.TryProgressAsync(
            request.CharacterId,
            TutorialTrigger.ClientStep(
                request.Request.StepKey,
                request.Request.TriggerType,
                request.Request.Route),
            cancellationToken);

        var nextState = result?.State ??
            await _tutorialService.GetStateAsync(request.CharacterId, cancellationToken);

        return _mapper.Map<TutorialStateDto?>(nextState);
    }
}
