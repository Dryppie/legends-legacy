namespace Domain.Models.Colosseum.Tournaments;

public static class TournamentRules
{
    public static int GetBracketSize(int participantCount)
    {
        var power = 1;
        while (power < participantCount) power *= 2;
        return Math.Max(2, power);
    }

    public static int GetByeCount(int participantCount)
    {
        return GetBracketSize(participantCount) - participantCount;
    }

    public static string GetRoundName(int roundNumber, int roundCount)
    {
        var remaining = roundCount - roundNumber + 1;
        return remaining switch
        {
            1 => "Final",
            2 => "Semi-final",
            3 => "Quarter-final",
            _ => $"Round {roundNumber}"
        };
    }

    public static int CalculatePlacement(int roundCount, int eliminatedRound)
    {
        var remaining = roundCount - eliminatedRound + 1;
        return remaining switch
        {
            1 => 2,
            2 => 3,
            3 => 5,
            _ => 9
        };
    }
}
