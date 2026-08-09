using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL.Achievements;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Guilds;

public class GuildMissionService : IGuildMissionService
{
    private const int DailyOrdersPerMember = 3;
    private const int WeeklyMissionOptionCount = 3;

    private readonly IDbContext _context;
    private readonly IReadOnlyList<GuildMissionDefinition> _weeklyDefinitions;
    private readonly IReadOnlyList<GuildMissionDefinition> _dailyDefinitions;
    private readonly IReadOnlyDictionary<Guid, GuildMissionDefinition> _allDefinitions;
    private readonly IAchievementService? _achievementService;

    public GuildMissionService(IDbContext context)
        : this(context, new DefaultGuildContentProvider(), null)
    {
    }

    public GuildMissionService(IDbContext context, IGuildContentProvider content, IAchievementService? achievementService = null)
    {
        _context = context;
        _weeklyDefinitions = content.WeeklyMissions;
        _dailyDefinitions = content.DailyOrders;
        _allDefinitions = _weeklyDefinitions.Concat(_dailyDefinitions).ToDictionary(x => x.Id);
        _achievementService = achievementService;
    }

    public async Task<GuildMissionOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await LoadGuildForCharacterAsync(characterId, cancellationToken);
        if (guild is null) return null;

        await EnsureCurrentStateAsync(guild, characterId, now, cancellationToken);
        if (_context.HasChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await BuildOverviewAsync(guild.Id, characterId, now, cancellationToken);
    }

    public async Task<GuildOperationResult<GuildMissionOverviewDto>> SelectMissionAsync(Guid characterId, Guid missionOptionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await LoadGuildForCharacterAsync(characterId, cancellationToken);
        if (guild is null) return GuildOperationResult<GuildMissionOverviewDto>.Fail("You are not in a guild.");

        var member = guild.Members.FirstOrDefault(x => x.CharacterId == characterId);
        if (member is null || (!member.IsGuildLeader() && member.Role != GuildRole.Officer))
        {
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("Only guild leaders and officers can select a guild mission.");
        }

        await EnsureCurrentStateAsync(guild, characterId, now, cancellationToken);

        var week = GetWeek(now);
        var selectedExists = await _context.GuildMissionOptions
            .AnyAsync(x => x.GuildId == guild.Id && x.WeekKey == week.Key && x.IsSelected, cancellationToken);
        if (selectedExists)
        {
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("A weekly guild mission has already been selected.");
        }

        var option = await _context.GuildMissionOptions
            .FirstOrDefaultAsync(x => x.Id == missionOptionId && x.GuildId == guild.Id && x.WeekKey == week.Key, cancellationToken);
        if (option is null)
        {
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("Mission option was not found for this week.");
        }

        SelectOption(guild, option, characterId, now);
        await _context.SaveChangesAsync(cancellationToken);
        var overview = await BuildOverviewAsync(guild.Id, characterId, now, cancellationToken);
        return GuildOperationResult<GuildMissionOverviewDto>.Success(overview);
    }

    public async Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimPersonalOrderRewardAsync(Guid characterId, Guid orderId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await LoadGuildForCharacterAsync(characterId, cancellationToken);
        if (guild is null) return GuildOperationResult<GuildMissionOverviewDto>.Fail("You are not in a guild.");

        await EnsureCurrentStateAsync(guild, characterId, now, cancellationToken);

        var order = await _context.PersonalGuildOrders
            .FirstOrDefaultAsync(x => x.Id == orderId && x.GuildId == guild.Id && x.CharacterId == characterId, cancellationToken);
        if (order is null) return GuildOperationResult<GuildMissionOverviewDto>.Fail("Guild order was not found.");
        if (order.Status == PersonalGuildOrderStatus.RewardClaimed || order.RewardClaimedAt.HasValue)
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("Guild order reward has already been claimed.");
        if (order.Status != PersonalGuildOrderStatus.Completed)
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("Guild order is not complete.");

        var character = await _context.Characters.FirstOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null) return GuildOperationResult<GuildMissionOverviewDto>.Fail("Character was not found.");

