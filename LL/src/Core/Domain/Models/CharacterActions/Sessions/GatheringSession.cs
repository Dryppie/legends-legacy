namespace Domain.Models.CharacterActions.Sessions;
public class GatheringSession
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public GatheringSummary GatheringSummary { get; set; } = null!;
}
