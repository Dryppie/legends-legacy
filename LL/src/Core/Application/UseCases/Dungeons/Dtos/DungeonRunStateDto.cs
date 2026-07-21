using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonRunStateDto : IMapFrom<DungeonRunState>
{
    public DungeonLootBagDto SecuredLoot { get; set; } = new();
    public DungeonLootBagDto PendingLoot { get; set; } = new();
    public List<DungeonMapNodeDto> MapNodes { get; set; } = [];
    public List<int> TraversedRoomIndexes { get; set; } = [];
    public List<DungeonRouteOptionDto> CurrentRouteOptions { get; set; } = [];
    public List<DungeonMasteryAwardReasonDto> MasteryAwardReasons { get; set; } = [];
    public int Vigor { get; set; } = 100;
    public string VigorState { get; set; } = "Steady";
    public List<DungeonVigorThresholdDto> VigorThresholds { get; set; } = [];
    public int CurrentSection { get; set; } = 1;
    public int TotalSections { get; set; } = 1;
    public int RestSitesVisited { get; set; }
    public string LastConsequence { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public List<DungeonVigorChangeDto> VigorHistory { get; set; } = [];
    public DungeonFailureAnalysisDto? FailureAnalysis { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonRunState, DungeonRunStateDto>();
}

public sealed class DungeonVigorThresholdDto : IMapFrom<DungeonVigorThreshold>
{
    public string State { get; set; } = string.Empty;
    public int MinimumVigor { get; set; }
    public int MaximumVigor { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Effects { get; set; } = [];
    public bool IsCurrent { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonVigorThreshold, DungeonVigorThresholdDto>();
}

public sealed class DungeonMapNodeDto : IMapFrom<DungeonMapNode>
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int RoomIndex { get; set; }
    public int Depth { get; set; }
    public int Lane { get; set; }
    public int Section { get; set; }
    public string Forecast { get; set; } = string.Empty;
    public int VigorCostMin { get; set; }
    public int VigorCostMax { get; set; }
    public List<int> NextRoomIndexes { get; set; } = [];

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonMapNode, DungeonMapNodeDto>();
}

public sealed class DungeonVigorChangeDto : IMapFrom<DungeonVigorChange>
{
    public int RoomIndex { get; set; }
    public int Amount { get; set; }
    public int VigorAfter { get; set; }
    public string Reason { get; set; } = string.Empty;

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonVigorChange, DungeonVigorChangeDto>();
}

public sealed class DungeonFailureAnalysisDto : IMapFrom<DungeonFailureAnalysis>
{
    public string Location { get; set; } = string.Empty;
    public int Section { get; set; }
    public string PrimaryCause { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = [];
    public DungeonLootBagDto LostPendingLoot { get; set; } = new();

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonFailureAnalysis, DungeonFailureAnalysisDto>();
}

public sealed class DungeonMasteryAwardReasonDto : IMapFrom<DungeonMasteryAwardReason>
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Experience { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonMasteryAwardReason, DungeonMasteryAwardReasonDto>();
}

public sealed class DungeonLootBagDto : IMapFrom<DungeonLootBag>
{
    public int Experience { get; set; }
    public int Cinders { get; set; }
    public int Soulstones { get; set; }
    public Dictionary<string, int> Items { get; set; } = [];

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonLootBag, DungeonLootBagDto>();
}

public sealed class DungeonRouteOptionDto : IMapFrom<DungeonRouteOption>
{
    public string Id { get; set; } = string.Empty;
    public int RoomIndex { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int RiskLevel { get; set; }
    public int VigorCostMin { get; set; }
    public int VigorCostMax { get; set; }
    public string Forecast { get; set; } = string.Empty;

    public void Mapping(Profile profile) =>
        profile.CreateMap<DungeonRouteOption, DungeonRouteOptionDto>();
}
