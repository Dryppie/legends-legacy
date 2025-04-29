using Application.Interfaces.Services.LL;
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
}