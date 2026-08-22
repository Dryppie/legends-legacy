using System.Security.Cryptography;
using System.Text;
using Domain.Models.RegionBosses;

namespace Services.LL.RegionBosses;

public sealed record RegionBossMatchedParty(int PartyNumber, int MatchmakingBand, IReadOnlyList<RegionBossSignup> Members);

public static class RegionBossMatchmaker
{
    private const int BroadBandWidth = 5_000;

    public static IReadOnlyList<RegionBossMatchedParty> Match(Guid eventId, IReadOnlyList<RegionBossSignup> signups)
    {
        var ordered = signups
            .OrderBy(x => x.PowerRating / BroadBandWidth)
            .ThenBy(x => StableKey(eventId, x.CharacterId))
            .ToArray();
        if (ordered.Length == 0)
            return [];

        var partyCount = Math.Max(1, (int)Math.Ceiling(ordered.Length / (double)RegionBossRules.MaximumPartySize));
        var baseSize = ordered.Length / partyCount;
        var largerParties = ordered.Length % partyCount;
        var result = new List<RegionBossMatchedParty>(partyCount);
        var offset = 0;
        for (var index = 0; index < partyCount; index++)
        {
            var size = baseSize + (index < largerParties ? 1 : 0);
            var members = ordered.Skip(offset).Take(size).ToArray();
            offset += size;
            result.Add(new RegionBossMatchedParty(
                index + 1,
                members.Length == 0 ? 0 : (int)Math.Round(members.Average(x => x.PowerRating) / BroadBandWidth),
                members));
        }
        return result;
    }

    private static string StableKey(Guid eventId, Guid characterId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"region-boss-match-v1:{eventId:N}:{characterId:N}"));
        return Convert.ToHexString(bytes);
    }
}
