using Application.Interfaces.Services.LL.Entities;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterIdByName;
public record GetCharacterIdByNameQuery(string Name) : IRequest<Response<Guid>>;
public class GetCharacterIdByNameQueryHandler : IRequestHandler<GetCharacterIdByNameQuery, Response<Guid>>
{
    private readonly ICharacterService _characterService;
    public GetCharacterIdByNameQueryHandler(ICharacterService characterService)
    {
        _characterService = characterService;
    }
    public async Task<Response<Guid>> Handle(GetCharacterIdByNameQuery request, CancellationToken cancellationToken)
    {
        var characterId = await _characterService.GetCharacterIdByNameAsync(request.Name, cancellationToken);
        return characterId != null
            ? Response<Guid>.Success(characterId.Value)
            : Response<Guid>.Fail("Character not found.");
    }
}