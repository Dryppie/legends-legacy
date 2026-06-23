using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CharacterActions.Queries.GetCharacterAction;

public record GetCharacterActionQuery(Guid CharacterId) : ICommand<Response<CharacterActionDto?>>;

public class GetCharacterActionQueryHandler : IRequestHandler<GetCharacterActionQuery, Response<CharacterActionDto?>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IMapper _mapper;

    public GetCharacterActionQueryHandler(
        ICharacterActionService characterActionService,
        IGameRealtimeBroadcaster gameRealtime,
        IMapper mapper)
    {
        _characterActionService = characterActionService;
        _gameRealtime = gameRealtime;
        _mapper = mapper;
    }

    public async Task<Response<CharacterActionDto?>> Handle(GetCharacterActionQuery request, CancellationToken cancellationToken)
    {
        var characterAction = await _characterActionService.GetCharacterActionAsync(request.CharacterId, cancellationToken);
        var dto = _mapper.Map<CharacterActionDto?>(characterAction);

        if (dto?.CombatSession?.CombatResult is not null)
        {
            await _gameRealtime.PublishAsync(
                new Audience.Character(request.CharacterId),
                new IdleCombatProcessed(request.CharacterId, dto),
                nameof(GetCharacterActionQueryHandler),
                cancellationToken);
        }

        return Response<CharacterActionDto?>.Success(dto);
    }
}
