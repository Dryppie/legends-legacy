using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Leaderboards;

namespace Application.UseCases.Leaderboards.Dtos;
public class LeaderboardEntryDto : IMapFrom<LeaderboardEntry>
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<LeaderboardEntry, LeaderboardEntryDto>();
    }
}
