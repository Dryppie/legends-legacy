using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Guilds;

public class GuildBuildingService : IGuildBuildingService
{
    private readonly IDbContext _context;
    private readonly IReadOnlyList<GuildBuildingDefinition> _definitions;
    private readonly IReadOnlyDictionary<GuildBuildingType, GuildBuildingDefinition> _definitionMap;

    public GuildBuildingService(IDbContext context)
        : this(context, new DefaultGuildContentProvider())
    {
    }

    public GuildBuildingService(IDbContext context, IGuildContentProvider content)
    {
        _context = context;
        _definitions = content.Buildings;
        _definitionMap = _definitions.ToDictionary(x => x.Type);
    }

    public async Task<GuildBuildingOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await LoadGuildAsync(characterId, cancellationToken);
        if (guild is null) return null;

        EnsureCurrentState(guild, null, now);
        if (_context.HasChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return BuildOverview(guild, characterId);
    }

    public async Task<GuildOperationResult<GuildBuildingOverviewDto>> ConstructAsync(
        Guid characterId,
        GuildBuildingType buildingType,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var guild = await LoadGuildAsync(characterId, cancellationToken);
        if (guild is null) return GuildOperationResult<GuildBuildingOverviewDto>.Fail("You are not in a guild.");

        EnsureCurrentState(guild, characterId, now);

        if (!CanManageBuildings(guild, characterId))
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("Only guild leaders and officers can spend Guild Supplies.");

        if (!_definitionMap.TryGetValue(buildingType, out var definition))
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("Guild building was not found.");

        if (definition.IsPermanent)
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("That building already exists.");

        if (guild.Buildings.Any(x => x.Type == buildingType))
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("That building already exists.");

        var guildHallLevel = GetGuildHallLevel(guild);
        if (guildHallLevel < definition.RequiredGuildHallLevel)
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail($"Requires Guild Hall level {definition.RequiredGuildHallLevel}.");

        var cost = GetCost(guild, definition, 1);
        if (!TrySpendGuildSupplies(guild, cost[GuildResourceType.GuildSupplies]))
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("Not enough Guild Supplies.");

        var building = new GuildBuilding
        {
            GuildId = guild.Id,
            Type = definition.Type,
            Level = 0,
            TargetLevel = 1,
            Status = GuildBuildingStatus.UnderConstruction,
            StartedAt = now,
            CompletesAt = now.AddHours(GetBuildHours(guild, definition, 1)),
            UpdatedAt = now
        };
        AddBuilding(guild, building);

        AddActivityLog(
            guild,
            GuildActivityLogType.BuildingConstructionStarted,
            characterId,
            $"{definition.Name} construction started.",
            now);

        return GuildOperationResult<GuildBuildingOverviewDto>.Success(BuildOverview(guild, characterId));
    }

    public async Task<GuildOperationResult<GuildBuildingOverviewDto>> UpgradeAsync(
        Guid characterId,
        Guid buildingId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var guild = await LoadGuildAsync(characterId, cancellationToken);
        if (guild is null) return GuildOperationResult<GuildBuildingOverviewDto>.Fail("You are not in a guild.");

        EnsureCurrentState(guild, characterId, now);

        if (!CanManageBuildings(guild, characterId))
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("Only guild leaders and officers can spend Guild Supplies.");

        var building = guild.Buildings.FirstOrDefault(x => x.Id == buildingId);
        if (building is null)
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("Guild building was not found.");

        var definition = _definitionMap[building.Type];
        if (building.Status != GuildBuildingStatus.Active)
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("That building is already being worked on.");

        if (building.Level >= definition.MaxLevel)
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("That building is already at max level.");

        var nextLevel = building.Level + 1;
        var cost = GetCost(guild, definition, nextLevel);
        if (!TrySpendGuildSupplies(guild, cost[GuildResourceType.GuildSupplies]))
            return GuildOperationResult<GuildBuildingOverviewDto>.Fail("Not enough Guild Supplies.");

        building.TargetLevel = nextLevel;
        building.Status = GuildBuildingStatus.Upgrading;
        building.StartedAt = now;
        building.CompletesAt = now.AddHours(GetBuildHours(guild, definition, nextLevel));
        building.CompletedAt = null;
        building.UpdatedAt = now;

        AddActivityLog(
            guild,
            GuildActivityLogType.BuildingUpgradeStarted,
            characterId,
            $"{definition.Name} upgrade to level {nextLevel} started.",
            now);

        return GuildOperationResult<GuildBuildingOverviewDto>.Success(BuildOverview(guild, characterId));
    }

