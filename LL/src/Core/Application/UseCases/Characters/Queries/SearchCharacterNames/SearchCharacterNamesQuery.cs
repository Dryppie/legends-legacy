using Application.MediatR.Markers;
using Domain.Models.Entities.Characters;
using MediatR;

namespace Application.UseCases.Characters.Queries.SearchCharacterNames;

public sealed record SearchCharacterNamesQuery(Guid CharacterId, string Prefix)
    : IQuery<IReadOnlyList<string>>;

public sealed class SearchCharacterNamesQueryHandler(ICharacterRepository characters)
    : IRequestHandler<SearchCharacterNamesQuery, IReadOnlyList<string>>
{
    private const int SuggestionLimit = 8;

    public async Task<IReadOnlyList<string>> Handle(
        SearchCharacterNamesQuery request,
        CancellationToken cancellationToken)
    {
        var prefix = request.Prefix.Trim();
        if (prefix.Length < 2 || prefix.Length > 80)
            return [];

        return await characters.SearchCharacterNamesAsync(
            prefix,
            request.CharacterId,
            SuggestionLimit,
            cancellationToken);
    }
}
