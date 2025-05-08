using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.CharacterActions;
public class CharacterAction
{
    private const int OFFLINE_DURATION = 12;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public CharacterActionType CharacterActionType => ActionDetails switch
    {
        GatheringActionDetails => CharacterActionType.Gathering,
        CombatActionDetails => CharacterActionType.Combat,
        //GatheringActionDetails => CharacterActionType.Gathering,
        _ => CharacterActionType.Idle
    };

    public ActionDetails? ActionDetails { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    //public bool IsCapped => DateTimeOffset.UtcNow >= UpdatedAt.AddHours(OFFLINE_DURATION);
    [NotMapped]
    public CombatSession? CombatSession { get; set; }

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