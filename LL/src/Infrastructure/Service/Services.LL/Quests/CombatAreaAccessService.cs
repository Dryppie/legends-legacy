using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.LL.Interfaces;
using Services.LL.WorldTower;

namespace Services.LL.Quests;

public sealed class CombatAreaAccessService : ICombatAreaAccessService
{
    private readonly IAreaService _areaService;
    private readonly IQuestRepository _questRepository;
    private readonly IDbContext? _db;
    private readonly string _serverId;

    public CombatAreaAccessService(
        IAreaService areaService,
        IQuestRepository questRepository,
        IDbContext? db = null,
        IOptions<WorldTowerOptions>? towerOptions = null)
    {
        _areaService = areaService;
        _questRepository = questRepository;
        _db = db;
        _serverId = towerOptions?.Value.ServerId ?? "default";
    }

    public async Task<CombatAreaAccessResult> GetAccessAsync(
        Guid characterId,
        string areaId,
        CancellationToken cancellationToken)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area is null)
        {
            return MissingArea(areaId);
        }

        var level = await _questRepository.GetCharacterLevelAsync(characterId, cancellationToken);
        var progresses = await _questRepository.GetProgressesAsync(characterId, cancellationToken);
        var clearedTowerFloors = await GetClearedTowerFloorsAsync([area], cancellationToken);
        return Resolve(area, level, progresses, clearedTowerFloors);
    }

    public async Task<IReadOnlyList<CombatAreaAccessResult>> GetAllAccessAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var level = await _questRepository.GetCharacterLevelAsync(characterId, cancellationToken);
        var progresses = await _questRepository.GetProgressesAsync(characterId, cancellationToken);
        var areas = await _areaService.GetAllAreasAsync(cancellationToken);
        var clearedTowerFloors = await GetClearedTowerFloorsAsync(areas, cancellationToken);
        return areas
            .OrderBy(x => x.DifficultyTier)
            .Select(area => Resolve(area, level, progresses, clearedTowerFloors))
            .ToList();
    }

    private async Task<HashSet<int>> GetClearedTowerFloorsAsync(
        IReadOnlyCollection<Area> areas,
        CancellationToken cancellationToken)
    {
        var requiredFloors = areas
            .Where(area => area.RequiredTowerFloor.HasValue)
            .Select(area => area.RequiredTowerFloor!.Value)
            .Distinct()
            .ToArray();
        if (_db is null || requiredFloors.Length == 0)
        {
            return [];
        }

        return (await _db.TowerFloorProgresses
                .Where(progress =>
                    progress.ServerId == _serverId &&
                    progress.IsCleared &&
                    requiredFloors.Contains(progress.FloorNumber))
                .Select(progress => progress.FloorNumber)
                .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    private static CombatAreaAccessResult Resolve(
        Area area,
        int? characterLevel,
        IReadOnlyList<CharacterQuestProgress> progresses,
        IReadOnlySet<int> clearedTowerFloors)
    {
        var requiredQuestIds = new[]
            {
                area.RequiredActiveQuestId,
                area.RequiredCompletedQuestId
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
        var unmetQuestIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(area.RequiredActiveQuestId) &&
            !progresses.Any(x =>
                x.QuestId.Equals(area.RequiredActiveQuestId, StringComparison.OrdinalIgnoreCase) &&
                x.Status == QuestStatus.Active))
        {
            unmetQuestIds.Add(area.RequiredActiveQuestId);
        }

        if (!string.IsNullOrWhiteSpace(area.RequiredCompletedQuestId) &&
            !progresses.Any(x =>
                x.QuestId.Equals(area.RequiredCompletedQuestId, StringComparison.OrdinalIgnoreCase) &&
                x.Status == QuestStatus.Completed))
        {
            unmetQuestIds.Add(area.RequiredCompletedQuestId);
        }

        var levelMet = characterLevel.HasValue && characterLevel.Value >= area.LevelRequirement;
        var towerFloorMet = !area.RequiredTowerFloor.HasValue ||
                            clearedTowerFloors.Contains(area.RequiredTowerFloor.Value);
        var canAccess = levelMet && unmetQuestIds.Count == 0 && towerFloorMet;
        var reasonCode = !towerFloorMet
            ? "tower_floor_requirement"
            : !levelMet
            ? "level_requirement"
            : unmetQuestIds.Count > 0
                ? "quest_requirement"
                : null;
        var message = !towerFloorMet
            ? $"Requires World Tower Floor {area.RequiredTowerFloor} to be completed."
            : !levelMet
            ? $"Requires character level {area.LevelRequirement}."
            : unmetQuestIds.Count > 0
                ? "Complete the required quest before entering this combat area."
                : null;

        return new CombatAreaAccessResult(
            area.Id,
            canAccess,
            canAccess || !area.HideWhenLocked,
            area.LevelRequirement,
            characterLevel,
            requiredQuestIds,
            unmetQuestIds,
            area.RequiredTowerFloor,
            towerFloorMet,
            reasonCode,
            message);
    }

    private static CombatAreaAccessResult MissingArea(string areaId) =>
        new(areaId, false, false, 0, null, [], [], null, false, "area_not_found", "Combat area was not found.");
}
