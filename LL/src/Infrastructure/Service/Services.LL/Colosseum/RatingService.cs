using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum;
using Domain.Models.Combat;

namespace Services.LL.Colosseum;
public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;
    private readonly Elo32Calculator _calculator = new();

    public RatingService(IRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task CalculateNewColosseumRatingsAsync(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken)
    {
        int ratingA = await _ratingRepository.GetColosseumRatingAsync(characterId, cancellationToken);
        int ratingB = await _ratingRepository.GetColosseumRatingAsync(enemyId, cancellationToken);

        // 2. Calculate new ratings
        var (newA, newB) = _calculator.Calculate(ratingA, ratingB, outcome);

        // 3. Save new ratings (replace with your actual saving logic)
        await _ratingRepository.SetColosseumRatingAsync(characterId, newA, cancellationToken);
        await _ratingRepository.SetColosseumRatingAsync(enemyId, newB, cancellationToken);
    }

    public ColosseumRatingPreview Preview(int myRating, int opponentRating)
    {
        var (win, _) = _calculator.Calculate(myRating, opponentRating, BattleOutcome.Victory);
        var (loss, _) = _calculator.Calculate(myRating, opponentRating, BattleOutcome.Defeat);
        var (draw, _) = _calculator.Calculate(myRating, opponentRating, BattleOutcome.Draw);

        return new ColosseumRatingPreview
        {
            CurrentRating = myRating,
            OpponentRating = opponentRating,
            RatingIfVictory = win,
            RatingIfDefeat = loss,
            RatingIfDraw = draw
        };
    }
}