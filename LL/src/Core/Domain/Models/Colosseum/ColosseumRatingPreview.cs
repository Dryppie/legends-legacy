namespace Domain.Models.Colosseum;
public sealed record ColosseumRatingPreview
{
    public int CurrentRating { get; init; }
    public int OpponentRating { get; init; }

    public int RatingIfVictory { get; init; }
    public int RatingIfDefeat { get; init; }
    public int RatingIfDraw { get; init; }

    public int DeltaIfVictory => RatingIfVictory - CurrentRating;
    public int DeltaIfDefeat => RatingIfDefeat - CurrentRating;
    public int DeltaIfDraw => RatingIfDraw - CurrentRating;
}