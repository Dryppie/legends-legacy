namespace Domain.Models.Colosseum.Tournaments;

public static class TournamentScoring
{
    public static int CalculatePoints(int? placement)
    {
        return placement switch
        {
            1 => 100,
            2 => 60,
            <= 4 => 35,
            <= 8 => 20,
            null => 0,
            _ => 10
        };
    }
}
