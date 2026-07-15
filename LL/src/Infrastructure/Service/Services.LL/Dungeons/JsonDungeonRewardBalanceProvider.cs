using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonRewardBalanceProvider : IDungeonRewardBalanceProvider
{
    private readonly DungeonRewardSettings _settings;

    public JsonDungeonRewardBalanceProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "progression", "dungeon-rewards.json");
        var document = JsonSerializer.Deserialize<DungeonRewardDocument>(File.ReadAllText(path), options)
                       ?? new DungeonRewardDocument();
        _settings = document.DungeonRewards;
        Validate(_settings);
    }

    public DungeonEncounterReward GetEncounterReward(int dungeonTier, RoomType roomType)
    {
        if (dungeonTier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dungeonTier), "Dungeon tier must be greater than zero.");
        }

        var tierMultiplier = Pow(_settings.TierMultiplier, dungeonTier - 1);
        var roomMultiplier = GetRoomMultiplier(roomType);
        return new DungeonEncounterReward(
            Scale(_settings.BaseExperiencePerEncounter, tierMultiplier, roomMultiplier),
            Scale(_settings.BaseCindersPerEncounter, tierMultiplier, roomMultiplier));
    }

    private decimal GetRoomMultiplier(RoomType roomType)
    {
        var key = roomType.ToString();
        return _settings.RoomMultipliers.TryGetValue(key, out var multiplier)
            ? multiplier
            : _settings.RoomMultipliers[RoomType.Unknown.ToString()];
    }

    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;
        for (var index = 0; index < exponent; index++)
        {
            result = checked(result * value);
        }

        return result;
    }

    private static int Scale(int baseValue, decimal tierMultiplier, decimal roomMultiplier)
    {
        var value = checked(baseValue * tierMultiplier * roomMultiplier);
        if (value > int.MaxValue)
        {
            throw new OverflowException("Dungeon encounter reward exceeds the supported range.");
        }

        return decimal.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero));
    }

    private static void Validate(DungeonRewardSettings settings)
    {
        if (settings.BaseExperiencePerEncounter <= 0 ||
            settings.BaseCindersPerEncounter <= 0 ||
            settings.TierMultiplier < 1 ||
            !settings.RoomMultipliers.TryGetValue(RoomType.Unknown.ToString(), out var fallback) ||
            fallback <= 0 ||
            settings.RoomMultipliers.Any(x => x.Value <= 0))
        {
            throw new InvalidOperationException("Dungeon reward progression settings are invalid.");
        }
    }

    private sealed class DungeonRewardDocument
    {
        public DungeonRewardSettings DungeonRewards { get; set; } = new();
    }

    private sealed class DungeonRewardSettings
    {
        public int BaseExperiencePerEncounter { get; set; }
        public int BaseCindersPerEncounter { get; set; }
        public decimal TierMultiplier { get; set; }
        public Dictionary<string, decimal> RoomMultipliers { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
