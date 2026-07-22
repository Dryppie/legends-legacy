using Application.Interfaces.Services.LL.PowerRatings;
using Application.MediatR.Markers;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetPowerRating;

public sealed record GetPowerRatingQuery(Guid CharacterId) : IQuery<PowerRatingSnapshot>;

public sealed class GetPowerRatingQueryHandler : IRequestHandler<GetPowerRatingQuery, PowerRatingSnapshot>
{
    private readonly IPowerRatingService _powerRatings;

    public GetPowerRatingQueryHandler(IPowerRatingService powerRatings)
    {
        _powerRatings = powerRatings;
    }

    public Task<PowerRatingSnapshot> Handle(
        GetPowerRatingQuery request,
        CancellationToken cancellationToken) =>
        _powerRatings.GetCharacterRatingAsync(request.CharacterId, cancellationToken);
}
