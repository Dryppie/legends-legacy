using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.CharacterActions.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.CharacterActions.Queries.GetCharacterAction;
public record GetCharacterActionQuery(Guid CharacterId) : IRequest<Response<CharacterActionDto?>>;

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
        try
        {
            var characterAction = await _characterActionService.GetCharacterActionAsync(request.CharacterId, cancellationToken);

            var characterActionDto = _mapper.Map<CharacterActionDto>(characterAction);
            return Response<CharacterActionDto?>.Success(characterActionDto);
        }
        catch (Exception)
        {
            return Response<CharacterActionDto?>.Fail("Error getting character action: " +  request.CharacterId);
        }
        
    }
}
