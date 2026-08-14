namespace Domain.Models.Colosseum.Tournaments;

public enum TournamentFormat
{
    SingleElimination = 0
}

public enum TournamentStatus
{
    Scheduled = 0,
    RegistrationOpen = 1,
    RegistrationClosed = 2,
    BracketGenerated = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}

public enum TournamentParticipantStatus
{
    Registered = 0,
    Active = 1,
    Eliminated = 2,
    Champion = 3,
    Withdrawn = 4
}

public enum TournamentTeamStatus
{
    Forming = 0,
    Active = 1,
    Eliminated = 2,
    Champion = 3,
    Disbanded = 4
}

public enum TournamentTeamRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3
}

public enum TournamentRoundStatus
{
    Pending = 0,
    Active = 1,
    Resolving = 2,
    Completed = 3
}

public enum TournamentMatchStatus
{
    Pending = 0,
    Ready = 1,
    Resolving = 2,
    Completed = 3,
    Bye = 4,
    Cancelled = 5
}

public enum TournamentMatchOutcome
{
    None = 0,
    PlayerOneWin = 1,
    PlayerTwoWin = 2,
    DrawAdvancedBySeed = 3,
    ByeAdvanced = 4,
    Forfeit = 5,
    DrawAdvancedByDamage = 6
}

public enum TournamentRewardStatus
{
    Unclaimed = 0,
    Claimed = 1
}
