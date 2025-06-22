using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Leaderboards;

namespace Application.UseCases.Leaderboards.Dtos;
public class LeaderboardDto : IMapFrom<Leaderboard>
{
    public List<LeaderboardEntryDto> Combat { get; set; } = [];
    public List<LeaderboardEntryDto> Wealth { get; set; } = [];
    public List<LeaderboardEntry> TotalLevel { get; set; } = [];
    public Dictionary<string, List<LeaderboardEntryDto>> Professions { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Leaderboard, LeaderboardDto>();
    }
}