    private async Task<Guild?> LoadGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(x => x.Members)
            .Include(x => x.Resources)
            .Include(x => x.Buildings)
            .Include(x => x.ActivityLogs)
            .FirstOrDefaultAsync(x => x.Members.Select(m => m.CharacterId).Contains(characterId), cancellationToken);

    private void EnsureCurrentState(Guild guild, Guid? characterId, DateTimeOffset now)
    {
        var hall = guild.Buildings.FirstOrDefault(x => x.Type == GuildBuildingType.GuildHall);
        if (hall is null)
        {
            AddBuilding(guild, new GuildBuilding
            {
                GuildId = guild.Id,
                Type = GuildBuildingType.GuildHall,
                Level = 1,
                Status = GuildBuildingStatus.Active,
                StartedAt = guild.CreatedAt,
                CompletedAt = guild.CreatedAt,
                UpdatedAt = now
            });
        }

        foreach (var building in guild.Buildings.Where(x => x.Status != GuildBuildingStatus.Active && x.CompletesAt <= now))
        {
            if (!_definitionMap.TryGetValue(building.Type, out var definition))
            {
                continue;
            }

            var completedLevel = building.TargetLevel ?? Math.Max(1, building.Level);
            var completedType = building.Status == GuildBuildingStatus.UnderConstruction
                ? GuildActivityLogType.BuildingConstructed
                : GuildActivityLogType.BuildingUpgraded;

            building.Level = completedLevel;
            building.TargetLevel = null;
            building.Status = GuildBuildingStatus.Active;
            building.CompletedAt = building.CompletesAt;
            building.CompletesAt = null;
            building.UpdatedAt = now;

            AddActivityLog(
                guild,
                completedType,
                characterId,
                $"{definition.Name} reached level {completedLevel}.",
                now);
        }
    }

