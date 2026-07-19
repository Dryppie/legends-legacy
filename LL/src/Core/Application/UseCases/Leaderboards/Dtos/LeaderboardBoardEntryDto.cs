using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Leaderboards;

namespace Application.UseCases.Leaderboards.Dtos;

public sealed class LeaderboardBoardEntryDto : IMapFrom<LeaderboardBoardEntry>
{
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public long PrimaryValue { get; set; }
    public long? SecondaryValue { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<LeaderboardBoardEntry, LeaderboardBoardEntryDto>();
}
