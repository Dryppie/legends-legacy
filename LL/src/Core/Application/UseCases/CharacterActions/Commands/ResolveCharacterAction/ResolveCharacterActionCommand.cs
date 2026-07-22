using Application.Interfaces.Services.LL.CharacterActions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;

public sealed record ResolveCharacterActionCommand(Guid CharacterId)
    : ICommand<Response<CharacterActionDto?>>;

public sealed class ResolveCharacterActionCommandHandler
    : IRequestHandler<ResolveCharacterActionCommand, Response<CharacterActionDto?>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;

    public ResolveCharacterActionCommandHandler(
        ICharacterActionService characterActionService,
        IMapper mapper)
    {
        _characterActionService = characterActionService;
        _mapper = mapper;
    }

    public async Task<Response<CharacterActionDto?>> Handle(
        ResolveCharacterActionCommand request,
        CancellationToken cancellationToken)
    {
        var action = await _characterActionService.GetCharacterActionAsync(
            request.CharacterId,
            cancellationToken);

        return Response<CharacterActionDto?>.Success(
            _mapper.Map<CharacterActionDto?>(action));
    }
}
