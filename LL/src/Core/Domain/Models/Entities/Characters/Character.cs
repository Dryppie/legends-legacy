using Domain.Models.CharacterActions;
using Domain.Models.Colosseum;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Soulstones;
using Domain.Models.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Characters;
public class Character : Entity
{
    public AppUser User { get; set; } = null!;
    /// <summary>
    /// This should only ever be used in the backend, as it's used for authentication
    /// </summary>
    public Guid UserId { get; set; }
    public CharacterAction? CharacterAction { get; set; }
    public float Experience { get; set; } = 0;
    [NotMapped]
    public float ExperienceUntilNextLevel { get; set; }
    public long Cinders { get; set; } = 0;
    public long Soulstones { get; set; } = 0;
    public ICollection<CharacterSoulstoneUpgrade> CharacterSoulstoneUpgrades { get; set; } = [];
    public Inventory Inventory { get; set; } = null!;
    public int ArenaRating { get; set; } = 1000;
    public ICollection<ColosseumMatchResult> ColosseumMatches { get; set; } = [];
    public ArenaTicketStatus ArenaTicketStatus { get; set; } = null!;
    public Guild? Guild { get; set; }
    public ICollection<Profession> Professions { get; set; } = [];
}