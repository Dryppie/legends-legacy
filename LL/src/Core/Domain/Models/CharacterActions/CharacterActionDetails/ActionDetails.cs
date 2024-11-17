namespace Domain.Models.CharacterActions.CharacterActionDetails;
public abstract class ActionDetails
{
    public Guid Id { get; set; }
    public Guid CharacterActionId { get; set; }
}
