using Domain.Models.CharacterActions;
using Domain.Models.Colosseum;
using Domain.Models.Achievements;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Essences;
using Domain.Models.Professions;
using Domain.Models.Soulstones;
using Domain.Models.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Characters;
public class Character : Entity
{
    public AppUser User { get; set; } = null!;
    public string NormalizedName { get; set; } = string.Empty;
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
    public long FateEcho { get; set; } = 0;
    public long SigilFragments { get; set; } = 0;
    public long AscensionStoneFragments { get; set; } = 0;
    public long GuildFavor { get; set; } = 0;
    public long GuildHonors { get; set; } = 0;
    public ICollection<CharacterSoulstoneUpgrade> CharacterSoulstoneUpgrades { get; set; } = [];
    public Inventory Inventory { get; set; } = null!;
    public CharacterArenaProfile ArenaProfile { get; set; } = null!;
    public ICollection<ColosseumMatchResult> ColosseumMatches { get; set; } = [];
    public ArenaTicketStatus ArenaTicketStatus { get; set; } = null!;
    public ICollection<EssenceLoadout> EssenceLoadouts { get; set; } = [];
    public Guid? EquippedTitleDefinitionId { get; set; }
    public TitleDisplayPosition EquippedTitleDisplayPosition { get; set; } = TitleDisplayPosition.Prefix;
    public TitleDefinition? EquippedTitleDefinition { get; set; }
    //public Guid? GuildId { get; set; }
    public Guild? Guild { get; set; }
    public ICollection<Profession> Professions { get; set; } = [];

    public void NormalizeName()
    {
        Name = Name.Trim();
        NormalizedName = string.IsNullOrWhiteSpace(Name)
            ? string.Empty
            : IdentityNormalizer.NormalizeRequired(Name);
    }
}
