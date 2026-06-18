using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Entities.Characters;

namespace Domain.Models.CharacterActions;
public class CharacterAction
{
    private const int OFFLINE_DURATION = 12;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public CharacterActionType CharacterActionType => ActionDetails switch
    {
        CombatActionDetails => CharacterActionType.Combat,
        CraftingActionDetails => CharacterActionType.Crafting,
        _ => CharacterActionType.Idle
    };

    public ActionDetails? ActionDetails { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    //public bool IsCapped => DateTimeOffset.UtcNow >= UpdatedAt.AddHours(OFFLINE_DURATION);
    [NotMapped]
    public CombatSession? CombatSession { get; set; }
    [NotMapped]
    public TemperingSession? TemperingSession { get; set; }

    public CharacterAction(Guid characterId, ActionDetails actionDetails)
    {
        CharacterId = characterId;
        ActionDetails = actionDetails;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public CharacterAction()
    {

    }
}
