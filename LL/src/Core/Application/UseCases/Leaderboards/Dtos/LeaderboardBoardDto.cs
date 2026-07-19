using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Leaderboards;

namespace Application.UseCases.Leaderboards.Dtos;

public sealed class LeaderboardBoardDto : IMapFrom<LeaderboardBoard>
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParticipantLabel { get; set; } = string.Empty;
    public string MetricLabel { get; set; } = string.Empty;
    public string? SecondaryMetricLabel { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public int TotalParticipants { get; set; }
    public int PageStartRank { get; set; }
    public int PageEndRank { get; set; }
    public string? PreviousCursor { get; set; }
    public string? NextCursor { get; set; }
    public string? SearchQuery { get; set; }
    public LeaderboardBoardEntryDto? SearchMatch { get; set; }
    public bool IsViewerRanked { get; set; }
    public string? ViewerUnrankedReason { get; set; }
    public List<LeaderboardBoardEntryDto> Entries { get; set; } = [];
    public LeaderboardBoardEntryDto? ViewerEntry { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<LeaderboardBoard, LeaderboardBoardDto>();
}
