using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterIdByName;
public record GetCharacterIdByNameQuery(string Name) : IQuery<Response<Guid?>>;
public class GetCharacterIdByNameQueryHandler : IRequestHandler<GetCharacterIdByNameQuery, Response<Guid?>>
{
    private readonly ICharacterService _characterService;
    public GetCharacterIdByNameQueryHandler(ICharacterService characterService)
    {
        _characterService = characterService;
    }
    public async Task<Response<Guid?>> Handle(GetCharacterIdByNameQuery request, CancellationToken cancellationToken)
    {
        var characterId = await _characterService.GetCharacterIdByNameAsync(request.Name, cancellationToken);
        return characterId != Guid.Empty
            ? Response<Guid?>.Success(characterId.Value)
            : Response<Guid?>.Fail("Character not found.");
    }
}