using Domain.Models.Combat;

namespace Domain.Models.CharacterActions.Sessions;
public class TemperingSession
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public TemperingSummary TemperingSummary { get; set; } = null!;
    public List<TemperingOutcomeEntry> Outcomes { get; set; } = [];
    public TemperingSession()
    {
        TemperingSummary = new TemperingSummary();
    }
}
