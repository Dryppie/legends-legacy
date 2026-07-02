using Application.Common.Interfaces;
using Domain.Models.Leaderboards;
using Domain.Models.Professions;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Leaderboards;
public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly IDbContext _context;

    public LeaderboardRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<Leaderboard> GetLeaderboardAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
        .Include(c => c.Professions)
        .ToListAsync(cancellationToken);

        var combatLeaderboard = characters
            .OrderByDescending(c => c.Level)
            .ThenByDescending(c => c.Experience)
            .Select((c, index) => new LeaderboardEntry
            {
                CharacterId = c.Id,
                CharacterName = c.Name,
                Level = c.Level,
                Experience = (int)c.Experience,
                Rank = index + 1,
            })
            .ToList();

        var combatTop50 = combatLeaderboard.Take(50).ToList();

        if (!combatTop50.Any(c => c.CharacterId == characterId))
        {
            var requesterEntry = combatLeaderboard.FirstOrDefault(c => c.CharacterId == characterId);
            if (requesterEntry != null)
                combatTop50.Add(requesterEntry);
        }

        var wealthLeaderboard = characters
            .OrderByDescending(c => c.Cinders)
            .Select((c, index) => new LeaderboardEntry
            {
                CharacterId = c.Id,
                CharacterName = c.Name,
                Level = (int)c.Cinders,
                Rank = index + 1,
            })
            .ToList();

        var wealthTop50 = wealthLeaderboard.Take(50).ToList();

        if (!wealthTop50.Any(c => c.CharacterId == characterId))
        {
            var requesterEntry = wealthLeaderboard.FirstOrDefault(c => c.CharacterId == characterId);
            if (requesterEntry != null)
                wealthTop50.Add(requesterEntry);
        }

        var professions = new[]
        {
            ProfessionType.Crafting,
            ProfessionType.Mining,
            ProfessionType.Woodcutting,
            ProfessionType.Fishing,
            ProfessionType.Skinning
        };
        var professionLeaderboards = new Dictionary<string, List<LeaderboardEntry>>();

        foreach (var profession in professions)
        {
            var professionLeaderboard = characters
                .Where(c => c.Professions.Any(p => p.ProfessionType == profession))
                .Select(c => new
                {
                    Character = c,
                    Profession = c.Professions.First(p => p.ProfessionType == profession)
                })
                .OrderByDescending(x => x.Profession.Level)
                .ThenByDescending(c => c.Profession.Experience)
                .Select((x, index) => new LeaderboardEntry
                {
                    CharacterId = x.Character.Id,
                    CharacterName = x.Character.Name,
                    Level = x.Profession.Level,
                    Experience = (int)x.Profession.Experience,
                    Rank = index + 1,
                })
                .ToList();

            var top50 = professionLeaderboard.Take(50).ToList();

            if (!top50.Any(c => c.CharacterId == characterId))
            {
                var requesterEntry = professionLeaderboard.FirstOrDefault(c => c.CharacterId == characterId);
                if (requesterEntry != null)
                    top50.Add(requesterEntry);
            }

            professionLeaderboards[profession.ToString()] = top50;
        }

        var totalLevelLeaderboard = characters
            .Select(c => new
            {
                Character = c,
                TotalLevel = c.Level + c.Professions.Sum(p => p.Level)
            })
            .OrderByDescending(x => x.TotalLevel)
            .Select((x, index) => new LeaderboardEntry
            {
                CharacterId = x.Character.Id,
                CharacterName = x.Character.Name,
                Level = x.TotalLevel,
                Rank = index + 1,
            })
            .ToList();

        var totalLevelTop50 = totalLevelLeaderboard.Take(50).ToList();
        if (!totalLevelTop50.Any(c => c.CharacterId == characterId))
        {
            var requesterEntry = totalLevelLeaderboard.FirstOrDefault(c => c.CharacterId == characterId);
            if (requesterEntry != null)
                totalLevelTop50.Add(requesterEntry);
        }

        return new Leaderboard
        {
            Combat = combatTop50,
            Wealth = wealthTop50,
            Professions = professionLeaderboards,
            TotalLevel = totalLevelTop50
        };
    }
}
