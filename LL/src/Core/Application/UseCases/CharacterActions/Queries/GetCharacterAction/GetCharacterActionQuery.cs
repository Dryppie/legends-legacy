using Application.Interfaces.Services.LL;
using Application.UseCases.CharacterActions.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.CharacterActions.Queries.GetCharacterAction;
public record GetCharacterActionQuery(Guid CharacterId) : IRequest<CharacterActionDto?>;

public class GetCharacterActionQueryHandler : IRequestHandler<GetCharacterActionQuery, CharacterActionDto?>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;
    public GetCharacterActionQueryHandler(ICharacterActionService characterActionService, IMapper mapper)
    {
        _characterActionService = characterActionService;
        _mapper = mapper;
    }
    public async Task<CharacterActionDto?> Handle(GetCharacterActionQuery request, CancellationToken cancellationToken)
    {
        var characterAction = await _characterActionService.GetCharacterActionAsync(request.CharacterId, cancellationToken);

        return _mapper.Map<CharacterActionDto>(characterAction);
    }
}
