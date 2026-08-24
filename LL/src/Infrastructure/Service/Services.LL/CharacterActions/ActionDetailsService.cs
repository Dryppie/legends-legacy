using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class ActionDetailsService : IActionDetailsService
{
    private readonly IEntityService _entityService;
    private readonly IAreaService _areaService;

    public ActionDetailsService(IEntityService entityService, IAreaService areaService)
    {
        _entityService = entityService;
        _areaService = areaService;
    }

    public async Task<CombatActionDetails?> CreateCombatActionDetailsAsync(string areaId, Guid characterId, CancellationToken cancellationToken)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        var character = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        if (area == null || character.Count == 0 || area.LevelRequirement > character.FirstOrDefault()?.Level) return null;

        return new CombatActionDetails
        {
            CharacterTeam = [characterId],
            AreaId = area.Id,
            Area = area,
        };
    }
}
