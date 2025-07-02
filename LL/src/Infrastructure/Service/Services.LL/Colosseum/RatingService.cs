using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum;
using Domain.Models.Combat;

namespace Services.LL.Colosseum;
public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;

    public RatingService(IRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task CalculateNewColosseumRatingsAsync(Guid characterId, Guid enemyId, BattleOutcome outcome, CancellationToken cancellationToken)
    {
        int ratingA = await _ratingRepository.GetColosseumRatingAsync(characterId, cancellationToken);
        int ratingB = await _ratingRepository.GetColosseumRatingAsync(enemyId, cancellationToken);

        // 2. Calculate new ratings
        var calculator = new Elo32Calculator();
        var (newA, newB) = calculator.Calculate(ratingA, ratingB, outcome);

        // 3. Save new ratings (replace with your actual saving logic)
        await _ratingRepository.SetColosseumRatingAsync(characterId, newA, cancellationToken);
        await _ratingRepository.SetColosseumRatingAsync(enemyId, newB, cancellationToken);
    }

    public async Task<ColosseumRatingPreview> PreviewColosseumRatingAsync(Guid characterId, Guid enemyId, CancellationToken cancellationToken)
    {
        // 1. Read current ratings
        int ratingA = await _ratingRepository.GetColosseumRatingAsync(characterId, cancellationToken);
        int ratingB = await _ratingRepository.GetColosseumRatingAsync(enemyId, cancellationToken);

        // 2. Run the calculator for each possible outcome
        var calculator = new Elo32Calculator();

        var (aIfWin, _) = calculator.Calculate(ratingA, ratingB, BattleOutcome.Victory);
        var (aIfLoss, _) = calculator.Calculate(ratingA, ratingB, BattleOutcome.Defeat);
        var (aIfDraw, _) = calculator.Calculate(ratingA, ratingB, BattleOutcome.Draw);

        // 3. Package and return (note: **nothing** is persisted here)
        return new ColosseumRatingPreview
        {
            CurrentRating = ratingA,
            OpponentRating = ratingB,
            RatingIfVictory = aIfWin,
            RatingIfDefeat = aIfLoss,
            RatingIfDraw = aIfDraw
        };
    }
}