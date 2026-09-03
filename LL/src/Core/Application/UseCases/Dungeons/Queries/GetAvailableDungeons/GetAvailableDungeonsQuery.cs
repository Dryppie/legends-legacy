using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Rewards;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Rewards;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetAvailableDungeons;

public record GetAvailableDungeonsQuery(Guid CharacterId) : IQuery<DungeonHubDto>;

public sealed class GetAvailableDungeonsQueryHandler(DungeonHubFactory hub)
    : IRequestHandler<GetAvailableDungeonsQuery, DungeonHubDto>
{
    public Task<DungeonHubDto> Handle(
        GetAvailableDungeonsQuery request,
        CancellationToken cancellationToken) =>
        hub.CreateAsync(request.CharacterId, cancellationToken);
}

public sealed class DungeonHubFactory
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonAccessPolicy _dungeonAccess;
    private readonly IDungeonPreviewRewardService _previewRewards;
    private readonly ICharacterRepository _characters;
    private readonly IDungeonRunService _dungeonRuns;
    private readonly IDungeonMasteryService _mastery;
    private readonly IDungeonSigilAssemblySettingsProvider _sigilAssemblySettings;
    private readonly IMapper _mapper;

    public DungeonHubFactory(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonAccessPolicy dungeonAccess,
        IDungeonPreviewRewardService previewRewards,
        ICharacterRepository characters,
        IDungeonRunService dungeonRuns,
        IDungeonMasteryService mastery,
        IDungeonSigilAssemblySettingsProvider sigilAssemblySettings,
        IMapper mapper)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonAccess = dungeonAccess;
        _previewRewards = previewRewards;
        _characters = characters;
        _dungeonRuns = dungeonRuns;
        _mastery = mastery;
        _sigilAssemblySettings = sigilAssemblySettings;
        _mapper = mapper;
    }

    public async Task<DungeonHubDto> CreateAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await CreateAsync(
            characterId,
            new Dictionary<string, int>(),
            cancellationToken);

    public async Task<DungeonHubDto> CreateAsync(
        Guid characterId,
        IReadOnlyDictionary<string, int> inventoryQuantityOverrides,
        CancellationToken cancellationToken)
    {
        var previews = new List<DungeonPreviewDto>();
        var sigilFragments = await _characters.GetSigilFragmentsAsync(
            characterId,
            cancellationToken);

        var dungeons = _dungeonDefinitions.GetAll()
            .OrderBy(x => DungeonDefinitionIdentity.GetFamilyId(x.Id))
            .ThenBy(x => x.Grade)
            .ToList();
        var completionRecords = await _dungeonRuns.GetCompletionRecordsAsync(
            characterId,
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var records = completionRecords.ToDictionary(
            x => x.DungeonDefinitionId,
            StringComparer.OrdinalIgnoreCase);
        var masteryByDungeon = await _mastery.GetMasteryByDungeonAsync(
            characterId,
            dungeons.Select(x => x.Id).ToArray(),
            cancellationToken);
        var accessByDungeon = await _dungeonAccess.EvaluateForPreviewAsync(
            characterId,
            dungeons,
            inventoryQuantityOverrides,
            cancellationToken);
        var rewardsByDungeon = await _previewRewards.GetPossibleCompletionRewardsAsync(
            dungeons,
            cancellationToken);
        var sigilSettings = _sigilAssemblySettings.GetSettings();

        foreach (var dungeon in dungeons)
        {
            records.TryGetValue(dungeon.Id, out var record);
            masteryByDungeon.TryGetValue(dungeon.Id, out var mastery);
            var accessSnapshot = accessByDungeon[dungeon.Id];
            var access = accessSnapshot.Entry;
            var sigilAssemblyAccess = accessSnapshot.SigilAssembly;
            var sigilRequirement = access.EntryRequirements.FirstOrDefault(x =>
                x.ItemId.Equals(dungeon.SigilItemId, StringComparison.OrdinalIgnoreCase));

            previews.Add(new DungeonPreviewDto
            {
                Id = dungeon.Id,
                Region = dungeon.Region,
                FamilyId = DungeonDefinitionIdentity.GetFamilyId(dungeon.Id),
                FamilyTitle = DungeonDefinitionIdentity.GetFamilyTitle(dungeon.Name),
                Title = dungeon.Name,
                Difficulty = FormatDifficulty(dungeon.Grade),
                Tier = dungeon.Tier,
                Grade = FormatGrade(dungeon.Grade),
                CanEnter = access.CanEnter,
                MissingRequirements = [.. access.MissingRequirements],
                EntryRequirements = _mapper.Map<List<DungeonEntryRequirementDto>>(access.EntryRequirements),
                SigilItemId = string.IsNullOrWhiteSpace(dungeon.SigilItemId) ? null : dungeon.SigilItemId,
                SigilName = sigilRequirement?.Name,
                CanAssembleSigil = sigilSettings.Enabled && sigilAssemblyAccess?.CanEnter == true,
                SigilAssemblyMissingRequirements = sigilAssemblyAccess?.MissingRequirements.ToList() ?? [],
                RequiredTowerFloor = dungeon.RequiredTowerFloor,
                RequiredPreviousDungeonId = dungeon.RequiredPreviousDungeonId,
                MinRooms = dungeon.MinRooms,
                MaxRooms = dungeon.MaxRooms,
                Record = MapRecord(record),
                Mastery = mastery is null ? new DungeonMasteryDto() : _mapper.Map<DungeonMasteryDto>(mastery),
                Rewards = _mapper.Map<List<DungeonPreviewRewardDto>>(rewardsByDungeon[dungeon.Id]),
            });
        }

        return new DungeonHubDto
        {
            SigilFragments = sigilFragments ?? 0,
            SigilAssemblyEnabled = sigilSettings.Enabled,
            SigilAssemblyCost = sigilSettings.FragmentCost,
            Dungeons = previews
        };
    }

    private DungeonRecordDto MapRecord(DungeonCompletionRecord? record)
    {
        return record is null
            ? new DungeonRecordDto()
            : _mapper.Map<DungeonRecordDto>(record);
    }

    private static string FormatGrade(Domain.Models.Dungeons.Definitions.DungeonGrade grade) =>
        grade switch
        {
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeII => "Grade II",
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeIII => "Grade III",
            _ => "Grade I"
        };

    private static string FormatDifficulty(Domain.Models.Dungeons.Definitions.DungeonGrade grade) =>
        grade switch
        {
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeII => "Veteran",
            Domain.Models.Dungeons.Definitions.DungeonGrade.GradeIII => "Champion",
            _ => "Novice"
        };

}
