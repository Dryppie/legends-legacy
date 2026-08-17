using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using Domain.Models.CharacterActions;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.StartCombatAction;
public record StartCombatActionCommand(Guid CharacterId, string AreaId) : ICommand<Response<CharacterActionDto>>;
public class StartCombatActionCommandHandler : IRequestHandler<StartCombatActionCommand, Response<CharacterActionDto>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IActionDetailsService _actionDetailsService;
    private readonly ICombatAreaAccessService _combatAreaAccessService;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;

    public StartCombatActionCommandHandler(
        ICharacterActionService characterActionService,
        IActionDetailsService actionDetailsService,
        ICombatAreaAccessService combatAreaAccessService,
        IMapper mapper,
        TimeProvider? timeProvider = null)
    {
        _characterActionService = characterActionService;
        _actionDetailsService = actionDetailsService;
        _combatAreaAccessService = combatAreaAccessService;
        _mapper = mapper;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Response<CharacterActionDto>> Handle(StartCombatActionCommand request, CancellationToken cancellationToken)
    {
        var access = await _combatAreaAccessService.GetAccessAsync(
                request.CharacterId,
                request.AreaId,
                cancellationToken);
        if (!access.CanAccess)
        {
            return Response<CharacterActionDto>.Fail(
                access.PlayerMessage ?? "This combat area is locked.");
        }

        var combatActionDetails = await _actionDetailsService.CreateCombatActionDetailsAsync(request.AreaId, request.CharacterId, cancellationToken);
        if (combatActionDetails == null)
            return Response<CharacterActionDto>.Fail("Unable to start combat.");

        var now = _timeProvider.GetUtcNow();
        var characterAction = new CharacterAction(request.CharacterId, combatActionDetails, now);

        var startedAction = await _characterActionService.StartCharacterActionAsync(characterAction, now, cancellationToken);
        return startedAction is not null
            ? Response<CharacterActionDto>.Success(_mapper.Map<CharacterActionDto>(startedAction))
            : Response<CharacterActionDto>.Fail("Unable to start combat");
    }
}
