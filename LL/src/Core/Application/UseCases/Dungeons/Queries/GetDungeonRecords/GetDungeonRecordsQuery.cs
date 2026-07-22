using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetDungeonRecords;

public record GetDungeonRecordsQuery(string FamilyId) : IQuery<DungeonRecordsDto>;

public sealed class GetDungeonRecordsQueryHandler : IRequestHandler<GetDungeonRecordsQuery, DungeonRecordsDto>
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonRunService _dungeonRuns;
    private readonly IMapper _mapper;

    public GetDungeonRecordsQueryHandler(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonRunService dungeonRuns,
        IMapper mapper)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonRuns = dungeonRuns;
        _mapper = mapper;
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

    private List<DungeonRecordEntryDto> MapRecords(IReadOnlyList<DungeonCompletionLeaderboardEntry>? entries)
    {
        var orderedEntries = entries?
            .OrderBy(entry => entry.FirstCompletedAt)
            .ThenByDescending(entry => entry.CompletionCount)
            .ToList() ?? [];

        return _mapper.Map<List<DungeonRecordEntryDto>>(orderedEntries);
    }

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
