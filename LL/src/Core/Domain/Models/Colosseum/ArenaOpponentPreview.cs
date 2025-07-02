using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;
public sealed record ArenaOpponentPreview
{
    public Character Opponent { get; init; } = default!;
    public ColosseumRatingPreview RatingDelta { get; init; } = default!;
}