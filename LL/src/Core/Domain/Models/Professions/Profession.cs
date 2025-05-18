using Domain.Models.Entities.Characters;

namespace Domain.Models.Professions;
public class Profession
{
    public Guid CharacerId { get; set; }
    public Character Character { get; set; } = null!;
    public ProfessionType ProfessionType { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
}