    private GuildBuildingOverviewDto BuildOverview(Guild guild, Guid characterId)
    {
        var guildHallLevel = GetGuildHallLevel(guild);
        var canManage = CanManageBuildings(guild, characterId);

        return new GuildBuildingOverviewDto(
            guild.Id,
            guildHallLevel,
            guild.Resources.FirstOrDefault(x => x.Resource == GuildResourceType.GuildSupplies)?.Amount ?? 0,
            canManage,
            _definitions.Select(definition => ToBuildingDto(guild, definition, canManage)).ToList(),
            guild.ActivityLogs
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => new GuildActivityLogDto(x.Type, x.CharacterId, x.Message, x.CreatedAt))
                .ToList());
    }

    private static GuildBuildingDto ToBuildingDto(Guild guild, GuildBuildingDefinition definition, bool canManage)
    {
        var building = guild.Buildings.FirstOrDefault(x => x.Type == definition.Type);
        var guildHallLevel = GetGuildHallLevel(guild);
        var status = building?.Status ?? GuildBuildingStatus.Active;
        var level = building?.Level ?? 0;
        var isConstructed = building is not null;
        var lockedReason = GetLockedReason(guild, definition, building, canManage);
        var nextLevel = isConstructed ? level + 1 : 1;
        var nextCost = nextLevel <= definition.MaxLevel ? GetCost(guild, definition, nextLevel) : null;

        return new GuildBuildingDto(
            building?.Id,
            ToDefinitionDto(definition),
            definition.IsPermanent && building is null ? 1 : level,
            building?.TargetLevel,
            status,
            building?.CompletesAt,
            nextCost,
            !definition.IsPermanent && !isConstructed && lockedReason is null && canManage,
            isConstructed && status == GuildBuildingStatus.Active && level < definition.MaxLevel && lockedReason is null && canManage,
            lockedReason);

        static string? GetLockedReason(Guild guild, GuildBuildingDefinition definition, GuildBuilding? building, bool canManage)
        {
            if (!canManage) return "Leader or officer required.";
            if (building?.Status is GuildBuildingStatus.UnderConstruction) return "Construction is in progress.";
            if (building?.Status is GuildBuildingStatus.Upgrading) return "Upgrade is in progress.";
            if (building?.Level >= definition.MaxLevel) return "Max level reached.";

            var guildHallLevel = GetGuildHallLevel(guild);
            if (guildHallLevel < definition.RequiredGuildHallLevel)
                return $"Requires Guild Hall level {definition.RequiredGuildHallLevel}.";

            var nextLevel = building is null ? 1 : building.Level + 1;
            var cost = GetCost(guild, definition, nextLevel);
            var availableSupplies = guild.Resources.FirstOrDefault(x => x.Resource == GuildResourceType.GuildSupplies)?.Amount ?? 0;
            return availableSupplies < cost[GuildResourceType.GuildSupplies]
                ? "Not enough Guild Supplies."
                : null;
        }
    }

    private static GuildBuildingDefinitionDto ToDefinitionDto(GuildBuildingDefinition definition) =>
        new(
            definition.Type,
            definition.Name,
            definition.Description,
            definition.MaxLevel,
            definition.IsPermanent,
            definition.RequiredGuildHallLevel,
            definition.UnlockSummary,
            definition.Benefits);

    private static bool CanManageBuildings(Guild guild, Guid characterId)
    {
        var member = guild.Members.FirstOrDefault(x => x.CharacterId == characterId);
        return member is not null && (member.IsGuildLeader() || member.Role == GuildRole.Officer);
    }

    private static int GetGuildHallLevel(Guild guild) =>
        Math.Max(1, guild.Buildings.FirstOrDefault(x => x.Type == GuildBuildingType.GuildHall)?.Level ?? 1);

    private static IReadOnlyDictionary<GuildResourceType, int> GetCost(Guild guild, GuildBuildingDefinition definition, int level)
    {
        var baseCost = definition.BaseCost + definition.UpgradeCostStep * Math.Max(0, level - 1);
        var discountPercent = GetTreasuryLevel(guild) * 2;
        var discountedCost = Math.Max(1, (int)Math.Ceiling(baseCost * (100 - discountPercent) / 100d));

        return new Dictionary<GuildResourceType, int>
        {
            [GuildResourceType.GuildSupplies] = discountedCost
        };
    }

    private static double GetBuildHours(Guild guild, GuildBuildingDefinition definition, int level)
    {
        var baseHours = definition.BaseHours * Math.Max(1, level);
        var discountPercent = GetTreasuryLevel(guild) * 2;
        return Math.Max(0.25d, baseHours * (100 - discountPercent) / 100d);
    }

    private static int GetTreasuryLevel(Guild guild) =>
        Math.Clamp(
            guild.Buildings.FirstOrDefault(x => x.Type == GuildBuildingType.Treasury && x.Status == GuildBuildingStatus.Active)?.Level ?? 0,
            0,
            5);

    private static bool TrySpendGuildSupplies(Guild guild, int amount)
    {
        var resource = guild.Resources.FirstOrDefault(x => x.Resource == GuildResourceType.GuildSupplies);
        if (resource is null || resource.Amount < amount) return false;

        resource.Amount -= amount;
        return true;
    }

    private void AddBuilding(Guild guild, GuildBuilding building)
    {
        _context.GuildBuildings.Add(building);
        if (!guild.Buildings.Contains(building))
        {
            guild.Buildings.Add(building);
        }
    }

    private void AddActivityLog(
        Guild guild,
        GuildActivityLogType type,
        Guid? characterId,
        string message,
        DateTimeOffset now)
    {
        var activityLog = new GuildActivityLog
        {
            GuildId = guild.Id,
            Type = type,
            CharacterId = characterId,
            Message = message,
            CreatedAt = now
        };

        _context.GuildActivityLogs.Add(activityLog);
        if (!guild.ActivityLogs.Contains(activityLog))
        {
            guild.ActivityLogs.Add(activityLog);
        }
    }
}
