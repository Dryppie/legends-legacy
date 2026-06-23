using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.WebSockets.Contracts;
using Application.WebSockets.Contracts.V2;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CharacterActions.Queries.GetCharacterAction;

public record GetCharacterActionQuery(Guid CharacterId) : ICommand<Response<CharacterActionDto?>>;

public class GetCharacterActionQueryHandler : IRequestHandler<GetCharacterActionQuery, Response<CharacterActionDto?>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IGameRealtimeBroadcasterV2 _gameRealtimeV2;
    private readonly IMapper _mapper;

    public GetCharacterActionQueryHandler(
        ICharacterActionService characterActionService,
        IGameRealtimeBroadcasterV2 gameRealtimeV2,
        IMapper mapper)
    {
        _characterActionService = characterActionService;
        _gameRealtimeV2 = gameRealtimeV2;
        _mapper = mapper;
    }

    public async Task<Response<CharacterActionDto?>> Handle(GetCharacterActionQuery request, CancellationToken cancellationToken)
    {
        var characterAction = await _characterActionService.GetCharacterActionAsync(request.CharacterId, cancellationToken);
        var dto = _mapper.Map<CharacterActionDto?>(characterAction);

        if (dto?.CombatSession?.CombatResult is not null)
        {
            await _gameRealtimeV2.PublishAsync(
                new Audience.Character(request.CharacterId),
                new IdleCombatProcessedV2(request.CharacterId, dto),
                nameof(GetCharacterActionQueryHandler),
                cancellationToken);
        }

        return Response<CharacterActionDto?>.Success(dto);
    }
}
