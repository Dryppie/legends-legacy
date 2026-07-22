using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.Characters.Queries.GetCharacter;
using Application.UseCases.GameBootstrap.Dtos;
using Application.UseCases.Tutorials.Queries.GetTutorialState;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.GameBootstrap.Queries.GetGameBootstrap;

public sealed record GetGameBootstrapQuery(Guid UserId, Guid CharacterId)
    : IQuery<Response<GameBootstrapDto>>;

public sealed class GetGameBootstrapQueryHandler
    : IRequestHandler<GetGameBootstrapQuery, Response<GameBootstrapDto>>
{
    private readonly IMapper _mapper;
    private readonly ISender _sender;

    public GetGameBootstrapQueryHandler(
        IMapper mapper,
        ISender sender)
    {
        _mapper = mapper;
        _sender = sender;
    }

    public async Task<Response<GameBootstrapDto>> Handle(
        GetGameBootstrapQuery request,
        CancellationToken cancellationToken)
    {
        var characterResponse = await _sender.Send(
            new GetCharacterQuery(request.UserId),
            cancellationToken);

        if (!characterResponse.IsSuccess || characterResponse.Data is null)
        {
            return Response<GameBootstrapDto>.Fail("Character was not found.");
        }

        var tutorial = await _sender.Send(
            new GetTutorialStateQuery(request.CharacterId),
            cancellationToken);

        var currentActionResponse = await _sender.Send(
            new ResolveCharacterActionCommand(request.CharacterId),
            cancellationToken);

        if (!currentActionResponse.IsSuccess)
        {
            return Response<GameBootstrapDto>.Fail(currentActionResponse.ErrorMessage);
        }

        var snapshot = new GameBootstrapSnapshot
        {
            Character = characterResponse.Data,
            Tutorial = tutorial,
            CurrentAction = currentActionResponse.Data,
            ServerTimeUtc = DateTimeOffset.UtcNow,
        };

        return Response<GameBootstrapDto>.Success(
            _mapper.Map<GameBootstrapDto>(snapshot));
    }
}
