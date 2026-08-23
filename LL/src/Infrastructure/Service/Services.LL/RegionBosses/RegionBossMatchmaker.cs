using System.Security.Cryptography;
using System.Text;
using Domain.Models.RegionBosses;

namespace Services.LL.RegionBosses;

public sealed record RegionBossMatchedParty(int PartyNumber, int MatchmakingBand, IReadOnlyList<RegionBossSignup> Members);

public static class RegionBossMatchmaker
{
    private const double MaximumPowerRatioWithinBand = 1.75d;

    public static IReadOnlyList<RegionBossMatchedParty> Match(Guid eventId, IReadOnlyList<RegionBossSignup> signups)
    {
        var ordered = signups
            .OrderBy(x => x.PowerRating)
            .ThenBy(x => StableKey(eventId, x.CharacterId))
            .ToArray();
        if (ordered.Length == 0)
            return [];

        var bands = CreateBands(ordered);
        MergeUndersizedBands(bands);
        var result = new List<RegionBossMatchedParty>();
        for (var bandIndex = 0; bandIndex < bands.Count; bandIndex++)
        {
            var band = bands[bandIndex]
                .OrderBy(x => StableKey(eventId, x.CharacterId))
                .ToArray();
            var partyCount = Math.Max(
                1,
                (int)Math.Ceiling(band.Length / (double)RegionBossRules.MaximumPartySize));
            var baseSize = band.Length / partyCount;
            var largerParties = band.Length % partyCount;
            var offset = 0;
            for (var partyIndex = 0; partyIndex < partyCount; partyIndex++)
            {
                var size = baseSize + (partyIndex < largerParties ? 1 : 0);
                var members = band.Skip(offset).Take(size).ToArray();
                offset += size;
                result.Add(new RegionBossMatchedParty(
                    result.Count + 1,
                    bandIndex + 1,
                    members));
            }
        }
        return result;
    }

    private static List<List<RegionBossSignup>> CreateBands(IReadOnlyList<RegionBossSignup> ordered)
    {
        var bands = new List<List<RegionBossSignup>>();
        foreach (var signup in ordered)
        {
            var current = bands.LastOrDefault();
            if (current is null || ExceedsBand(current[0].PowerRating, signup.PowerRating))
            {
                current = [];
                bands.Add(current);
            }
            current.Add(signup);
        }
        return bands;
    }

    private static bool ExceedsBand(int minimumRating, int candidateRating)
    {
        if (minimumRating <= 0)
            return candidateRating > minimumRating;
        return candidateRating > minimumRating * MaximumPowerRatioWithinBand;
    }

    private static void MergeUndersizedBands(List<List<RegionBossSignup>> bands)
    {
        while (bands.Count > 1)
        {
            var index = bands.FindIndex(x => x.Count < RegionBossRules.RecommendedMinimumPartySize);
            if (index < 0)
                return;

            var mergeWith = index switch
            {
                0 => 1,
                _ when index == bands.Count - 1 => index - 1,
                _ => Distance(bands[index], bands[index - 1]) <= Distance(bands[index], bands[index + 1])
                    ? index - 1
                    : index + 1
            };
            bands[mergeWith].AddRange(bands[index]);
            bands[mergeWith].Sort((left, right) => left.PowerRating.CompareTo(right.PowerRating));
            bands.RemoveAt(index);
        }
    }

    private static double Distance(IReadOnlyCollection<RegionBossSignup> left, IReadOnlyCollection<RegionBossSignup> right) =>
        Math.Abs(left.Average(x => x.PowerRating) - right.Average(x => x.PowerRating));

    private static string StableKey(Guid eventId, Guid characterId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"region-boss-match-v1:{eventId:N}:{characterId:N}"));
        return Convert.ToHexString(bytes);
    }
}
