using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Queries.GetCharacterAction;
using Application.UseCases.Characters.Queries.GetCharacter;
using Application.UseCases.GameBootstrap.Dtos;
using Application.UseCases.Quests.Queries.GetQuestJournal;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Attributes;
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

        var questJournal = await _sender.Send(
            new GetQuestJournalQuery(request.CharacterId),
            cancellationToken);

        // Bootstrap is a read-only snapshot. Action advancement is intentionally
        // owned by CharacterActions/Resolve so reconnecting clients cannot launch
        // a second, competing offline resolver through this endpoint.
        var currentActionResponse = await _sender.Send(
            new GetCharacterActionQuery(request.CharacterId),
            cancellationToken);

        if (!currentActionResponse.IsSuccess)
        {
            return Response<GameBootstrapDto>.Fail(currentActionResponse.ErrorMessage);
        }

        var snapshot = new GameBootstrapSnapshot
        {
            Character = characterResponse.Data,
            QuestJournal = questJournal,
            CurrentAction = currentActionResponse.Data,
            ServerTimeUtc = DateTimeOffset.UtcNow,
            AttributeDefinitions = AttributeCatalog.All,
        };

        return Response<GameBootstrapDto>.Success(
            _mapper.Map<GameBootstrapDto>(snapshot));
    }
}
