using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;
public class ColosseumArenaRank
{
    public int Rank { get; set; }
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public int Rating { get; set; }
    public Guid SeasonId { get; set; }
}