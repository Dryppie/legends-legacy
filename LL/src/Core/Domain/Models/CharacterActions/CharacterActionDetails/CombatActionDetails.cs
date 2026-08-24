using Domain.Models.Regions.Areas;

namespace Domain.Models.CharacterActions.CharacterActionDetails;
public class CombatActionDetails : ActionDetails
{
    public List<Guid> CharacterTeam { get; set; } = [];
    public string AreaId { get; set; } = string.Empty;
    public Area Area { get; set; }

    public CombatActionDetails(List<Guid> characterTeam, Area area)
    {
        CharacterTeam = characterTeam;
        AreaId = area.Id;
        Area = area;
    }
    public CombatActionDetails()
    {

    }
}
