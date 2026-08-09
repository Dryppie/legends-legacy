using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Quests;

public sealed class CombatAreaAccessService(
    IAreaService areaService,
    IQuestRepository questRepository) : ICombatAreaAccessService
{
    public async Task<CombatAreaAccessResult> GetAccessAsync(
        Guid characterId,
        string areaId,
        CancellationToken cancellationToken)
    {
        var area = await areaService.GetAreaByIdAsync(areaId);
        if (area is null)
        {
            return MissingArea(areaId);
        }

        var level = await questRepository.GetCharacterLevelAsync(characterId, cancellationToken);
        var progresses = await questRepository.GetProgressesAsync(characterId, cancellationToken);
        return Resolve(area, level, progresses);
    }

    public async Task<IReadOnlyList<CombatAreaAccessResult>> GetAllAccessAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var level = await questRepository.GetCharacterLevelAsync(characterId, cancellationToken);
        var progresses = await questRepository.GetProgressesAsync(characterId, cancellationToken);
        var areas = await areaService.GetAllAreasAsync(cancellationToken);
        return areas
            .OrderBy(x => x.DifficultyTier)
            .Select(area => Resolve(area, level, progresses))
            .ToList();
    }

    private static CombatAreaAccessResult Resolve(
        Area area,
        int? characterLevel,
        IReadOnlyList<CharacterQuestProgress> progresses)
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
        var canAccess = levelMet && unmetQuestIds.Count == 0;
        var reasonCode = !levelMet
            ? "level_requirement"
            : unmetQuestIds.Count > 0
                ? "quest_requirement"
                : null;
        var message = !levelMet
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
            reasonCode,
            message);
    }

    private static CombatAreaAccessResult MissingArea(string areaId) =>
        new(areaId, false, false, 0, null, [], [], "area_not_found", "Combat area was not found.");
}
