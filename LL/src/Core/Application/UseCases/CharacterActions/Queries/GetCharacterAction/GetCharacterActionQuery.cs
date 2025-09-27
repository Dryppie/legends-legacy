using Application.Interfaces.Services.LL.CharacterActions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CharacterActions.Queries.GetCharacterAction;
public record GetCharacterActionQuery(Guid CharacterId) : ICommand<Response<CharacterActionDto?>>;

public class GetCharacterActionQueryHandler : IRequestHandler<GetCharacterActionQuery, Response<CharacterActionDto?>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;
    public GetCharacterActionQueryHandler(ICharacterActionService characterActionService, IMapper mapper)
    {
        _characterActionService = characterActionService;
        _mapper = mapper;
    }
    public async Task<Response<CharacterActionDto?>> Handle(GetCharacterActionQuery request, CancellationToken cancellationToken)
    {
        var characterAction = await _characterActionService.GetCharacterActionAsync(request.CharacterId, cancellationToken);

        return Response<CharacterActionDto?>.Success(_mapper.Map<CharacterActionDto?>(characterAction));
    }
}
