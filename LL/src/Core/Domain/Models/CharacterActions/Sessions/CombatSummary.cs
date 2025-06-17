namespace Domain.Models.CharacterActions.Sessions;
public class CombatSummary
{
    public int TotalBattles { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int TotalExperience { get; set; }
    public int TotalCinders { get; set; }
    public int TotalSoulstones { get; set; } = 0;
}