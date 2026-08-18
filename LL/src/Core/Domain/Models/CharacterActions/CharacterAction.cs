using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Entities.Characters;
using Domain.Models.Professions.Crafting;

namespace Domain.Models.CharacterActions;
public class CharacterAction
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public CharacterActionType CharacterActionType => ActionDetails switch
    {
        CombatActionDetails => CharacterActionType.Combat,
        CraftingActionDetails => CharacterActionType.Crafting,
        _ => CharacterActionType.Idle
    };

    public ActionDetails? ActionDetails { get; set; }
    /// <summary>
    /// Earliest UTC boundary at which this action has work eligible for resolution.
    /// Null means that the retained action row has no active schedule.
    /// </summary>
    public DateTimeOffset? NextResolutionAtUtc { get; set; }
    /// <summary>
    /// Audit timestamp for the last persisted mutation. It is not a gameplay schedule.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>
    /// Earliest UTC timestamp at which combat may be replaced by another action.
    /// This is a fixed lock from combat start, not the rolling resolution boundary.
    /// </summary>
    public DateTimeOffset? BlockedUntilUtc { get; set; }
    public long ScheduleGeneration { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public uint RowVersion { get; set; }

    [NotMapped]
    public CombatSession? CombatSession { get; set; }
    [NotMapped]
    public TemperingSession? TemperingSession { get; set; }
    [NotMapped]
    public int ProcessedCount { get; set; }
    [NotMapped]
    public bool HasMoreDueWork { get; set; }
    [NotMapped]
    public int? ResolutionIntervalMs { get; set; }
    [NotMapped]
    public ICollection<CraftingQueueItem> PausedTemperingQueueItems { get; set; } = [];

    public CharacterAction(Guid characterId, ActionDetails actionDetails, DateTimeOffset now)
    {
        CharacterId = characterId;
        ActionDetails = actionDetails;
        NextResolutionAtUtc = now;
        UpdatedAt = now;
    }

    public CharacterAction()
    {

    }
}
