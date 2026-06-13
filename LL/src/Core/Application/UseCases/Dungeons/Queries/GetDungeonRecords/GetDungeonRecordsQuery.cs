using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetDungeonRecords;

public record GetDungeonRecordsQuery(string FamilyId) : IQuery<DungeonRecordsDto>;

public sealed class GetDungeonRecordsQueryHandler : IRequestHandler<GetDungeonRecordsQuery, DungeonRecordsDto>
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonRunRepository _dungeonRuns;

    public GetDungeonRecordsQueryHandler(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonRunRepository dungeonRuns)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonRuns = dungeonRuns;
    }

    public async Task<DungeonRecordsDto> Handle(
        GetDungeonRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var familyId = request.FamilyId.Trim();
        var dungeons = _dungeonDefinitions.GetAll()
            .Where(x => string.Equals(DungeonDefinitionIdentity.GetFamilyId(x.Id), familyId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Grade)
            .ToList();

        if (dungeons.Count == 0)
        {
            return new DungeonRecordsDto { FamilyId = familyId };
        }

        var entries = await _dungeonRuns.GetCompletionLeaderboardAsync(
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var entriesByDungeon = entries
            .GroupBy(x => x.DungeonDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

        return new DungeonRecordsDto
        {
            FamilyId = familyId,
            FamilyTitle = DungeonDefinitionIdentity.GetFamilyTitle(dungeons[0].Name),
            Tiers = dungeons.Select(dungeon => new DungeonTierRecordsDto
            {
                DungeonDefinitionId = dungeon.Id,
                Difficulty = FormatDifficulty(dungeon.Grade),
                Grade = FormatGrade(dungeon.Grade),
                Records = MapRecords(TryGetRecords(entriesByDungeon, dungeon.Id))
            }).ToList()
        };
    }

    private static IReadOnlyList<DungeonCompletionLeaderboardEntry>? TryGetRecords(
        IReadOnlyDictionary<string, List<DungeonCompletionLeaderboardEntry>> entriesByDungeon,
        string dungeonDefinitionId)
    {
        return entriesByDungeon.TryGetValue(dungeonDefinitionId, out var entries)
            ? entries
            : null;
    }

    private static List<DungeonRecordEntryDto> MapRecords(IReadOnlyList<DungeonCompletionLeaderboardEntry>? entries) =>
        entries?
            .OrderBy(x => x.FirstCompletedAt)
            .ThenByDescending(x => x.CompletionCount)
            .Select(x => new DungeonRecordEntryDto
            {
                CharacterId = x.CharacterId,
                CharacterName = x.CharacterName,
                FirstClearedAt = x.FirstCompletedAt,
                LastClearedAt = x.LastCompletedAt,
                TotalClears = x.CompletionCount
            })
            .ToList() ?? [];

    private static string FormatGrade(DungeonGrade grade) =>
        grade switch
        {
            DungeonGrade.GradeII => "Grade II",
            DungeonGrade.GradeIII => "Grade III",
            _ => "Grade I"
        };

    private static string FormatDifficulty(DungeonGrade grade) =>
        grade switch
        {
            DungeonGrade.GradeII => "Veteran",
            DungeonGrade.GradeIII => "Champion",
            _ => "Novice"
        };
}