        var orderReward = ApplyMissionBoardRewardBonus(guild, new WeeklyReward(50, 20, 10));

        character.GuildFavor += orderReward.Favor;
        AddGuildXp(guild, orderReward.GuildXp);
        AddGuildSupplies(guild, orderReward.Supplies);
        AddPeriodRewards(guild.Id, characterId, GuildMissionPeriodType.Daily, GetDailyKey(now), orderReward.Favor, orderReward.GuildXp, orderReward.Supplies, orderCompleted: true, now);
        AddPeriodRewards(guild.Id, characterId, GuildMissionPeriodType.Weekly, GetWeek(now).Key, orderReward.Favor, orderReward.GuildXp, orderReward.Supplies, orderCompleted: true, now);

        order.Status = PersonalGuildOrderStatus.RewardClaimed;
        order.RewardClaimedAt = now;
        AddActivityLog(
            guild,
            GuildActivityLogType.PersonalOrderRewardClaimed,
            characterId,
            $"Personal guild order reward claimed for {GetDefinitionName(order.MissionDefinitionId)}.",
            now);

        if (_achievementService is not null)
        {
            await _achievementService.RecordGuildProgressAsync(characterId, 0, false, orderReward.Supplies, cancellationToken);
        }

        var overview = await BuildOverviewAsync(guild.Id, characterId, now, cancellationToken);
        return GuildOperationResult<GuildMissionOverviewDto>.Success(overview);
    }

    public async Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimWeeklyRewardAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await LoadGuildForCharacterAsync(characterId, cancellationToken);
        if (guild is null) return GuildOperationResult<GuildMissionOverviewDto>.Fail("You are not in a guild.");

        await EnsureCurrentStateAsync(guild, characterId, now, cancellationToken);
        var week = GetWeek(now);

        var instance = await _context.GuildMissionInstances
            .Include(x => x.Contributions)
            .FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.WeekKey == week.Key, cancellationToken);
        if (instance is null || instance.Status is not (GuildMissionStatus.Completed or GuildMissionStatus.Active))
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("There is no weekly mission reward to claim.");
        if (instance.CurrentAmount < instance.TargetAmount)
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("The weekly guild mission is not complete.");

        CompleteMissionIfNeeded(instance, now);

        var contribution = instance.Contributions.FirstOrDefault(x => x.CharacterId == characterId);
        if (contribution is null || contribution.ContributionTier == GuildContributionTier.None)
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("You need at least Bronze contribution to claim this reward.");
        if (contribution.RewardClaimedAt.HasValue)
            return GuildOperationResult<GuildMissionOverviewDto>.Fail("Weekly guild mission reward has already been claimed.");

        var character = await _context.Characters.FirstOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null) return GuildOperationResult<GuildMissionOverviewDto>.Fail("Character was not found.");

        var rewards = ApplyMissionBoardRewardBonus(guild, GetWeeklyReward(contribution.ContributionTier));
        character.GuildFavor += rewards.Favor;
        AddGuildXp(guild, rewards.GuildXp);
        AddGuildSupplies(guild, rewards.Supplies);
        AddPeriodRewards(guild.Id, characterId, GuildMissionPeriodType.Weekly, week.Key, rewards.Favor, rewards.GuildXp, rewards.Supplies, orderCompleted: false, now);

        contribution.RewardClaimedAt = now;
        AddActivityLog(
            guild,
            GuildActivityLogType.WeeklyMissionRewardClaimed,
            characterId,
            $"Weekly guild mission reward claimed at {contribution.ContributionTier} tier.",
            now);

        if (_achievementService is not null)
        {
            await _achievementService.RecordGuildProgressAsync(characterId, 0, false, rewards.Supplies, cancellationToken);
        }

        var overview = await BuildOverviewAsync(guild.Id, characterId, now, cancellationToken);
        return GuildOperationResult<GuildMissionOverviewDto>.Success(overview);
    }

    public async Task<GuildContributionResult> RecordContributionAsync(GuildContributionEvent contributionEvent, CancellationToken cancellationToken)
    {
        if (contributionEvent.Amount <= 0)
        {
            return new GuildContributionResult(false, false, 0, 0);
        }

        if (!string.IsNullOrWhiteSpace(contributionEvent.IdempotencyKey))
        {
            var localAlreadyProcessed = _context.GuildContributionLedgers.Local
                .Any(x => x.IdempotencyKey == contributionEvent.IdempotencyKey);
            if (localAlreadyProcessed)
            {
                return new GuildContributionResult(true, true, 0, 0);
            }

            var alreadyProcessed = await _context.GuildContributionLedgers
                .AnyAsync(x => x.IdempotencyKey == contributionEvent.IdempotencyKey, cancellationToken);
            if (alreadyProcessed)
            {
                return new GuildContributionResult(true, true, 0, 0);
            }
        }

        var now = contributionEvent.OccurredAt ?? DateTimeOffset.UtcNow;
        var guild = await LoadGuildForCharacterAsync(contributionEvent.CharacterId, cancellationToken);
        if (guild is null) return new GuildContributionResult(false, false, 0, 0);

        await EnsureCurrentStateAsync(guild, contributionEvent.CharacterId, now, cancellationToken);

        _context.GuildContributionLedgers.Add(new GuildContributionLedger
        {
            GuildId = guild.Id,
            CharacterId = contributionEvent.CharacterId,
            Source = contributionEvent.Source,
            Metric = contributionEvent.Metric,
            Amount = contributionEvent.Amount,
            ContextId = contributionEvent.ContextId,
            IdempotencyKey = contributionEvent.IdempotencyKey,
            OccurredAt = now,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var completedOrders = await ProgressPersonalOrdersAsync(guild.Id, contributionEvent.CharacterId, contributionEvent.Metric, contributionEvent.Amount, now, cancellationToken);
        var weekly = await ProgressWeeklyMissionAsync(guild, contributionEvent.CharacterId, contributionEvent.Metric, contributionEvent.Amount, now, cancellationToken);

        AddContributionPeriod(guild.Id, contributionEvent.CharacterId, GuildMissionPeriodType.Daily, GetDailyKey(now), contributionEvent.Amount, weekly.Progress, now);
        AddContributionPeriod(guild.Id, contributionEvent.CharacterId, GuildMissionPeriodType.Weekly, GetWeek(now).Key, contributionEvent.Amount, weekly.Progress, now);

        if (_achievementService is not null && completedOrders > 0)
        {
            await _achievementService.RecordGuildProgressAsync(
                contributionEvent.CharacterId,
                completedOrders,
                false,
                0,
                cancellationToken);
        }

        if (_achievementService is not null && weekly.Completed)
        {
            foreach (var participantId in weekly.ParticipantIds)
            {
                await _achievementService.RecordGuildProgressAsync(participantId, 0, true, 0, cancellationToken);
            }
        }

        return new GuildContributionResult(true, false, weekly.Progress, completedOrders);
    }

    private async Task<Guild?> LoadGuildForCharacterAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Guilds
            .Include(x => x.Members)
            .Include(x => x.Resources)
            .Include(x => x.Buildings)
            .FirstOrDefaultAsync(x => x.Members.Select(m => m.CharacterId).Contains(characterId), cancellationToken);

    private async Task EnsureCurrentStateAsync(Guild guild, Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var week = GetWeek(now);
        var dailyKey = GetDailyKey(now);

        var expiredInstances = await _context.GuildMissionInstances
            .Where(x => x.GuildId == guild.Id && x.EndsAt <= now && (x.Status == GuildMissionStatus.Active || x.Status == GuildMissionStatus.PendingSelection))
            .ToListAsync(cancellationToken);
        foreach (var expired in expiredInstances)
        {
            expired.Status = expired.CurrentAmount >= expired.TargetAmount ? GuildMissionStatus.Completed : GuildMissionStatus.Expired;
            expired.CompletedAt ??= expired.Status == GuildMissionStatus.Completed ? expired.EndsAt : null;
        }

        var currentOptions = await _context.GuildMissionOptions
            .Where(x => x.GuildId == guild.Id && x.WeekKey == week.Key)
            .ToListAsync(cancellationToken);
        var obsoleteOptions = currentOptions
            .Where(x => !_allDefinitions.ContainsKey(x.MissionDefinitionId))
            .ToList();
        if (obsoleteOptions.Count > 0)
        {
            _context.GuildMissionOptions.RemoveRange(obsoleteOptions);
            currentOptions = currentOptions.Except(obsoleteOptions).ToList();
        }

        var currentInstance = await _context.GuildMissionInstances
            .FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.WeekKey == week.Key, cancellationToken);
        if (currentInstance is not null && !_allDefinitions.ContainsKey(currentInstance.MissionDefinitionId))
        {
            _context.GuildMissionInstances.Remove(currentInstance);
            currentInstance = null;
            _context.GuildMissionOptions.RemoveRange(currentOptions);
            currentOptions = [];
        }

        if (currentOptions.Count == 0)
        {
            currentOptions = GetWeeklyMissionOptions(guild, week.Key)
                .Select(def => new GuildMissionOption
                {
                    GuildId = guild.Id,
                    MissionDefinitionId = def.Id,
                    WeekKey = week.Key,
                    GeneratedAt = now,
                    ExpiresAt = week.EndsAt
                })
                .ToList();
            _context.GuildMissionOptions.AddRange(currentOptions);
        }

        if (currentInstance is { Status: GuildMissionStatus.Active }
            && _allDefinitions.TryGetValue(currentInstance.MissionDefinitionId, out var currentDefinition))
        {
            currentInstance.TargetAmount = Math.Max(1, currentDefinition.BaseTarget);
        }

        var selectedOption = currentOptions.FirstOrDefault(x => x.IsSelected);
        if (currentInstance is null && selectedOption is not null)
        {
            SelectOption(guild, selectedOption, selectedOption.SelectedByCharacterId, selectedOption.SelectedAt ?? now);
        }

        var currentOrders = await _context.PersonalGuildOrders
            .Where(x => x.GuildId == guild.Id && x.CharacterId == characterId && x.PeriodType == GuildMissionPeriodType.Daily && x.PeriodKey == dailyKey)
            .ToListAsync(cancellationToken);
        if (currentOrders.Count == 0)
        {
            var orders = GetDailyOrderDefinitions(guild, dailyKey).Select(def => new PersonalGuildOrder
            {
                GuildId = guild.Id,
                CharacterId = characterId,
                MissionDefinitionId = def.Id,
                PeriodType = GuildMissionPeriodType.Daily,
                PeriodKey = dailyKey,
                TargetAmount = def.BaseTarget,
                GeneratedAt = now
            });
            _context.PersonalGuildOrders.AddRange(orders);
        }
    }

    private void SelectOption(Guild guild, GuildMissionOption option, Guid? selectedByCharacterId, DateTimeOffset now)
    {
        if (!_allDefinitions.TryGetValue(option.MissionDefinitionId, out var definition))
        {
            return;
        }

        option.IsSelected = true;
        option.SelectedAt = now;
        option.SelectedByCharacterId = selectedByCharacterId;

        var week = GetWeek(now);
        _context.GuildMissionInstances.Add(new GuildMissionInstance
        {
            GuildId = guild.Id,
            MissionDefinitionId = definition.Id,
            WeekKey = week.Key,
            TargetAmount = Math.Max(1, definition.BaseTarget),
            CurrentAmount = 0,
            Status = GuildMissionStatus.Active,
            StartedAt = now,
            EndsAt = week.EndsAt,
            RewardClaimDeadline = week.EndsAt.AddDays(7)
        });
        AddActivityLog(
            guild,
            GuildActivityLogType.MissionSelected,
            selectedByCharacterId,
            $"{definition.Name} selected as this week's guild mission.",
            now);
    }

    private async Task<int> ProgressPersonalOrdersAsync(Guid guildId, Guid characterId, GuildContributionMetric metric, long amount, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var dailyKey = GetDailyKey(now);
        var orders = await _context.PersonalGuildOrders
            .Where(x => x.GuildId == guildId
                && x.CharacterId == characterId
                && x.PeriodType == GuildMissionPeriodType.Daily
                && x.PeriodKey == dailyKey
                && x.Status == PersonalGuildOrderStatus.Active)
            .ToListAsync(cancellationToken);

        var completed = 0;
        foreach (var order in orders)
        {
            if (!_allDefinitions.TryGetValue(order.MissionDefinitionId, out var definition) || definition.Metric != metric)
            {
                continue;
            }

            order.CurrentAmount = Math.Min(order.TargetAmount, order.CurrentAmount + amount);
            if (order.CurrentAmount >= order.TargetAmount)
            {
                order.Status = PersonalGuildOrderStatus.Completed;
                order.CompletedAt = now;
                completed++;
            }
        }

        return completed;
    }

    private async Task<(long Progress, bool Completed, IReadOnlyList<Guid> ParticipantIds)> ProgressWeeklyMissionAsync(Guild guild, Guid characterId, GuildContributionMetric metric, long amount, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var week = GetWeek(now);
        var instance = await _context.GuildMissionInstances
            .Include(x => x.Contributions)
            .FirstOrDefaultAsync(x =>
                x.GuildId == guild.Id
                && x.WeekKey == week.Key
                && (x.Status == GuildMissionStatus.Active || x.Status == GuildMissionStatus.Completed),
                cancellationToken);
        if (instance is null) return (0, false, []);
        if (!_allDefinitions.TryGetValue(instance.MissionDefinitionId, out var definition) || definition.Metric != metric) return (0, false, []);

        var wasCompleted = instance.Status == GuildMissionStatus.Completed;
        var progress = Math.Min(amount, Math.Max(0, instance.TargetAmount - instance.CurrentAmount));
        instance.CurrentAmount += progress;
        var contribution = instance.Contributions.FirstOrDefault(x => x.CharacterId == characterId);
        if (contribution is null)
        {
            contribution = new GuildMissionContribution
            {
                GuildMissionInstanceId = instance.Id,
                GuildId = guild.Id,
                CharacterId = characterId
            };
            _context.GuildMissionContributions.Add(contribution);
            instance.Contributions.Add(contribution);
        }

        contribution.Amount += amount;
        contribution.LastContributedAt = now;
        contribution.ContributionTier = CalculateTier(contribution.Amount, instance.TargetAmount);

        CompleteMissionIfNeeded(instance, now);
        var newlyCompleted = !wasCompleted && instance.Status == GuildMissionStatus.Completed;
        return (
            progress,
            newlyCompleted,
            newlyCompleted
                ? instance.Contributions.Where(x => x.Amount > 0).Select(x => x.CharacterId).Distinct().ToList()
                : []);
    }

    private void AddContributionPeriod(Guid guildId, Guid characterId, GuildMissionPeriodType type, string key, long score, long weeklyProgress, DateTimeOffset now)
    {
        var period = _context.GuildMemberContributionPeriods.Local
            .FirstOrDefault(x => x.GuildId == guildId && x.CharacterId == characterId && x.PeriodType == type && x.PeriodKey == key);
        period ??= _context.GuildMemberContributionPeriods
            .FirstOrDefault(x => x.GuildId == guildId && x.CharacterId == characterId && x.PeriodType == type && x.PeriodKey == key);

        if (period is null)
        {
            period = new GuildMemberContributionPeriod
            {
                GuildId = guildId,
                CharacterId = characterId,
                PeriodType = type,
                PeriodKey = key
            };
            _context.GuildMemberContributionPeriods.Add(period);
        }

        period.ContributionScore += score;
        period.WeeklyMissionContribution += weeklyProgress;
        period.LastContributedAt = now;
    }

    private void AddPeriodRewards(Guid guildId, Guid characterId, GuildMissionPeriodType type, string key, long favor, long xp, long supplies, bool orderCompleted, DateTimeOffset now)
    {
        var period = _context.GuildMemberContributionPeriods.Local
            .FirstOrDefault(x => x.GuildId == guildId && x.CharacterId == characterId && x.PeriodType == type && x.PeriodKey == key);
        period ??= _context.GuildMemberContributionPeriods
            .FirstOrDefault(x => x.GuildId == guildId && x.CharacterId == characterId && x.PeriodType == type && x.PeriodKey == key);

        if (period is null)
        {
            period = new GuildMemberContributionPeriod
            {
                GuildId = guildId,
                CharacterId = characterId,
                PeriodType = type,
                PeriodKey = key
            };
            _context.GuildMemberContributionPeriods.Add(period);
        }

        period.GuildFavorEarned += favor;
        period.GuildXpGenerated += xp;
        period.GuildSuppliesGenerated += supplies;
        period.OrdersCompleted += orderCompleted ? 1 : 0;
        period.LastContributedAt = now;
    }

    private async Task<GuildMissionOverviewDto> BuildOverviewAsync(Guid guildId, Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(x => x.Members)
            .FirstAsync(x => x.Id == guildId, cancellationToken);
        var week = GetWeek(now);
        var dailyKey = GetDailyKey(now);

        var options = await _context.GuildMissionOptions
            .Where(x => x.GuildId == guildId && x.WeekKey == week.Key)
            .OrderBy(x => x.GeneratedAt)
            .ToListAsync(cancellationToken);
        var activeMission = await _context.GuildMissionInstances
            .Include(x => x.Contributions)
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.WeekKey == week.Key, cancellationToken);
        if (activeMission is not null)
        {
            CompleteMissionIfNeeded(activeMission, now);
            foreach (var contribution in activeMission.Contributions)
            {
                contribution.ContributionTier = CalculateTier(contribution.Amount, activeMission.TargetAmount);
            }
        }

        var personalOrders = await _context.PersonalGuildOrders
            .Where(x => x.GuildId == guildId && x.CharacterId == characterId && x.PeriodType == GuildMissionPeriodType.Daily && x.PeriodKey == dailyKey)
            .OrderBy(x => x.GeneratedAt)
            .ToListAsync(cancellationToken);

        var dailyPeriod = await GetContributionPeriodAsync(guildId, characterId, GuildMissionPeriodType.Daily, dailyKey, cancellationToken);
        var weeklyPeriod = await GetContributionPeriodAsync(guildId, characterId, GuildMissionPeriodType.Weekly, week.Key, cancellationToken);
        var leaderboard = await GetContributionLeaderboardAsync(guildId, week.Key, cancellationToken);
        var member = guild.Members.FirstOrDefault(x => x.CharacterId == characterId);

        var myContribution = activeMission?.Contributions.FirstOrDefault(x => x.CharacterId == characterId);
        return new GuildMissionOverviewDto(
            guild.Id,
            guild.GuildXp,
            guild.GuildLevel,
            now.Date.AddDays(1),
            week.EndsAt,
            member is not null && (member.IsGuildLeader() || member.Role == GuildRole.Officer) && activeMission is null,
            options.Select(ToOptionDto).ToList(),
            activeMission is null ? null : ToInstanceDto(activeMission),
            myContribution is null
                ? new GuildMissionContributionDto(0, GuildContributionTier.None, null, false, false)
                : new GuildMissionContributionDto(
                    myContribution.Amount,
                    myContribution.ContributionTier,
                    myContribution.LastContributedAt,
                    myContribution.RewardClaimedAt.HasValue,
                    activeMission!.CurrentAmount >= activeMission.TargetAmount
                        && myContribution.ContributionTier != GuildContributionTier.None
                        && !myContribution.RewardClaimedAt.HasValue),
            personalOrders.Select(ToOrderDto).ToList(),
            new GuildContributionSummaryDto(
                dailyKey,
                week.Key,
                dailyPeriod?.ContributionScore ?? 0,
                weeklyPeriod?.ContributionScore ?? 0,
                weeklyPeriod?.GuildFavorEarned ?? 0,
                weeklyPeriod?.GuildXpGenerated ?? 0,
                weeklyPeriod?.GuildSuppliesGenerated ?? 0,
                weeklyPeriod?.OrdersCompleted ?? 0),
            leaderboard);
    }

    private async Task<GuildMemberContributionPeriod?> GetContributionPeriodAsync(Guid guildId, Guid characterId, GuildMissionPeriodType type, string key, CancellationToken cancellationToken) =>
        await _context.GuildMemberContributionPeriods
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.CharacterId == characterId && x.PeriodType == type && x.PeriodKey == key, cancellationToken);

    private async Task<IReadOnlyList<GuildContributionLeaderboardEntryDto>> GetContributionLeaderboardAsync(
        Guid guildId,
        string weekKey,
        CancellationToken cancellationToken) =>
        await _context.GuildMemberContributionPeriods
            .Where(x => x.GuildId == guildId
                && x.PeriodType == GuildMissionPeriodType.Weekly
                && x.PeriodKey == weekKey
                && (x.ContributionScore > 0 || x.WeeklyMissionContribution > 0 || x.OrdersCompleted > 0))
            .Join(
                _context.Characters,
                period => period.CharacterId,
                character => character.Id,
                (period, character) => new { period, character })
            .OrderByDescending(x => x.period.ContributionScore)
            .ThenByDescending(x => x.period.WeeklyMissionContribution)
            .ThenByDescending(x => x.period.OrdersCompleted)
            .Take(10)
            .Select(x => new GuildContributionLeaderboardEntryDto(
                x.period.CharacterId,
                x.character.Name,
                x.period.ContributionScore,
                x.period.WeeklyMissionContribution,
                x.period.GuildFavorEarned,
                x.period.GuildXpGenerated,
                x.period.GuildSuppliesGenerated,
                x.period.OrdersCompleted,
                x.period.LastContributedAt))
            .ToListAsync(cancellationToken);

    private static void CompleteMissionIfNeeded(GuildMissionInstance instance, DateTimeOffset now)
    {
        if (instance.Status == GuildMissionStatus.Active && instance.CurrentAmount >= instance.TargetAmount)
        {
            instance.Status = GuildMissionStatus.Completed;
            instance.CompletedAt ??= now;
        }
    }

    private static GuildContributionTier CalculateTier(long amount, long target)
    {
        if (amount <= 0) return GuildContributionTier.None;

        var ratio = amount / (double)Math.Max(1, target);
        return ratio switch
        {
            >= 0.1d => GuildContributionTier.Platinum,
            >= 0.075d => GuildContributionTier.Gold,
            >= 0.05d => GuildContributionTier.Silver,
            >= 0.025d => GuildContributionTier.Bronze,
            _ => GuildContributionTier.None
        };
    }

    private static WeeklyReward GetWeeklyReward(GuildContributionTier tier) => tier switch
    {
        GuildContributionTier.Platinum => new WeeklyReward(225, 650, 130),
        GuildContributionTier.Gold => new WeeklyReward(175, 500, 100),
        GuildContributionTier.Silver => new WeeklyReward(100, 250, 50),
        GuildContributionTier.Bronze => new WeeklyReward(50, 100, 20),
        _ => new WeeklyReward(0, 0, 0)
    };

    private IReadOnlyList<GuildMissionDefinition> GetWeeklyMissionOptions(Guild guild, string weekKey)
    {
        var missionBoardLevel = GetMissionBoardLevel(guild);
        if (missionBoardLevel < 2)
        {
            return _weeklyDefinitions.Take(WeeklyMissionOptionCount).ToList();
        }

        var count = WeeklyMissionOptionCount + 1;
        return GuildContentHelpers.PickWeeklyRotation(_weeklyDefinitions, weekKey, count, x => x.Key);
    }

    private IReadOnlyList<GuildMissionDefinition> GetDailyOrderDefinitions(Guild guild, string dailyKey)
    {
        var missionBoardLevel = GetMissionBoardLevel(guild);
        if (missionBoardLevel < 3)
        {
            return _dailyDefinitions.Take(DailyOrdersPerMember).ToList();
        }

        var count = DailyOrdersPerMember + 1;
        return GuildContentHelpers.PickWeeklyRotation(_dailyDefinitions, dailyKey, count, x => x.Key);
    }

    private static int GetMissionBoardLevel(Guild guild) =>
        guild.Buildings.FirstOrDefault(x => x.Type == GuildBuildingType.MissionBoard)?.Level ?? 0;

    private static WeeklyReward ApplyMissionBoardRewardBonus(Guild guild, WeeklyReward reward)
    {
        var multiplier = 1 + Math.Min(25, GetMissionBoardLevel(guild) * 5) / 100d;
        return new WeeklyReward(
            (long)Math.Ceiling(reward.Favor * multiplier),
            (long)Math.Ceiling(reward.GuildXp * multiplier),
            (int)Math.Ceiling(reward.Supplies * multiplier));
    }

    private static void AddGuildXp(Guild guild, long xp)
    {
        guild.GuildXp += xp;
        guild.GuildLevel = Math.Max(1, (int)(guild.GuildXp / 10000) + 1);
    }

    private static void AddGuildSupplies(Guild guild, int amount)
    {
        var resource = guild.Resources.FirstOrDefault(x => x.Resource == GuildResourceType.GuildSupplies);
        if (resource is null)
        {
            guild.Resources.Add(new GuildResource { GuildId = guild.Id, Resource = GuildResourceType.GuildSupplies, Amount = amount });
        }
        else
        {
            resource.Amount += amount;
        }
    }

    private GuildMissionOptionDto ToOptionDto(GuildMissionOption option) =>
        new(option.Id, ToDefinitionDto(_allDefinitions[option.MissionDefinitionId]), option.WeekKey, option.ExpiresAt, option.IsSelected);

    private GuildMissionInstanceDto ToInstanceDto(GuildMissionInstance instance) =>
        new(
            instance.Id,
            ToDefinitionDto(_allDefinitions[instance.MissionDefinitionId]),
            instance.WeekKey,
            instance.TargetAmount,
            instance.CurrentAmount,
            instance.Status,
            instance.StartedAt,
            instance.EndsAt,
            instance.RewardClaimDeadline);

    private PersonalGuildOrderDto ToOrderDto(PersonalGuildOrder order) =>
        new(
            order.Id,
            ToDefinitionDto(_allDefinitions[order.MissionDefinitionId]),
            order.PeriodKey,
            order.TargetAmount,
            order.CurrentAmount,
            order.Status,
            order.Status == PersonalGuildOrderStatus.Completed && !order.RewardClaimedAt.HasValue,
            order.GeneratedAt,
            order.CompletedAt);

    private static GuildMissionDefinitionDto ToDefinitionDto(GuildMissionDefinition definition) =>
        new(
            definition.Id,
            definition.Key,
            definition.Name,
            definition.Description,
            definition.Category,
            definition.Metric,
            definition.BaseTarget);

    private string GetDefinitionName(Guid definitionId) =>
        _allDefinitions.TryGetValue(definitionId, out var definition)
            ? definition.Name
            : "Unknown Order";

    private void AddActivityLog(
        Guild guild,
        GuildActivityLogType type,
        Guid? characterId,
        string message,
        DateTimeOffset now)
    {
        _context.GuildActivityLogs.Add(new GuildActivityLog
        {
            GuildId = guild.Id,
            Type = type,
            CharacterId = characterId,
            Message = message,
            CreatedAt = now
        });
    }

    private static string GetDailyKey(DateTimeOffset now) => now.UtcDateTime.ToString("yyyyMMdd");

    private static WeekPeriod GetWeek(DateTimeOffset now)
    {
        var utcDate = now.UtcDateTime.Date;
        var daysSinceMonday = ((int)utcDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var start = new DateTimeOffset(utcDate.AddDays(-daysSinceMonday), TimeSpan.Zero);
        var end = start.AddDays(7);
        return new WeekPeriod(start.ToString("yyyyMMdd"), start, end);
    }

    private sealed record WeekPeriod(string Key, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
    private sealed record WeeklyReward(long Favor, long GuildXp, int Supplies);
}
