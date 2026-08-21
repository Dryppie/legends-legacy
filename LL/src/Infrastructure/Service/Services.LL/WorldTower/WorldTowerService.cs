using System.Globalization;
using System.Diagnostics.Metrics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Combat;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.WorldTower;
using Application.UseCases.WorldTower.Dtos;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using AutoMapper;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.WorldTower;

public sealed class WorldTowerService : IWorldTowerService
{
    private const string EchoModeUnlockKey = "tower_echo_mode_unlock";
    private const string TowerExpeditionTargetUrlFormat = "/game/world/tower/expeditions/{0}";
    private const string TowerHallOfFameTargetUrl = "/game/world/tower/hall-of-fame";
    private static readonly TowerRallyStatus[] ActiveRallyStatuses =
    [
        TowerRallyStatus.Recruiting,
        TowerRallyStatus.Ready,
        TowerRallyStatus.InProgress
    ];
    private static readonly Meter TowerMeter = new("LegendsLegacy.WorldTower");
    private static readonly Histogram<double> EngineDurationMilliseconds =
        TowerMeter.CreateHistogram<double>("world_tower.combat.engine.duration", "ms");
    private static readonly Histogram<long> EngineAllocatedBytes =
        TowerMeter.CreateHistogram<long>("world_tower.combat.engine.allocated", "By");
    private static readonly Histogram<long> PlaybackBundleBytes =
        TowerMeter.CreateHistogram<long>("world_tower.playback.bundle.size", "By");

    private readonly IDbContext _db;
    private readonly IWorldTowerDefinitionProvider _definitions;
    private readonly ICharacterSnapshotService _snapshots;
    private readonly IPowerRatingService _powerRatings;
    private readonly IEntityService _entities;
    private readonly ICombatSetupService _combatSetup;
    private readonly ICombatEngineExecutor _combatEngine;
    private readonly ISnapshotCombatantBuilder _snapshotCombatants;
    private readonly ICreatureAbilityDefinitionProvider _creatureAbilities;
    private readonly IAbilityCatalogProvider _abilityCatalog;
    private readonly ICombatEncounterResultFactory _resultFactory;
    private readonly IGameEventOutbox _outbox;
    private readonly IGameRealtimeBroadcaster _realtime;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;
    private readonly WorldTowerOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IMemoryCache _timelineCache;
    private readonly ILogger<WorldTowerService> _logger;
    private readonly int _echoModeUnlockFloor;

    public WorldTowerService(
        IDbContext db,
        IWorldTowerDefinitionProvider definitions,
        ICharacterSnapshotService snapshots,
        IPowerRatingService powerRatings,
        IEntityService entities,
        ICombatSetupService combatSetup,
        ICombatEngineExecutor combatEngine,
        ISnapshotCombatantBuilder snapshotCombatants,
        ICreatureAbilityDefinitionProvider creatureAbilities,
        IAbilityCatalogProvider abilityCatalog,
        ICombatEncounterResultFactory resultFactory,
        IGameEventOutbox outbox,
        IGameRealtimeBroadcaster realtime,
        IMapper mapper,
        IOptions<WorldTowerOptions> options,
        JsonSerializerOptions jsonOptions,
        IMemoryCache timelineCache,
        TimeProvider timeProvider,
        ILogger<WorldTowerService> logger)
    {
        _db = db;
        _definitions = definitions;
        _snapshots = snapshots;
        _powerRatings = powerRatings;
        _entities = entities;
        _combatSetup = combatSetup;
        _combatEngine = combatEngine;
        _snapshotCombatants = snapshotCombatants;
        _creatureAbilities = creatureAbilities;
        _abilityCatalog = abilityCatalog;
        _resultFactory = resultFactory;
        _outbox = outbox;
        _realtime = realtime;
        _mapper = mapper;
        _options = options.Value;
        _jsonOptions = jsonOptions;
        _timelineCache = timelineCache;
        _timeProvider = timeProvider;
        _logger = logger;
        _echoModeUnlockFloor = _definitions.GetFloors()
            .Single(floor => floor.Unlocks.Any(unlock =>
                string.Equals(unlock.Key, EchoModeUnlockKey, StringComparison.OrdinalIgnoreCase)))
            .FloorNumber;
    }

    public async Task<TowerOverviewDto> GetOverviewAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        await EnsureFloorProgressAsync(cancellationToken);
        var releasedFloors = _definitions.GetFloors();
        var releasedFloorNumbers = releasedFloors.Select(x => x.FloorNumber).ToArray();
        var progress = await _db.TowerFloorProgresses
            .AsNoTracking()
            .Where(x => x.ServerId == _options.ServerId
                        && releasedFloorNumbers.Contains(x.FloorNumber))
            .ToDictionaryAsync(x => x.FloorNumber, cancellationToken);
        var rallies = await ActiveRalliesQuery()
            .AsNoTracking()
            .Include(x => x.Participants)
            .Include(x => x.Applications)
            .Where(x => releasedFloorNumbers.Contains(x.FloorNumber))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var summaries = releasedFloors
            .Select(floor => ToFloorSummary(
                floor,
                progress[floor.FloorNumber],
                rallies.Any(x => x.FloorNumber == floor.FloorNumber)))
            .ToArray();
        var hall = await GetHallOfFameAsync(cancellationToken);
        var current = summaries.FirstOrDefault(x => x.State is not (TowerFloorStateType.Locked or TowerFloorStateType.Cleared));
        var towerTokens = await _db.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => x.TowerTokens)
            .SingleAsync(cancellationToken);

        return new TowerOverviewDto(
            _options.ServerId,
            progress.Values.Where(x => x.UnlockedAt.HasValue).Select(x => x.FloorNumber).DefaultIfEmpty(0).Max(),
            progress.Values.Where(x => x.IsCleared).Select(x => x.FloorNumber).DefaultIfEmpty(0).Max(),
            IsEchoUnlocked(progress),
            towerTokens,
            current,
            summaries,
            rallies.Select(ToRallySummary).ToArray(),
            hall.Take(5).ToArray());
    }

    public async Task<TowerFloorDetailDto?> GetFloorAsync(
        Guid characterId,
        int floorNumber,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.GetFloor(floorNumber);
        if (definition is null)
            return null;

        await EnsureFloorProgressAsync(cancellationToken);
        var progress = await _db.TowerFloorProgresses
            .AsNoTracking()
            .Where(x => x.ServerId == _options.ServerId)
            .ToDictionaryAsync(x => x.FloorNumber, cancellationToken);
        var currentCharacterRallyId = await ActiveRalliesQuery()
            .AsNoTracking()
            .Where(x => x.Participants.Any(participant => participant.CharacterId == characterId))
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var rallies = await ActiveRalliesQuery()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Participants)
            .Include(x => x.Applications)
            .Where(x => x.FloorNumber == floorNumber)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return await ToFloorDetailAsync(
            characterId,
            definition,
            progress,
            rallies,
            currentCharacterRallyId,
            cancellationToken);
    }

    public async Task<TowerRallyDto?> GetRallyAsync(
        Guid characterId,
        Guid rallyId,
        CancellationToken cancellationToken)
    {
        var accountId = await _db.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        var rally = await _db.TowerRallies
            .AsNoTracking()
            .Include(x => x.Participants)
            .Include(x => x.Applications)
            .Include(x => x.Attempt)
                .ThenInclude(x => x!.Playback)
            .SingleOrDefaultAsync(x => x.Id == rallyId && x.ServerId == _options.ServerId, cancellationToken);
        return rally is null || _definitions.GetFloor(rally.FloorNumber) is null
            ? null
            : ToRallyDto(rally, characterId, accountId);
    }

    public async Task<TowerBattleReportDto?> GetAttemptReportAsync(
        Guid characterId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var json = await _db.TowerAttempts
            .AsNoTracking()
            .Where(x => x.Id == attemptId
                        && x.ServerId == _options.ServerId
                        && x.Status != TowerAttemptStatus.Started
                        && x.Status != TowerAttemptStatus.Playback
                        && x.TowerRally.Participants.Any(participant =>
                            participant.CharacterId == characterId))
            .Select(x => x.BattleReportJson)
            .SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<TowerBattleReportDto>(json, _jsonOptions);
    }

    public async Task<CombatResultDto?> GetAttemptCombatResultAsync(
        Guid characterId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var isPublicFirstClear = await _db.TowerFloorProgresses
            .AsNoTracking()
            .AnyAsync(x => x.ServerId == _options.ServerId
                           && x.FirstClearAttemptId == attemptId,
                cancellationToken);
        var json = await _db.TowerAttempts
            .AsNoTracking()
            .Where(x => x.Id == attemptId
                        && x.ServerId == _options.ServerId
                        && x.Status != TowerAttemptStatus.Started
                        && x.Status != TowerAttemptStatus.Playback
                        && (isPublicFirstClear
                            || x.TowerRally.Participants.Any(participant =>
                                participant.CharacterId == characterId)))
            .Select(x => x.CombatResultJson)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var result = JsonSerializer.Deserialize<CombatResult>(json, _jsonOptions);
        return result is null ? null : _mapper.Map<CombatResultDto>(result);
    }

    public async Task<TowerCombatPlaybackDto?> GetAttemptPlaybackAsync(
        Guid characterId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var playback = await _db.TowerCombatPlaybacks
            .AsNoTracking()
            .Include(x => x.TowerAttempt)
                .ThenInclude(x => x.TowerRally)
                    .ThenInclude(x => x.Participants)
            .SingleOrDefaultAsync(x => x.TowerAttemptId == attemptId
                && x.TowerAttempt.ServerId == _options.ServerId
                && (x.TowerAttempt.TowerRally.Status == TowerRallyStatus.InProgress
                    || x.TowerAttempt.TowerRally.Participants.Any(participant =>
                        participant.CharacterId == characterId)), cancellationToken);
        return playback is null
            ? null
            : ToPlaybackDto(playback, _timeProvider.GetUtcNow());
    }

    public async Task<TowerPlaybackBundleContentDto?> GetAttemptPlaybackBundleAsync(
        Guid characterId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var metadata = await _db.TowerCombatPlaybacks
            .AsNoTracking()
            .Where(x => x.TowerAttemptId == attemptId
                        && x.SchemaVersion == TowerCombatPlayback.CompactBundleSchemaVersion
                        && x.TowerAttempt.ServerId == _options.ServerId
                        && (x.TowerAttempt.TowerRally.Status == TowerRallyStatus.InProgress
                            || x.TowerAttempt.TowerRally.Participants.Any(participant =>
                                participant.CharacterId == characterId)))
            .Select(x => new
            {
                x.BundleContentType,
                x.BundleContentEncoding,
                x.BundleHash
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (metadata is null
            || string.IsNullOrWhiteSpace(metadata.BundleHash)
            || string.IsNullOrWhiteSpace(metadata.BundleContentType)
            || string.IsNullOrWhiteSpace(metadata.BundleContentEncoding))
            return null;

        var bytes = await _timelineCache.GetOrCreateAsync(
            $"world-tower:bundle:{attemptId}:{metadata.BundleHash}",
            async entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
                entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                return await _db.TowerCombatPlaybackArtifacts
                    .AsNoTracking()
                    .Where(x => x.TowerAttemptId == attemptId)
                    .Select(x => x.BundleBytes)
                    .SingleAsync(cancellationToken);
            });

        return new TowerPlaybackBundleContentDto(
            bytes ?? throw new InvalidOperationException("The Tower playback artifact is missing."),
            metadata.BundleContentType,
            metadata.BundleContentEncoding,
            metadata.BundleHash);
    }

    public async Task<TowerCombatFrameBatchDto?> GetAttemptPlaybackFramesAsync(
        Guid characterId,
        Guid attemptId,
        int afterSequence,
        CancellationToken cancellationToken)
    {
        var playback = await _db.TowerCombatPlaybacks
            .AsNoTracking()
            .Include(x => x.TowerAttempt)
                .ThenInclude(x => x.TowerRally)
                    .ThenInclude(x => x.Participants)
            .SingleOrDefaultAsync(x => x.TowerAttemptId == attemptId
                && x.TowerAttempt.ServerId == _options.ServerId
                && (x.TowerAttempt.TowerRally.Status == TowerRallyStatus.InProgress
                    || x.TowerAttempt.TowerRally.Participants.Any(participant =>
                        participant.CharacterId == characterId)), cancellationToken);
        if (playback is null)
            return null;

        if (playback.SchemaVersion == TowerCombatPlayback.CompactBundleSchemaVersion)
        {
            var manifest = ToPlaybackDto(playback, _timeProvider.GetUtcNow());
            return new TowerCombatFrameBatchDto(
                attemptId,
                afterSequence,
                manifest.CurrentSequence,
                false,
                []);
        }

        var allFrames = DeserializeFrames(playback);
        var completed = playback.TowerAttempt.Status is TowerAttemptStatus.Succeeded or TowerAttemptStatus.Failed;
        var current = GetCurrentFrame(playback, allFrames, _timeProvider.GetUtcNow(), completed);
        var frames = allFrames
            .Where(frame => frame.Sequence > afterSequence && frame.Sequence <= current.Sequence)
            .Take(_options.RecoveryFrameLimit)
            .ToArray();
        return new TowerCombatFrameBatchDto(
            attemptId,
            afterSequence,
            current.Sequence,
            frames.Length > 0 && frames[^1].Sequence < current.Sequence,
            frames);
    }

    public async Task<IReadOnlyList<TowerHallOfFameEntryDto>> GetHallOfFameAsync(
        CancellationToken cancellationToken)
    {
        var releasedFloors = _definitions.GetFloors();
        var releasedFloorNumbers = releasedFloors.Select(x => x.FloorNumber).ToArray();
        var releasedFloorsByNumber = releasedFloors.ToDictionary(x => x.FloorNumber);
        var firstClears = await _db.TowerFloorProgresses
            .AsNoTracking()
            .Where(x => x.ServerId == _options.ServerId
                        && releasedFloorNumbers.Contains(x.FloorNumber)
                        && x.FirstClearAttemptId.HasValue)
            .Select(x => new { x.FloorNumber, AttemptId = x.FirstClearAttemptId!.Value })
            .ToListAsync(cancellationToken);
        if (firstClears.Count == 0)
            return [];
        var firstClearFloorByAttemptId = firstClears.ToDictionary(x => x.AttemptId, x => x.FloorNumber);
        var firstClearIds = firstClearFloorByAttemptId.Keys.ToArray();

        var attempts = await _db.TowerAttempts
            .AsNoTracking()
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Participants)
            .Where(x => firstClearIds.Contains(x.Id)
                        && releasedFloorNumbers.Contains(x.FloorNumber))
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync(cancellationToken);
        var hallAccountIds = attempts
            .SelectMany(x => x.TowerRally.Participants)
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        var restrictedAccountIds = await ActiveSharedRestrictions()
            .Where(x => hallAccountIds.Contains(x.AccountId))
            .Select(x => x.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var restrictedAccounts = restrictedAccountIds.ToHashSet();

        return attempts
            .Where(attempt => firstClearFloorByAttemptId[attempt.Id] == attempt.FloorNumber)
            .Select(attempt =>
        {
            var floor = releasedFloorsByNumber[attempt.FloorNumber];
            return new TowerHallOfFameEntryDto(
                floor.FloorNumber,
                floor.Name,
                floor.GuardianName,
                attempt.Id,
                attempt.CompletedAt ?? attempt.StartedAt,
                attempt.AttemptNumberForFloor,
                attempt.FightDurationSeconds ?? 0,
                attempt.TowerRally.Participants
                    .Where(x => !restrictedAccounts.Contains(x.AccountId))
                    .OrderBy(x => x.JoinedAt)
                    .Select(x => new TowerHallOfFameParticipantDto(
                        x.CharacterId,
                        x.CharacterName,
                        x.GuildName,
                        x.PowerRating))
                    .ToArray());
        }).ToArray();
    }

    public async Task<IReadOnlyList<TowerPersonalExpeditionDto>> GetPersonalExpeditionsAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var releasedFloors = _definitions.GetFloors();
        var releasedFloorNumbers = releasedFloors.Select(x => x.FloorNumber).ToArray();
        var releasedFloorsByNumber = releasedFloors.ToDictionary(x => x.FloorNumber);
        var attempts = await _db.TowerAttempts
            .AsNoTracking()
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Participants)
            .Where(x => x.ServerId == _options.ServerId
                        && releasedFloorNumbers.Contains(x.FloorNumber)
                        && x.TowerRally.Participants.Any(participant =>
                            participant.CharacterId == characterId))
            .OrderByDescending(x => x.CompletedAt ?? x.StartedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return attempts.Select(attempt =>
        {
            var floor = releasedFloorsByNumber[attempt.FloorNumber];
            return new TowerPersonalExpeditionDto(
                attempt.TowerRallyId,
                attempt.Id,
                attempt.FloorNumber,
                floor.Name,
                floor.GuardianName,
                attempt.Mode,
                attempt.Status,
                attempt.AttemptNumberForFloor,
                attempt.StartedAt,
                attempt.CompletedAt,
                attempt.FightDurationSeconds,
                attempt.TowerRally.Participants
                    .OrderBy(x => x.JoinedAt)
                    .Select(x => new TowerHallOfFameParticipantDto(
                        x.CharacterId,
                        x.CharacterName,
                        x.GuildName,
                        x.PowerRating))
                    .ToArray());
        }).ToArray();
    }

    public async Task<TowerOperationResult<TowerRallyDto>> CreateRallyAsync(
        Guid characterId,
        int floorNumber,
        TowerRallyMode mode,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.GetFloor(floorNumber);
        if (definition is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower floor was not found.");

        await EnsureFloorProgressAsync(cancellationToken);
        await _db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber, cancellationToken);
        var validation = await ValidateRallyModeAsync(definition, mode, cancellationToken);
        if (validation is not null)
            return TowerOperationResult<TowerRallyDto>.Fail(validation);

        var eligibility = await GetJoinEligibilityAsync(characterId, definition, null, cancellationToken);
        if (eligibility.Error is not null)
            return TowerOperationResult<TowerRallyDto>.Fail(eligibility.Error);

        var now = DateTimeOffset.UtcNow;
        var rally = new TowerRally
        {
            ServerId = _options.ServerId,
            FloorNumber = floorNumber,
            Mode = mode,
            Status = definition.RequiredSlots == 1 ? TowerRallyStatus.Ready : TowerRallyStatus.Recruiting,
            CreatedByCharacterId = characterId,
            RequiredSlots = definition.RequiredSlots,
            CreatedAt = now
        };
        var leader = await CreateParticipantAsync(eligibility, rally, now, cancellationToken);
        leader.PartySlot = 1;
        rally.Participants.Add(leader);
        _db.TowerRallies.Add(rally);
        await EnqueueRallyUpdateAsync(rally, "Created", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, eligibility.AccountId));
    }

    public async Task<TowerOperationResult<TowerRallyDto>> ApplyToRallyAsync(
        Guid characterId,
        Guid rallyId,
        CancellationToken cancellationToken)
    {
        await _db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        var definition = _definitions.GetFloor(floorNumber.Value)!;
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);

        var rally = await _db.TowerRallies
            .Include(x => x.Participants)
            .Include(x => x.Applications)
            .Include(x => x.Attempt)
            .SingleOrDefaultAsync(x => x.Id == rallyId && x.ServerId == _options.ServerId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.Status is not (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready))
            return TowerOperationResult<TowerRallyDto>.Fail("This Expedition is no longer accepting applications.");
        if (rally.Participants.Count >= rally.RequiredSlots)
            return TowerOperationResult<TowerRallyDto>.Fail("This Expedition is already full.");

        var modeError = await ValidateRallyModeAsync(definition, rally.Mode, cancellationToken);
        if (modeError is not null)
            return TowerOperationResult<TowerRallyDto>.Fail(modeError);
        var eligibility = await GetJoinEligibilityAsync(characterId, definition, rally.Id, cancellationToken);
        if (eligibility.Error is not null)
            return TowerOperationResult<TowerRallyDto>.Fail(eligibility.Error);
        if (rally.Participants.Any(x => x.AccountId == eligibility.AccountId))
            return TowerOperationResult<TowerRallyDto>.Fail("This account already occupies a slot in the Expedition.");
        var existingApplication = rally.Applications.SingleOrDefault(x =>
            x.AccountId == eligibility.AccountId);
        if (existingApplication?.Status == TowerRallyApplicationStatus.Pending)
        {
            return TowerOperationResult<TowerRallyDto>.Fail("This account has already applied to this Expedition.");
        }
        if (existingApplication?.Status == TowerRallyApplicationStatus.Accepted
            && rally.Participants.Any(x => x.AccountId == eligibility.AccountId))
        {
            return TowerOperationResult<TowerRallyDto>.Fail("This account already occupies a slot in the Expedition.");
        }

        var snapshot = await _snapshots.CreateAsync(eligibility.CharacterId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var application = existingApplication ?? new TowerRallyApplication
        {
            TowerRally = rally
        };
        application.CharacterId = eligibility.CharacterId;
        application.AccountId = eligibility.AccountId;
        application.CharacterName = eligibility.CharacterName;
        application.GuildId = eligibility.GuildId;
        application.GuildName = eligibility.GuildName;
        application.PowerRating = eligibility.PowerRating;
        application.CharacterSnapshotId = snapshot.Id;
        application.CharacterSnapshot = snapshot;
        application.Status = TowerRallyApplicationStatus.Pending;
        application.AppliedAt = now;
        application.ResolvedAt = null;
        application.ResolvedByCharacterId = null;
        if (existingApplication is null)
        {
            rally.Applications.Add(application);
            _db.TowerRallyApplications.Add(application);
        }
        await EnqueueRallyUpdateAsync(rally, "ApplicationSubmitted", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, eligibility.AccountId));
    }

    public async Task<TowerOperationResult<TowerRallyDto>> AcceptRallyApplicationAsync(
        Guid characterId,
        Guid rallyId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        var applicantCharacterId = await _db.TowerRallyApplications
            .AsNoTracking()
            .Where(x => x.Id == applicationId && x.TowerRallyId == rallyId)
            .Select(x => (Guid?)x.CharacterId)
            .SingleOrDefaultAsync(cancellationToken);
        if (applicantCharacterId.HasValue)
            await _db.AcquireCharacterCommandLockAsync(applicantCharacterId.Value, cancellationToken);
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);
        var rally = await GetMutableRallyWithApplicationsAsync(rallyId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.CreatedByCharacterId != characterId)
            return TowerOperationResult<TowerRallyDto>.Fail("Only the Expedition leader can accept applications.");
        if (rally.Status != TowerRallyStatus.Recruiting)
            return TowerOperationResult<TowerRallyDto>.Fail("This Expedition is no longer accepting applications.");
        if (rally.Participants.Count >= rally.RequiredSlots)
            return TowerOperationResult<TowerRallyDto>.Fail("This Expedition is already full.");

        var application = rally.Applications.SingleOrDefault(x =>
            x.Id == applicationId && x.Status == TowerRallyApplicationStatus.Pending);
        if (application is null)
            return TowerOperationResult<TowerRallyDto>.Fail("The pending Expedition application was not found.");
        if (rally.Participants.Any(x => x.AccountId == application.AccountId))
            return TowerOperationResult<TowerRallyDto>.Fail("This account already occupies a slot in the Expedition.");
        var conflicting = await _db.TowerRallyParticipants
            .AsNoTracking()
            .AnyAsync(x => x.CharacterId == application.CharacterId
                           && x.TowerRallyId != rally.Id
                           && ActiveRallyStatuses.Contains(x.TowerRally.Status), cancellationToken);
        if (conflicting)
            return TowerOperationResult<TowerRallyDto>.Fail("This character is already locked into another active Tower Expedition.");

        var now = DateTimeOffset.UtcNow;
        application.Status = TowerRallyApplicationStatus.Accepted;
        application.ResolvedAt = now;
        application.ResolvedByCharacterId = characterId;
        var participant = new TowerRallyParticipant
        {
            TowerRally = rally,
            CharacterId = application.CharacterId,
            AccountId = application.AccountId,
            CharacterName = application.CharacterName,
            GuildId = application.GuildId,
            GuildName = application.GuildName,
            PowerRating = application.PowerRating,
            CharacterSnapshotId = application.CharacterSnapshotId,
            CharacterSnapshot = application.CharacterSnapshot,
            JoinedAt = now
        };
        rally.Participants.Add(participant);
        _db.TowerRallyParticipants.Add(participant);
        if (rally.Participants.Count == rally.RequiredSlots)
        {
            rally.Status = WorldTowerPartyRules.HasCompletePartyLayout(rally)
                ? TowerRallyStatus.Ready
                : TowerRallyStatus.Recruiting;
            foreach (var pending in rally.Applications.Where(x =>
                         x.Id != application.Id && x.Status == TowerRallyApplicationStatus.Pending))
            {
                pending.Status = TowerRallyApplicationStatus.Declined;
                pending.ResolvedAt = now;
                pending.ResolvedByCharacterId = characterId;
            }
        }
        await EnqueueRallyUpdateAsync(rally, "ApplicationAccepted", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var leaderAccountId = await GetAccountIdAsync(characterId, cancellationToken);
        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, leaderAccountId));
    }

    public async Task<TowerOperationResult<TowerRallyDto>> DeclineRallyApplicationAsync(
        Guid characterId,
        Guid rallyId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);
        var rally = await GetMutableRallyWithApplicationsAsync(rallyId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.CreatedByCharacterId != characterId)
            return TowerOperationResult<TowerRallyDto>.Fail("Only the Expedition leader can decline applications.");

        var application = rally.Applications.SingleOrDefault(x =>
            x.Id == applicationId && x.Status == TowerRallyApplicationStatus.Pending);
        if (application is null)
            return TowerOperationResult<TowerRallyDto>.Fail("The pending Expedition application was not found.");

        var now = DateTimeOffset.UtcNow;
        application.Status = TowerRallyApplicationStatus.Declined;
        application.ResolvedAt = now;
        application.ResolvedByCharacterId = characterId;
        await EnqueueRallyUpdateAsync(rally, "ApplicationDeclined", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var leaderAccountId = await GetAccountIdAsync(characterId, cancellationToken);
        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, leaderAccountId));
    }

    private async Task<TowerRally?> GetMutableRallyWithApplicationsAsync(
        Guid rallyId,
        CancellationToken cancellationToken)
    {
        var rally = await _db.TowerRallies
            .Include(x => x.Participants)
            .Include(x => x.Applications)
                .ThenInclude(x => x.CharacterSnapshot)
            .Include(x => x.Attempt)
            .SingleOrDefaultAsync(x => x.Id == rallyId && x.ServerId == _options.ServerId, cancellationToken);
        return rally is not null && _definitions.GetFloor(rally.FloorNumber) is not null
            ? rally
            : null;
    }

    private async Task<int?> GetRallyFloorNumberAsync(Guid rallyId, CancellationToken cancellationToken)
    {
        var floorNumber = await _db.TowerRallies
            .AsNoTracking()
            .Where(x => x.Id == rallyId && x.ServerId == _options.ServerId)
            .Select(x => (int?)x.FloorNumber)
            .SingleOrDefaultAsync(cancellationToken);
        return floorNumber.HasValue && _definitions.GetFloor(floorNumber.Value) is not null
            ? floorNumber
            : null;
    }

    private async Task<Guid?> GetAccountIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _db.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TowerOperationResult<TowerRallyDto>> LeaveRallyAsync(
        Guid characterId,
        Guid rallyId,
        CancellationToken cancellationToken)
    {
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);

        var rally = await _db.TowerRallies
            .Include(x => x.Participants)
            .Include(x => x.Applications)
            .Include(x => x.Attempt)
            .SingleOrDefaultAsync(x => x.Id == rallyId && x.ServerId == _options.ServerId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.Status is not (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready))
            return TowerOperationResult<TowerRallyDto>.Fail("Participants cannot leave a started Expedition.");

        var participant = rally.Participants.SingleOrDefault(x => x.CharacterId == characterId);
        var application = rally.Applications.SingleOrDefault(x =>
            x.CharacterId == characterId && x.Status == TowerRallyApplicationStatus.Pending);
        if (participant is null && application is null)
            return TowerOperationResult<TowerRallyDto>.Fail("You are not part of this Expedition and have no pending application.");

        var now = DateTimeOffset.UtcNow;
        string eventName;
        Guid? accountId;
        if (application is not null)
        {
            application.Status = TowerRallyApplicationStatus.Withdrawn;
            application.ResolvedAt = now;
            application.ResolvedByCharacterId = characterId;
            eventName = "ApplicationWithdrawn";
            accountId = application.AccountId;
        }
        else if (rally.CreatedByCharacterId == characterId)
        {
            rally.Status = TowerRallyStatus.Cancelled;
            rally.CancelledAt = now;
            foreach (var pending in rally.Applications.Where(x =>
                         x.Status == TowerRallyApplicationStatus.Pending))
            {
                pending.Status = TowerRallyApplicationStatus.Declined;
                pending.ResolvedAt = now;
                pending.ResolvedByCharacterId = characterId;
            }
            eventName = "Cancelled";
            accountId = participant!.AccountId;
        }
        else
        {
            var acceptedApplication = rally.Applications.SingleOrDefault(x =>
                x.AccountId == participant!.AccountId
                && x.Status == TowerRallyApplicationStatus.Accepted);
            if (acceptedApplication is not null)
            {
                acceptedApplication.Status = TowerRallyApplicationStatus.Withdrawn;
                acceptedApplication.ResolvedAt = now;
                acceptedApplication.ResolvedByCharacterId = characterId;
            }
            rally.Participants.Remove(participant!);
            _db.TowerRallyParticipants.Remove(participant!);
            rally.Status = TowerRallyStatus.Recruiting;
            eventName = "ParticipantLeft";
            accountId = participant!.AccountId;
        }
        await EnqueueRallyUpdateAsync(rally, eventName, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, accountId));
    }

    public async Task<TowerOperationResult<TowerRallyDto>> UpdateRallyLoadoutAsync(
        Guid characterId,
        Guid rallyId,
        CancellationToken cancellationToken)
    {
        await _db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);

        var rally = await GetMutableRallyWithApplicationsAsync(rallyId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.Status is not (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready))
            return TowerOperationResult<TowerRallyDto>.Fail("The locked build can only be updated before the Expedition starts.");

        var participant = rally.Participants.SingleOrDefault(x => x.CharacterId == characterId);
        var pendingApplication = rally.Applications.SingleOrDefault(x =>
            x.CharacterId == characterId && x.Status == TowerRallyApplicationStatus.Pending);
        if (participant is null && pendingApplication is null)
            return TowerOperationResult<TowerRallyDto>.Fail("You are not part of this Expedition and have no pending application.");

        var rating = await _powerRatings.GetCharacterRatingAsync(characterId, cancellationToken);
        if (rating.State != PowerAnalysisState.Available)
            return TowerOperationResult<TowerRallyDto>.Fail(
                rating.StatusMessage ?? "Power Rating is unavailable for this character.");

        var snapshot = await _snapshots.CreateAsync(characterId, cancellationToken);
        var powerRating = CombatRatingDisplay.FromRaw(rating.Overall);
        var now = DateTimeOffset.UtcNow;
        Guid? accountId;
        if (participant is not null)
        {
            participant.CharacterSnapshotId = snapshot.Id;
            participant.CharacterSnapshot = snapshot;
            participant.PowerRating = powerRating;
            accountId = participant.AccountId;

            var acceptedApplication = rally.Applications.SingleOrDefault(x =>
                x.CharacterId == characterId && x.Status == TowerRallyApplicationStatus.Accepted);
            if (acceptedApplication is not null)
            {
                acceptedApplication.CharacterSnapshotId = snapshot.Id;
                acceptedApplication.CharacterSnapshot = snapshot;
                acceptedApplication.PowerRating = powerRating;
            }
        }
        else
        {
            pendingApplication!.CharacterSnapshotId = snapshot.Id;
            pendingApplication.CharacterSnapshot = snapshot;
            pendingApplication.PowerRating = powerRating;
            pendingApplication.AppliedAt = now;
            accountId = pendingApplication.AccountId;
        }

        await EnqueueRallyUpdateAsync(rally, "LoadoutUpdated", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, accountId));
    }

    public async Task<TowerOperationResult<TowerRallyDto>> UpdateRallyPartiesAsync(
        Guid characterId,
        Guid rallyId,
        IReadOnlyList<TowerPartyAssignment> assignments,
        CancellationToken cancellationToken)
    {
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        await _db.AcquireWorldTowerFloorLockAsync(
            _options.ServerId,
            floorNumber.Value,
            cancellationToken);

        var rally = await GetMutableRallyWithApplicationsAsync(rallyId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.CreatedByCharacterId != characterId)
            return TowerOperationResult<TowerRallyDto>.Fail("Only the Expedition leader can arrange parties.");
        if (rally.Status is not (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready))
            return TowerOperationResult<TowerRallyDto>.Fail("Parties cannot be changed after the Expedition starts.");
        if (assignments.Count != rally.Participants.Count)
            return TowerOperationResult<TowerRallyDto>.Fail("The party layout must include every Expedition participant.");

        var participantIds = rally.Participants.Select(participant => participant.CharacterId).ToHashSet();
        var assignmentIds = assignments.Select(assignment => assignment.CharacterId).ToArray();
        if (assignmentIds.Distinct().Count() != assignmentIds.Length
            || assignmentIds.Any(character => !participantIds.Contains(character)))
        {
            return TowerOperationResult<TowerRallyDto>.Fail(
                "The party layout contains a duplicate or unknown participant.");
        }

        if (assignments.Any(assignment =>
                !WorldTowerPartyRules.IsValidSlot(assignment.PartySlot, rally.RequiredSlots)))
        {
            return TowerOperationResult<TowerRallyDto>.Fail(
                $"Party slots must be between 1 and {rally.RequiredSlots}, or empty for the bench.");
        }

        var occupiedSlots = assignments
            .Where(assignment => assignment.PartySlot.HasValue)
            .Select(assignment => assignment.PartySlot!.Value)
            .ToArray();
        if (occupiedSlots.Distinct().Count() != occupiedSlots.Length)
            return TowerOperationResult<TowerRallyDto>.Fail("Each party slot can hold only one participant.");

        var assignmentByCharacter = assignments.ToDictionary(assignment => assignment.CharacterId);
        foreach (var participant in rally.Participants)
            participant.PartySlot = assignmentByCharacter[participant.CharacterId].PartySlot;

        rally.Status = WorldTowerPartyRules.HasCompletePartyLayout(rally)
            ? TowerRallyStatus.Ready
            : TowerRallyStatus.Recruiting;
        var now = _timeProvider.GetUtcNow();
        await EnqueueRallyUpdateAsync(rally, "PartiesUpdated", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var accountId = await GetAccountIdAsync(characterId, cancellationToken);
        return TowerOperationResult<TowerRallyDto>.Success(
            ToRallyDto(rally, characterId, accountId));
    }

    public async Task<TowerOperationResult<TowerRallyDto>> TransferRallyLeadershipAsync(
        Guid characterId,
        Guid rallyId,
        Guid targetCharacterId,
        CancellationToken cancellationToken)
    {
        var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
        if (!floorNumber.HasValue)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);

        var rally = await GetMutableRallyWithApplicationsAsync(rallyId, cancellationToken);
        if (rally is null)
            return TowerOperationResult<TowerRallyDto>.Fail("Tower Expedition was not found.");
        if (rally.CreatedByCharacterId != characterId)
            return TowerOperationResult<TowerRallyDto>.Fail("Only the Expedition leader can transfer leadership.");
        if (rally.Status is not (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready))
            return TowerOperationResult<TowerRallyDto>.Fail("Leadership cannot be transferred after the Expedition has started.");
        if (targetCharacterId == characterId)
            return TowerOperationResult<TowerRallyDto>.Fail("You already lead this Expedition.");

        var target = rally.Participants.SingleOrDefault(x => x.CharacterId == targetCharacterId);
        if (target is null)
            return TowerOperationResult<TowerRallyDto>.Fail("The new leader must be a locked-in Expedition participant.");

        var now = DateTimeOffset.UtcNow;
        rally.CreatedByCharacterId = targetCharacterId;
        await EnqueueRallyUpdateAsync(rally, "LeadershipTransferred", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var accountId = await GetAccountIdAsync(characterId, cancellationToken);
        return TowerOperationResult<TowerRallyDto>.Success(ToRallyDto(rally, characterId, accountId));
    }

    public async Task<TowerOperationResult<TowerAttemptResultDto>> StartRallyAsync(
        Guid characterId,
        Guid rallyId,
        CancellationToken cancellationToken)
    {
        TowerFloorDefinition definition;

        await using (var transaction = await _db.BeginTransactionAsync(cancellationToken))
        {
            var floorNumber = await GetRallyFloorNumberAsync(rallyId, cancellationToken);
            if (!floorNumber.HasValue)
                return TowerOperationResult<TowerAttemptResultDto>.Fail("Tower Expedition was not found.");
            definition = _definitions.GetFloor(floorNumber.Value)!;

            await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber.Value, cancellationToken);
            var rally = await _db.TowerRallies
                .Include(x => x.Participants)
                .Include(x => x.Applications)
                .Include(x => x.Attempt)
                .SingleOrDefaultAsync(x => x.Id == rallyId && x.ServerId == _options.ServerId, cancellationToken);
            if (rally is null)
                return TowerOperationResult<TowerAttemptResultDto>.Fail("Tower Expedition was not found.");

            if (rally.CreatedByCharacterId != characterId)
                return TowerOperationResult<TowerAttemptResultDto>.Fail("Only the Expedition leader can start the attempt.");
            if (rally.Participants.Count != rally.RequiredSlots)
                return TowerOperationResult<TowerAttemptResultDto>.Fail("The Expedition must fill every slot before it can start.");
            if (!WorldTowerPartyRules.HasCompletePartyLayout(rally))
            {
                return TowerOperationResult<TowerAttemptResultDto>.Fail(
                    "Every Expedition participant must be assigned to a party before the attempt can start.");
            }
            if (rally.Status != TowerRallyStatus.Ready)
                return TowerOperationResult<TowerAttemptResultDto>.Fail("The Expedition is not ready to start.");
            var participantAccountIds = rally.Participants
                .Select(x => x.AccountId)
                .Distinct()
                .ToArray();
            if (await ActiveSharedRestrictions()
                    .AnyAsync(x => participantAccountIds.Contains(x.AccountId), cancellationToken))
                return TowerOperationResult<TowerAttemptResultDto>.Fail(
                    "An Expedition participant is no longer eligible for multiplayer activity.");
            if (rally.Attempt is not null)
                return TowerOperationResult<TowerAttemptResultDto>.Fail("This Expedition already has an attempt.");

            var modeError = await ValidateRallyModeAsync(definition, rally.Mode, cancellationToken);
            if (modeError is not null)
                return TowerOperationResult<TowerAttemptResultDto>.Fail(modeError);

            var anotherAttemptIsActive = await _db.TowerAttempts
                .AsNoTracking()
                .AnyAsync(x => x.ServerId == _options.ServerId
                               && x.FloorNumber == rally.FloorNumber
                               && x.TowerRallyId != rally.Id
                               && (x.Status == TowerAttemptStatus.Started
                                   || x.Status == TowerAttemptStatus.Playback),
                    cancellationToken);
            if (anotherAttemptIsActive)
            {
                return TowerOperationResult<TowerAttemptResultDto>.Fail(
                    "Another Expedition is already attempting this floor. Wait for that attempt to finish.");
            }

            var attemptNumber = await _db.TowerAttempts.CountAsync(
                x => x.ServerId == _options.ServerId && x.FloorNumber == rally.FloorNumber,
                cancellationToken) + 1;
            var attempt = new TowerAttempt
            {
                TowerRallyId = rally.Id,
                ServerId = _options.ServerId,
                FloorNumber = rally.FloorNumber,
                Mode = rally.Mode,
                Status = TowerAttemptStatus.Started,
                AttemptNumberForFloor = attemptNumber,
                StartedAt = _timeProvider.GetUtcNow()
            };
            rally.Status = TowerRallyStatus.InProgress;
            rally.StartedAt = attempt.StartedAt;
            rally.Attempt = attempt;
            _db.TowerAttempts.Add(attempt);
            await EnqueueRallyUpdateAsync(rally, "Started", attempt.StartedAt, cancellationToken);
            await EnqueueTowerBattleChatAnnouncementAsync(
                rally,
                definition,
                attempt.StartedAt,
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return TowerOperationResult<TowerAttemptResultDto>.Success(new TowerAttemptResultDto(
                attempt.Id,
                definition.FloorNumber,
                definition.GuardianName,
                TowerAttemptStatus.Started,
                null));
        }
    }

    public async Task<bool> SimulateQueuedAttemptAsync(
        Guid attemptId,
        string leaseOwner,
        CancellationToken cancellationToken)
    {
        var attempt = await _db.TowerAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == attemptId, cancellationToken);
        if (attempt is null
            || attempt.Status != TowerAttemptStatus.Started
            || attempt.SimulationLeaseOwner != leaseOwner
            || attempt.SimulationLeaseUntil <= _timeProvider.GetUtcNow())
            return false;

        var definition = GetRequiredFloor(attempt.FloorNumber);
        try
        {
            var simulationStartedAt = _timeProvider.GetTimestamp();
            var outcome = await ResolveCombatAsync(attempt.TowerRallyId, attempt.Id, definition, cancellationToken);
            await PreparePlaybackAsync(attempt.Id, leaseOwner, definition, outcome, cancellationToken);
            _logger.LogInformation(
                "World Tower attempt {AttemptId} simulated in {ElapsedMilliseconds} ms: {Ticks} ticks, {Events} events, {Frames} frames.",
                attempt.Id,
                _timeProvider.GetElapsedTime(simulationStartedAt).TotalMilliseconds,
                outcome.CombatResult.Duration,
                outcome.CombatResult.EventLog.Count,
                outcome.Checkpoints.Count);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "World Tower attempt {AttemptId} failed during queued simulation.", attempt.Id);
            await MarkAttemptErroredAsync(attempt.Id, leaseOwner, exception.Message, cancellationToken);
            return false;
        }
    }

    public async Task<TowerOperationResult<TowerFloorDetailDto>> ContributeAsync(
        Guid characterId,
        int floorNumber,
        TowerContributionKind kind,
        int amount,
        CancellationToken cancellationToken)
    {
        if (amount != 1)
            return TowerOperationResult<TowerFloorDetailDto>.Fail("Scouting and preparation are performed one action at a time.");
        var definition = _definitions.GetFloor(floorNumber);
        if (definition is null)
            return TowerOperationResult<TowerFloorDetailDto>.Fail("Tower floor was not found.");
        var isScouting = kind == TowerContributionKind.Research;
        var allowedKinds = new[]
        {
            TowerContributionKind.Research,
            TowerContributionKind.SupplyWeapons,
            TowerContributionKind.InscribeWards,
            TowerContributionKind.ScoutWeakPoints
        };
        if (!allowedKinds.Contains(kind))
            return TowerOperationResult<TowerFloorDetailDto>.Fail("Contribution kind is not supported.");

        await EnsureFloorProgressAsync(cancellationToken);
        await _db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, floorNumber, cancellationToken);
        var progress = await _db.TowerFloorProgresses.SingleAsync(
            x => x.ServerId == _options.ServerId && x.FloorNumber == floorNumber,
            cancellationToken);
        if (progress.IsCleared)
            return TowerOperationResult<TowerFloorDetailDto>.Fail("Contributions are not accepted for a cleared floor.");
        if (!isScouting && !progress.UnlockedAt.HasValue)
            return TowerOperationResult<TowerFloorDetailDto>.Fail("Preparation requires this floor to be unlocked.");
        if (!await _db.Characters.AnyAsync(x => x.Id == characterId, cancellationToken))
            return TowerOperationResult<TowerFloorDetailDto>.Fail("Character was not found.");

        var weekKey = GetWeekKey(_timeProvider.GetUtcNow());
        if (isScouting)
        {
            if (progress.ScoutingProgress >= 100)
                return TowerOperationResult<TowerFloorDetailDto>.Fail("Scouting is already complete for this floor.");
            if (progress.ScoutingProgress + amount > 100)
                return TowerOperationResult<TowerFloorDetailDto>.Fail(
                    $"Only {100 - progress.ScoutingProgress} more scouting contribution point(s) can benefit this floor.");
        }
        else
        {
            var preparationTotal = await _db.TowerContributions
                .Where(x => x.ServerId == _options.ServerId
                            && x.FloorNumber == floorNumber
                            && x.WeekKey == weekKey
                            && x.Kind == kind)
                .SumAsync(x => x.Amount, cancellationToken);
            var currentEffect = preparationTotal * _options.PreparationPercentPerPoint;
            if (currentEffect >= _options.PreparationMaxEffectPercent)
                return TowerOperationResult<TowerFloorDetailDto>.Fail("This preparation bonus is already maxed.");
            if (currentEffect + amount * _options.PreparationPercentPerPoint
                > _options.PreparationMaxEffectPercent)
            {
                var remainingPoints = (int)Math.Floor(
                    (_options.PreparationMaxEffectPercent - currentEffect)
                    / _options.PreparationPercentPerPoint);
                return TowerOperationResult<TowerFloorDetailDto>.Fail(
                    $"Only {remainingPoints} more contribution point(s) can benefit this preparation bonus.");
            }
        }

        var used = await _db.TowerContributions
            .Where(x => x.ServerId == _options.ServerId
                        && x.CharacterId == characterId
                        && x.WeekKey == weekKey
                        && (isScouting
                            ? x.Kind == TowerContributionKind.Research
                            : x.Kind != TowerContributionKind.Research))
            .CountAsync(cancellationToken);
        var cap = isScouting
            ? _options.ManualScoutingWeeklyCapPerCharacter
            : _options.PreparationWeeklyCapPerCharacter;
        if (used >= cap)
            return TowerOperationResult<TowerFloorDetailDto>.Fail($"The weekly limit of {cap} actions has been reached.");

        _db.TowerContributions.Add(new TowerContribution
        {
            ServerId = _options.ServerId,
            FloorNumber = floorNumber,
            CharacterId = characterId,
            Kind = kind,
            Amount = amount,
            WeekKey = weekKey,
            CreatedAt = _timeProvider.GetUtcNow()
        });
        if (isScouting)
            progress.AddScoutingProgress(amount, _timeProvider.GetUtcNow());
        await _db.SaveChangesAsync(cancellationToken);

        var detail = await GetFloorAsync(characterId, floorNumber, cancellationToken);
        return TowerOperationResult<TowerFloorDetailDto>.Success(detail!);
    }

    private async Task EnsureFloorProgressAsync(CancellationToken cancellationToken)
    {
        var ownsTransaction = _db.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.BeginTransactionAsync(cancellationToken)
            : null;
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, 1, cancellationToken);

        var existing = await _db.TowerFloorProgresses
            .Where(x => x.ServerId == _options.ServerId)
            .ToDictionaryAsync(x => x.FloorNumber, cancellationToken);
        var existingUnlocks = new HashSet<string>(
            await _db.ServerUnlocks
                .Where(x => x.ServerId == _options.ServerId)
                .Select(x => x.UnlockKey)
                .ToArrayAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var changed = false;

        foreach (var floor in _definitions.GetFloors())
        {
            if (!existing.TryGetValue(floor.FloorNumber, out var state))
            {
                state = new TowerFloorProgress
                {
                    ServerId = _options.ServerId,
                    FloorNumber = floor.FloorNumber,
                    UnlockedAt = floor.FloorNumber == 1 ? now : null,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                existing.Add(floor.FloorNumber, state);
                _db.TowerFloorProgresses.Add(state);
                changed = true;
            }

            if (floor.FloorNumber > 1
                && !state.UnlockedAt.HasValue
                && existing.GetValueOrDefault(floor.FloorNumber - 1)?.IsCleared == true)
            {
                changed |= state.Unlock(now);
            }

            if (state.IsCleared)
            {
                foreach (var unlock in floor.Unlocks)
                {
                    if (!existingUnlocks.Add(unlock.Key))
                        continue;

                    _db.ServerUnlocks.Add(new ServerUnlock
                    {
                        ServerId = _options.ServerId,
                        UnlockKey = unlock.Key,
                        SourceFloorNumber = floor.FloorNumber,
                        UnlockedAt = state.ClearedAt ?? now
                    });
                    changed = true;
                }
            }
        }

        if (changed)
            await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<TowerRally> ActiveRalliesQuery() =>
        _db.TowerRallies.Where(x =>
            x.ServerId == _options.ServerId && ActiveRallyStatuses.Contains(x.Status));

    private async Task<string?> ValidateRallyModeAsync(
        TowerFloorDefinition definition,
        TowerRallyMode mode,
        CancellationToken cancellationToken)
    {
        var progress = await _db.TowerFloorProgresses
            .AsNoTracking()
            .Where(x => x.ServerId == _options.ServerId)
            .ToDictionaryAsync(x => x.FloorNumber, cancellationToken);
        var floor = progress[definition.FloorNumber];

        return mode switch
        {
            TowerRallyMode.FirstClear when !floor.UnlockedAt.HasValue => "This Tower floor is still locked.",
            TowerRallyMode.FirstClear when floor.IsCleared => "This Tower floor has already been cleared.",
            TowerRallyMode.Echo when definition.Type == TowerFloorType.Sovereign => "Sovereign floors cannot be cleared in Echo Mode.",
            TowerRallyMode.Echo when !IsEchoUnlocked(progress) =>
                $"Echo Mode unlocks when Floor {_echoModeUnlockFloor} is cleared.",
            TowerRallyMode.Echo when !floor.IsCleared => "Echo Expeditions require a cleared floor.",
            TowerRallyMode.Echo when !definition.EchoEnabledAfterClear => "Echo Mode is disabled for this floor.",
            _ => null
        };
    }

    private async Task<JoinEligibility> GetJoinEligibilityAsync(
        Guid characterId,
        TowerFloorDefinition definition,
        Guid? targetRallyId,
        CancellationToken cancellationToken)
    {
        var character = await _db.Characters
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null)
            return JoinEligibility.Fail("Character was not found.");

        // Guild membership lives in GuildMembers. Character.Guild is NOT the
        // guild a character belongs to - EF pairs it with Guild.Owner, so it
        // only resolves for the character who founded the guild.
        var membership = await _db.GuildMembers
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .Select(x => new { x.GuildId, GuildName = x.Guild.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (targetRallyId.HasValue)
        {
            var accountAlreadyJoined = await _db.TowerRallyParticipants
                .AsNoTracking()
                .AnyAsync(x => x.TowerRallyId == targetRallyId.Value
                               && x.AccountId == character.UserId, cancellationToken);
            if (accountAlreadyJoined)
                return JoinEligibility.Fail("This account already occupies a slot in the Expedition.");
        }

        var conflicting = await _db.TowerRallyParticipants
            .AsNoTracking()
            .AnyAsync(x => x.CharacterId == characterId
                           && x.TowerRallyId != targetRallyId
                           && ActiveRallyStatuses.Contains(x.TowerRally.Status), cancellationToken);
        if (conflicting)
            return JoinEligibility.Fail("This character is already locked into an active Tower Expedition.");

        var rating = await _powerRatings.GetCharacterRatingAsync(characterId, cancellationToken);
        if (rating.State != PowerAnalysisState.Available)
            return JoinEligibility.Fail(rating.StatusMessage ?? "Power Rating is unavailable for this character.");

        return new JoinEligibility(
            character.Id,
            character.UserId,
            character.Name,
            membership?.GuildId,
            membership?.GuildName,
            CombatRatingDisplay.FromRaw(rating.Overall),
            null);
    }

    private async Task<TowerRallyParticipant> CreateParticipantAsync(
        JoinEligibility eligibility,
        TowerRally rally,
        DateTimeOffset joinedAt,
        CancellationToken cancellationToken,
        Domain.Models.Snapshots.CharacterSnapshot? suppliedSnapshot = null)
    {
        var snapshot = suppliedSnapshot
            ?? await _snapshots.CreateAsync(eligibility.CharacterId, cancellationToken);
        if (suppliedSnapshot is not null)
            _db.CharacterSnapshots.Add(snapshot);
        return new TowerRallyParticipant
        {
            TowerRally = rally,
            CharacterId = eligibility.CharacterId,
            AccountId = eligibility.AccountId,
            CharacterName = eligibility.CharacterName,
            GuildId = eligibility.GuildId,
            GuildName = eligibility.GuildName,
            PowerRating = eligibility.PowerRating,
            CharacterSnapshotId = snapshot.Id,
            CharacterSnapshot = snapshot,
            JoinedAt = joinedAt
        };
    }

    private async Task<TowerCombatOutcome> ResolveCombatAsync(
        Guid rallyId,
        Guid attemptId,
        TowerFloorDefinition definition,
        CancellationToken cancellationToken)
    {
        var snapshotStartedAt = _timeProvider.GetTimestamp();
        var rally = await _db.GetWorldTowerRallyWithSnapshotsAsync(
                rallyId,
                _options.ServerId,
                cancellationToken)
            ?? throw new InvalidOperationException("The Tower Expedition was not found.");
        _logger.LogInformation(
            "World Tower attempt {AttemptId} loaded {ParticipantCount} combat snapshots in {ElapsedMilliseconds} ms.",
            attemptId,
            rally.Participants.Count,
            _timeProvider.GetElapsedTime(snapshotStartedAt).TotalMilliseconds);
        var weekKey = GetWeekKey(_timeProvider.GetUtcNow());
        var contributionTotals = await _db.TowerContributions
            .AsNoTracking()
            .Where(x => x.ServerId == _options.ServerId
                        && x.FloorNumber == definition.FloorNumber
                        && x.WeekKey == weekKey
                        && x.Kind != TowerContributionKind.Research)
            .GroupBy(x => x.Kind)
            .Select(x => new { Kind = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.Kind, x => x.Amount, cancellationToken);
        var isFloorCleared = await _db.TowerFloorProgresses
            .AsNoTracking()
            .AnyAsync(x => x.ServerId == _options.ServerId
                           && x.FloorNumber == definition.FloorNumber
                           && x.IsCleared,
                cancellationToken);
        var preparation = CreatePreparationModifiers(contributionTotals, isFloorCleared);

        var orderedParticipants = rally.Participants
            .OrderBy(x => x.PartySlot)
            .ThenBy(x => x.JoinedAt)
            .ThenBy(x => x.Id)
            .ToArray();
        var friendly = (await _snapshotCombatants.BuildAsync(
            orderedParticipants.Select(participant => new SnapshotCombatantRequest(
                participant.CharacterSnapshot,
                new CombatParticipantSlot(
                    participant.CharacterId.ToString(),
                    participant.CharacterId,
                    CombatSide.Friendly,
                    participant.PartySlot.HasValue
                        ? WorldTowerPartyRules.GetPartyNumber(participant.PartySlot.Value)
                        : null))).ToArray(),
            cancellationToken)).ToList();
        foreach (var participant in friendly)
        {
            AddPercentModifier(participant.Combatant, AttributeType.Power, preparation.PlayerDamagePercent);
            AddPercentModifier(participant.Combatant, AttributeType.ArmorPenetration, preparation.WeakPointPercent);
            AddPercentModifier(participant.Combatant, AttributeType.MagicPenetration, preparation.WeakPointPercent);
        }

        var guardianSource = (await _entities.GetEntitiesByIdsForCombatAsync(
                [definition.GuardianCreatureId],
                cancellationToken))
            .OfType<Creature>()
            .SingleOrDefault()
            ?? throw new InvalidOperationException($"Guardian creature '{definition.GuardianCreatureId}' was not found.");
        var guardian = _combatSetup.CreateCreatureCombatEntities(
            [guardianSource],
            new Area { DifficultyTier = 1 }).Single();
        WorldTowerGuardianScaling.Apply(guardian, definition.GuardianScaling);
        AddPercentModifier(guardian, AttributeType.Power, -preparation.GuardianDamageReductionPercent);
        var hostileSlot = new CombatParticipantSlot(
            definition.GuardianCreatureId.ToString(),
            definition.GuardianCreatureId,
            CombatSide.Hostile);
        var hostile = new CombatRuntimeParticipant(hostileSlot, guardianSource, guardian);

        await _combatSetup.PrepareEntitiesForCombat(
            [.. friendly.Select(x => x.Combatant), hostile.Combatant]);
        var startedAt = _timeProvider.GetUtcNow();
        var plan = new CombatEncounterPlan(
            attemptId,
            CombatMode.Raid,
            1,
            startedAt,
            [.. friendly.Select(x => x.Slot), hostileSlot],
            new RaidEncounterSourceContext(rallyId, 1, $"tower-floor-{definition.FloorNumber}"));
        var runtime = new CombatEncounterRuntime(plan, friendly, [hostile]);
        var engineStartedAt = _timeProvider.GetTimestamp();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
        var execution = _options.CompactPlaybackEnabled
            ? await _combatEngine.ExecuteTowerPlaybackAsync(
                runtime,
                _options.CombatTicksPerFrame,
                cancellationToken)
            : await _combatEngine.ExecuteWithCheckpointsAsync(
                runtime,
                _options.CombatTicksPerFrame,
                cancellationToken);
        var engineElapsed = _timeProvider.GetElapsedTime(engineStartedAt).TotalMilliseconds;
        var allocatedBytes = Math.Max(0, GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore);
        EngineDurationMilliseconds.Record(engineElapsed);
        EngineAllocatedBytes.Record(allocatedBytes);
        _logger.LogInformation(
            "World Tower attempt {AttemptId} engine completed {Ticks} ticks and {Frames} frames in {ElapsedMilliseconds} ms with approximately {AllocatedBytes} allocated bytes.",
            attemptId,
            execution.Result.Duration,
            execution.Checkpoints.Count,
            engineElapsed,
            allocatedBytes);
        var combatResult = execution.Result;
        var resolution = _resultFactory.Create(runtime, combatResult);
        var succeeded = resolution.Outcome == BattleOutcome.Victory;
        var readiness = CreateReadiness(rally.Participants, definition);
        var stats = combatResult.EntityStats.ToDictionary(x => x.EntityId, StringComparer.OrdinalIgnoreCase);
        var summaries = rally.Participants
            .OrderBy(x => x.JoinedAt)
            .Select(participant =>
            {
                stats.TryGetValue(participant.CharacterId.ToString(), out var entityStats);
                var postState = resolution.FriendlyPostState.FirstOrDefault(x =>
                    string.Equals(x.Id, participant.CharacterId.ToString(), StringComparison.OrdinalIgnoreCase));
                return new TowerParticipantCombatSummaryDto(
                    participant.CharacterId,
                    participant.CharacterName,
                    entityStats?.DamageDone ?? 0,
                    entityStats?.DamageTaken ?? 0,
                    entityStats?.HealingDone ?? 0,
                    postState?.Health > 0,
                    participant.PartySlot.HasValue
                        ? WorldTowerPartyRules.GetPartyNumber(participant.PartySlot.Value)
                        : null);
            })
            .ToArray();
        var guardianState = resolution.HostilePostState.Single();
        var healthRemaining = guardianState.MaxHealth <= 0
            ? 0
            : Math.Round(100m * guardianState.Health / guardianState.MaxHealth, 2);
        var duration = Math.Max(0, (int)Math.Ceiling(
            combatResult.Duration / (double)FastCombatEngine.TicksPerSecond));
        var failureReason = succeeded ? null : "The Expedition was defeated before the Guardian fell.";
        var report = new TowerBattleReportDto(
            definition.FloorNumber,
            definition.GuardianName,
            succeeded,
            failureReason,
            duration,
            healthRemaining,
            summaries,
            readiness);

        return new TowerCombatOutcome(
            combatResult,
            execution.Checkpoints,
            report,
            succeeded,
            duration,
            failureReason);
    }

    private async Task<TowerCombatPlaybackDto> PreparePlaybackAsync(
        Guid attemptId,
        string leaseOwner,
        TowerFloorDefinition definition,
        TowerCombatOutcome outcome,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        await _db.AcquireWorldTowerFloorLockAsync(
            _options.ServerId,
            definition.FloorNumber,
            cancellationToken);
        var attempt = await _db.TowerAttempts
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Participants)
            .SingleAsync(x => x.Id == attemptId, cancellationToken);
        if (attempt.Status != TowerAttemptStatus.Started)
            throw new InvalidOperationException("The Tower attempt is no longer awaiting playback preparation.");
        if (attempt.SimulationLeaseOwner != leaseOwner)
            throw new InvalidOperationException("The Tower simulation lease is no longer owned by this worker.");

        if (outcome.Checkpoints.Count == 0 || !outcome.Checkpoints[^1].IsFinal)
            throw new InvalidOperationException("Combat resolution did not produce a final playback frame.");

        var now = _timeProvider.GetUtcNow();
        TowerCombatPlayback playback;
        if (_options.CompactPlaybackEnabled)
        {
            var bundle = CreatePlaybackBundle(outcome);
            var uncompressedBytes = JsonSerializer.SerializeToUtf8Bytes(bundle, _jsonOptions);
            if (uncompressedBytes.Length > _options.MaximumBundleUncompressedBytes)
                throw new InvalidOperationException(
                    $"Tower playback exceeded the {_options.MaximumBundleUncompressedBytes} byte uncompressed limit.");
            var compressedBytes = CompressPlaybackBundle(uncompressedBytes);
            if (compressedBytes.Length > _options.MaximumBundleCompressedBytes)
                throw new InvalidOperationException(
                    $"Tower playback exceeded the {_options.MaximumBundleCompressedBytes} byte compressed limit.");
            var bundleHash = Convert.ToHexString(SHA256.HashData(compressedBytes)).ToLowerInvariant();
            PlaybackBundleBytes.Record(compressedBytes.Length);
            _logger.LogInformation(
                "World Tower attempt {AttemptId} created playback bundle {UncompressedBytes} -> {CompressedBytes} bytes ({FrameCount} frames).",
                attemptId,
                uncompressedBytes.Length,
                compressedBytes.Length,
                bundle.Frames.Count);

            playback = new TowerCombatPlayback
            {
                TowerAttemptId = attempt.Id,
                TowerAttempt = attempt,
                SchemaVersion = TowerCombatPlayback.CompactBundleSchemaVersion,
                TicksPerSecond = FastCombatEngine.TicksPerSecond,
                TicksPerFrame = _options.CombatTicksPerFrame,
                TotalTicks = outcome.CombatResult.Duration,
                FrameCount = bundle.Frames.Count,
                BundleHash = bundleHash,
                BundleLength = compressedBytes.Length,
                BundleContentType = "application/json",
                BundleContentEncoding = "br",
                SimulationCompletedAt = now,
                PlaybackStartedAt = now,
                PlaybackEndsAt = now.AddSeconds(
                    outcome.CombatResult.Duration / (double)FastCombatEngine.TicksPerSecond),
                NextFrameDueAt = now.AddSeconds(
                    outcome.CombatResult.Duration / (double)FastCombatEngine.TicksPerSecond),
                LastPublishedSequence = bundle.Frames.Count - 1
            };
            playback.Artifact = new TowerCombatPlaybackArtifact
            {
                TowerAttemptId = attempt.Id,
                Playback = playback,
                BundleBytes = compressedBytes
            };
        }
        else
        {
            var frames = outcome.Checkpoints
                .Select(checkpoint => ToFrameDto(
                    checkpoint,
                    checkpoint.IsFinal ? outcome.CombatResult.Outcome : null))
                .ToArray();
            playback = new TowerCombatPlayback
            {
                TowerAttemptId = attempt.Id,
                TowerAttempt = attempt,
                SchemaVersion = 1,
                TicksPerSecond = FastCombatEngine.TicksPerSecond,
                TicksPerFrame = _options.CombatTicksPerFrame,
                TotalTicks = outcome.CombatResult.Duration,
                FrameCount = frames.Length,
                TimelineJson = JsonSerializer.Serialize(frames, _jsonOptions),
                SimulationCompletedAt = now,
                PlaybackStartedAt = now,
                PlaybackEndsAt = now.AddSeconds(
                    outcome.CombatResult.Duration / (double)FastCombatEngine.TicksPerSecond),
                NextFrameDueAt = now
            };
        }

        attempt.Status = TowerAttemptStatus.Playback;
        attempt.FightDurationSeconds = outcome.FightDurationSeconds;
        attempt.FailureReason = outcome.FailureReason;
        attempt.CombatResultJson = JsonSerializer.Serialize(outcome.CombatResult, _jsonOptions);
        attempt.BattleReportJson = JsonSerializer.Serialize(outcome.Report, _jsonOptions);
        attempt.SimulationLeaseOwner = null;
        attempt.SimulationLeaseUntil = null;
        attempt.Playback = playback;
        _db.TowerCombatPlaybacks.Add(playback);
        await EnqueueRallyUpdateAsync(attempt.TowerRally, "PlaybackReady", now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToPlaybackDto(playback, now);
    }

    public async Task<bool> PublishDuePlaybackFrameAsync(
        Guid attemptId,
        string leaseOwner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var playback = await _db.TowerCombatPlaybacks
            .AsNoTracking()
            .Include(x => x.TowerAttempt)
                .ThenInclude(x => x.TowerRally)
                    .ThenInclude(x => x.Participants)
            .SingleOrDefaultAsync(x => x.TowerAttemptId == attemptId, cancellationToken);
        if (playback is null
            || playback.DispatchLeaseOwner != leaseOwner
            || playback.DispatchLeaseUntil <= now)
            return false;

        if (playback.SchemaVersion == TowerCombatPlayback.CompactBundleSchemaVersion)
        {
            if (now < playback.PlaybackEndsAt
                || playback.TowerAttempt.Status != TowerAttemptStatus.Playback)
                return false;

            var report = JsonSerializer.Deserialize<TowerBattleReportDto>(
                playback.TowerAttempt.BattleReportJson!, _jsonOptions)
                ?? throw new InvalidOperationException("The stored Tower battle report is invalid.");
            await ApplyOutcomeAsync(
                playback.TowerAttemptId,
                GetRequiredFloor(playback.TowerAttempt.FloorNumber),
                CreateStoredOutcome(playback.TowerAttempt, report),
                cancellationToken);

            var finalizedCursor = await _db.TowerCombatPlaybacks
                .SingleAsync(x => x.TowerAttemptId == attemptId, cancellationToken);
            finalizedCursor.DispatchLeaseOwner = null;
            finalizedCursor.DispatchLeaseUntil = null;
            finalizedCursor.NextFrameDueAt = DateTimeOffset.MaxValue;
            finalizedCursor.RowVersion++;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var frames = DeserializeFrames(playback);
        var dueFrame = GetCurrentFrame(playback, frames, now, revealFinal: true);
        var needsFinalization = dueFrame.IsFinal
            && playback.TowerAttempt.Status == TowerAttemptStatus.Playback;
        if (dueFrame.Sequence <= playback.LastPublishedSequence && !needsFinalization)
            return false;

        if (needsFinalization)
        {
            var report = JsonSerializer.Deserialize<TowerBattleReportDto>(
                playback.TowerAttempt.BattleReportJson!, _jsonOptions)
                ?? throw new InvalidOperationException("The stored Tower battle report is invalid.");
            await ApplyOutcomeAsync(
                playback.TowerAttemptId,
                GetRequiredFloor(playback.TowerAttempt.FloorNumber),
                CreateStoredOutcome(playback.TowerAttempt, report),
                cancellationToken);
        }

        var participantIds = playback.TowerAttempt.TowerRally.Participants
            .Select(x => x.CharacterId)
            .Distinct()
            .ToArray();
        if (dueFrame.Sequence > playback.LastPublishedSequence)
        {
            await _realtime.PublishAsync(
                new Audience.Characters(participantIds),
                new WorldTowerCombatFrameUpdated(
                    playback.TowerAttemptId,
                    playback.TowerAttempt.TowerRallyId,
                    playback.PlaybackStartedAt,
                    playback.TicksPerSecond,
                    playback.TicksPerFrame,
                    dueFrame),
                nameof(WorldTowerService),
                cancellationToken);
        }

        var cursor = await _db.TowerCombatPlaybacks
            .SingleAsync(x => x.TowerAttemptId == attemptId, cancellationToken);
        cursor.DispatchLeaseOwner = null;
        cursor.DispatchLeaseUntil = null;
        cursor.NextFrameDueAt = GetNextFrameDueAt(playback, frames, dueFrame);
        if (cursor.LastPublishedSequence >= dueFrame.Sequence)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        cursor.LastPublishedSequence = dueFrame.Sequence;
        cursor.RowVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TowerCombatOutcome CreateStoredOutcome(
        TowerAttempt attempt,
        TowerBattleReportDto report) =>
        new(
            new CombatResult
            {
                Outcome = report.Succeeded ? BattleOutcome.Victory : BattleOutcome.Defeat
            },
            [],
            report,
            report.Succeeded,
            attempt.FightDurationSeconds ?? 0,
            attempt.FailureReason);

    private async Task ApplyOutcomeAsync(
        Guid attemptId,
        TowerFloorDefinition definition,
        TowerCombatOutcome outcome,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        await _db.AcquireWorldTowerFloorLockAsync(_options.ServerId, definition.FloorNumber, cancellationToken);

        var attempt = await _db.TowerAttempts
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Participants)
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Applications)
            .SingleAsync(x => x.Id == attemptId, cancellationToken);
        if (attempt.Status is TowerAttemptStatus.Succeeded or TowerAttemptStatus.Failed)
            return;
        if (attempt.Status != TowerAttemptStatus.Playback)
            throw new InvalidOperationException("Only a playing Tower attempt can be finalized.");
        var progress = await _db.TowerFloorProgresses.SingleAsync(
            x => x.ServerId == _options.ServerId && x.FloorNumber == definition.FloorNumber,
            cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var createdHallRecord = false;
        var unlockedNextFloor = false;

        attempt.Succeeded = outcome.Succeeded;
        attempt.Status = outcome.Succeeded ? TowerAttemptStatus.Succeeded : TowerAttemptStatus.Failed;
        attempt.CompletedAt = now;
        attempt.FightDurationSeconds = outcome.FightDurationSeconds;
        attempt.FailureReason = outcome.FailureReason;
        attempt.TowerRally.Status = TowerRallyStatus.Completed;
        attempt.TowerRally.CompletedAt = now;

        if (outcome.Succeeded && attempt.Mode == TowerRallyMode.FirstClear && !progress.IsCleared)
        {
            createdHallRecord = progress.RecordFirstClear(attempt.Id, now);

            var next = _definitions.GetFloor(definition.FloorNumber + 1);
            if (next is not null)
            {
                var nextProgress = await _db.TowerFloorProgresses.SingleAsync(
                    x => x.ServerId == _options.ServerId && x.FloorNumber == next.FloorNumber,
                    cancellationToken);
                if (!nextProgress.UnlockedAt.HasValue)
                    unlockedNextFloor = nextProgress.Unlock(now);
            }

            foreach (var key in definition.Unlocks
                         .Select(x => x.Key)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await _db.ServerUnlocks.AnyAsync(
                        x => x.ServerId == _options.ServerId && x.UnlockKey == key,
                        cancellationToken))
                {
                    _db.ServerUnlocks.Add(new ServerUnlock
                    {
                        ServerId = _options.ServerId,
                        UnlockKey = key,
                        SourceFloorNumber = definition.FloorNumber,
                        UnlockedAt = now
                    });
                }
            }

            await GrantTowerTokensAsync(
                attempt.TowerRally.Participants.Select(x => x.CharacterId),
                definition.FirstClearTowerTokens,
                cancellationToken);
        }
        else if (outcome.Succeeded && attempt.Mode == TowerRallyMode.Echo)
        {
            var weekKey = GetWeekKey(now);
            var characterIds = attempt.TowerRally.Participants
                .Select(x => x.CharacterId)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            foreach (var characterId in characterIds)
                await _db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
            var alreadyRewarded = await _db.TowerEchoClears
                .Where(x => x.ServerId == _options.ServerId
                            && x.WeekKey == weekKey
                            && characterIds.Contains(x.CharacterId))
                .Select(x => x.CharacterId)
                .ToListAsync(cancellationToken);
            var eligible = characterIds.Except(alreadyRewarded).ToArray();
            foreach (var characterId in eligible)
            {
                _db.TowerEchoClears.Add(new TowerEchoClear
                {
                    ServerId = _options.ServerId,
                    FloorNumber = definition.FloorNumber,
                    CharacterId = characterId,
                    WeekKey = weekKey,
                    ClearedAt = now
                });
            }
            await GrantTowerTokensAsync(eligible, definition.TowerTokens, cancellationToken);
        }
        else if (!outcome.Succeeded && attempt.Mode == TowerRallyMode.FirstClear)
        {
            var weekStart = GetWeekStart(now);
            var failedAttemptsThisWeek = await _db.TowerAttempts.CountAsync(
                x => x.ServerId == _options.ServerId
                     && x.FloorNumber == definition.FloorNumber
                     && x.Mode == TowerRallyMode.FirstClear
                     && x.Status == TowerAttemptStatus.Failed
                     && x.CompletedAt >= weekStart,
                cancellationToken);
            if (failedAttemptsThisWeek < _options.FailedAttemptScoutingWeeklyCap)
                progress.AddScoutingProgress(_options.FailedAttemptScoutingGain, now);
        }

        // A floor is conquered exactly once per server. RecordFirstClear returns false when
        // the floor was already cleared and the whole method holds the floor lock, so this
        // cannot double-announce; Echo repeat-clears and defeats never set it at all.
        if (createdHallRecord)
        {
            await EnqueueFloorConqueredChatAnnouncementAsync(
                attempt.TowerRally,
                definition,
                now,
                cancellationToken);
        }

        await EnqueueRallyUpdateAsync(
            attempt.TowerRally,
            outcome.Succeeded ? "CompletedVictory" : "CompletedDefeat",
            now,
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

    }

    private async Task GrantTowerTokensAsync(
        IEnumerable<Guid> characterIds,
        int amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
            return;
        var ids = characterIds.Distinct().ToArray();
        var characters = await _db.Characters
            .Where(x => ids.Contains(x.Id) &&
                        !ActiveSharedRestrictions().Any(restriction =>
                            restriction.AccountId == x.UserId))
            .ToListAsync(cancellationToken);
        foreach (var character in characters)
            character.TowerTokens = checked(character.TowerTokens + amount);
    }

    private IQueryable<AccountRestriction> ActiveSharedRestrictions()
    {
        var now = _timeProvider.GetUtcNow();
        return _db.AccountRestrictions.Where(restriction =>
            restriction.RevokedAt == null &&
            (restriction.ExpiresAt == null || restriction.ExpiresAt > now) &&
            (restriction.RestrictionType == AccountRestrictionType.Ban ||
             restriction.RestrictionType == AccountRestrictionType.MultiplayerRestriction));
    }

    private async Task MarkAttemptErroredAsync(
        Guid attemptId,
        string leaseOwner,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var attempt = await _db.TowerAttempts
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Participants)
            .Include(x => x.TowerRally)
                .ThenInclude(x => x.Applications)
            .SingleAsync(x => x.Id == attemptId, cancellationToken);
        if (attempt.Status != TowerAttemptStatus.Started
            || attempt.SimulationLeaseOwner != leaseOwner)
            return;
        attempt.Status = TowerAttemptStatus.Errored;
        attempt.FailureReason = failureReason.Length <= 500 ? failureReason : failureReason[..500];
        attempt.CompletedAt = DateTimeOffset.UtcNow;
        attempt.SimulationLeaseOwner = null;
        attempt.SimulationLeaseUntil = null;
        attempt.TowerRally.Status = TowerRallyStatus.Completed;
        attempt.TowerRally.CompletedAt = attempt.CompletedAt;
        await EnqueueRallyUpdateAsync(
            attempt.TowerRally,
            "CompletedError",
            attempt.CompletedAt.Value,
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void AddPercentModifier(CombatEntity entity, AttributeType attribute, decimal amount)
    {
        if (amount == 0)
            return;
        entity.TemporaryModifiers.Add(new InstanceAttributeModifier(
            attribute,
            (float)amount,
            ModifierType.Multiplicative));
    }

    private PreparationModifiers CreatePreparationModifiers(
        IReadOnlyDictionary<TowerContributionKind, int> totals,
        bool isFloorCleared)
    {
        decimal Effect(TowerContributionKind kind) => isFloorCleared
            ? _options.PreparationMaxEffectPercent
            : Math.Min(
                _options.PreparationMaxEffectPercent,
                totals.GetValueOrDefault(kind) * _options.PreparationPercentPerPoint);

        return new PreparationModifiers(
            Effect(TowerContributionKind.SupplyWeapons),
            Effect(TowerContributionKind.InscribeWards),
            Effect(TowerContributionKind.ScoutWeakPoints));
    }

    private async Task<TowerFloorDetailDto> ToFloorDetailAsync(
        Guid characterId,
        TowerFloorDefinition definition,
        IReadOnlyDictionary<int, TowerFloorProgress> progress,
        IReadOnlyList<TowerRally> rallies,
        Guid? currentCharacterRallyId,
        CancellationToken cancellationToken)
    {
        var floorProgress = progress[definition.FloorNumber];
        var weekKey = GetWeekKey(_timeProvider.GetUtcNow());
        var weeklyContributions = await _db.TowerContributions
            .AsNoTracking()
            .Where(x => x.ServerId == _options.ServerId
                        && x.WeekKey == weekKey)
            .ToListAsync(cancellationToken);
        var floorContributions = weeklyContributions
            .Where(x => x.FloorNumber == definition.FloorNumber)
            .ToArray();
        decimal Effect(TowerContributionKind kind) => floorProgress.IsCleared
            ? _options.PreparationMaxEffectPercent
            : Math.Min(
                _options.PreparationMaxEffectPercent,
                floorContributions.Where(x => x.Kind == kind).Sum(x => x.Amount)
                * _options.PreparationPercentPerPoint);
        var weeklyCharacterPreparation = weeklyContributions
            .Where(x => x.CharacterId == characterId && x.Kind != TowerContributionKind.Research)
            .Count();
        var weeklyCharacterResearch = weeklyContributions
            .Where(x => x.CharacterId == characterId && x.Kind == TowerContributionKind.Research)
            .Count();
        var echoRewardClaimedThisWeek = await _db.TowerEchoClears
            .AsNoTracking()
            .AnyAsync(x => x.ServerId == _options.ServerId
                           && x.CharacterId == characterId
                           && x.WeekKey == weekKey,
                cancellationToken);
        var state = GetFloorState(floorProgress, rallies.Count > 0);
        var knownGuardianReveals = GetGuardianAbilityReveals(definition)
            .Where(x => x.Threshold <= floorProgress.ScoutingProgress)
            .ToArray();
        var knownGuardianTags = knownGuardianReveals
            .SelectMany(x => x.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TowerFloorDetailDto(
            definition.FloorNumber,
            definition.Name,
            definition.Type,
            state,
            definition.RequiredSlots,
            definition.RecommendedPowerRating,
            !currentCharacterRallyId.HasValue,
            currentCharacterRallyId,
            state is TowerFloorStateType.Sealed or TowerFloorStateType.Scouting or TowerFloorStateType.Rallying,
            floorProgress.IsCleared && definition.EchoEnabledAfterClear && IsEchoUnlocked(progress),
            floorProgress.ScoutingProgress,
            weeklyCharacterResearch,
            _options.ManualScoutingWeeklyCapPerCharacter,
            new TowerGuardianInfoDto(
                definition.GuardianName,
                knownGuardianTags,
                knownGuardianReveals),
            new TowerPreparationSummaryDto(
                Effect(TowerContributionKind.SupplyWeapons),
                Effect(TowerContributionKind.InscribeWards),
                Effect(TowerContributionKind.ScoutWeakPoints),
                weeklyCharacterPreparation,
                _options.PreparationWeeklyCapPerCharacter,
                _options.PreparationMaxEffectPercent),
            rallies.Select(ToRallySummary).ToArray(),
            definition.Unlocks
                .Select(x => new TowerUnlockDto(x.Key, x.Description))
                .ToArray(),
            definition.FirstClearTowerTokens,
            definition.TowerTokens,
            echoRewardClaimedThisWeek);
    }

    private IReadOnlyList<TowerScoutingRevealDto> GetGuardianAbilityReveals(
        TowerFloorDefinition definition)
    {
        var profileId = definition.GuardianAbilityProfileId;
        var catalog = _abilityCatalog.GetCatalog();
        var abilities = _creatureAbilities.GetAbilityIds(profileId)
            .Select(abilityId => catalog.AbilitiesById.TryGetValue(abilityId, out var ability)
                ? ability
                : throw new InvalidOperationException(
                    $"World Tower floor {definition.FloorNumber} Guardian '{profileId}' references unknown ability '{abilityId}'."))
            .OrderBy(ability => ability.Kind == AbilitySpecKind.Passive ? 1 : 0)
            .ToArray();

        if (abilities.Length == 0)
            throw new InvalidOperationException(
                $"World Tower floor {definition.FloorNumber} Guardian '{profileId}' has no configured abilities.");
        if (abilities.Length > 4)
            throw new InvalidOperationException(
                $"World Tower floor {definition.FloorNumber} Guardian '{profileId}' has {abilities.Length} abilities; scouting supports at most four reveals.");

        var thresholds = GetScoutingThresholds(abilities.Length);
        return abilities
            .Select((ability, index) => new TowerScoutingRevealDto(
                thresholds[index],
                ability.Name,
                ability.Description,
                ability.Kind,
                ability.Kind == AbilitySpecKind.Active
                    ? (int)Math.Ceiling(ability.CooldownTicks / (double)FastCombatEngine.TicksPerSecond)
                    : null,
                ability.Tags))
            .ToArray();
    }

    private static IReadOnlyList<int> GetScoutingThresholds(int abilityCount)
    {
        return abilityCount switch
        {
            1 => [100],
            2 => [25, 100],
            3 => [25, 50, 100],
            4 => [25, 50, 75, 100],
            _ => []
        };
    }

    private TowerFloorSummaryDto ToFloorSummary(
        TowerFloorDefinition definition,
        TowerFloorProgress progress,
        bool hasActiveRally) =>
        new(
            definition.FloorNumber,
            definition.Name,
            definition.Type,
            GetFloorState(progress, hasActiveRally),
            definition.RequiredSlots,
            definition.RecommendedPowerRating,
            progress.ScoutingProgress,
            definition.GuardianName);

    private static TowerFloorStateType GetFloorState(TowerFloorProgress progress, bool hasActiveRally) =>
        progress.IsCleared ? TowerFloorStateType.Cleared
        : !progress.UnlockedAt.HasValue ? TowerFloorStateType.Locked
        : hasActiveRally ? TowerFloorStateType.Rallying
        : progress.ScoutingProgress > 0 ? TowerFloorStateType.Scouting
        : TowerFloorStateType.Sealed;

    private TowerRallyDto ToRallyDto(TowerRally rally, Guid characterId, Guid? accountId)
    {
        var definition = GetRequiredFloor(rally.FloorNumber);
        var isParticipant = rally.Participants.Any(x => x.CharacterId == characterId);
        var canWatchPlayback = isParticipant || rally.Status == TowerRallyStatus.InProgress;
        var accountOccupiesSlot = accountId.HasValue
            && rally.Participants.Any(x => x.AccountId == accountId.Value);
        var isLeader = rally.CreatedByCharacterId == characterId;
        var accountHasPendingApplication = accountId.HasValue
            && rally.Applications.Any(x =>
                x.AccountId == accountId.Value
                && x.Status == TowerRallyApplicationStatus.Pending);
        var visibleApplications = rally.Applications
            .Where(x => isLeader
                ? x.Status == TowerRallyApplicationStatus.Pending
                : x.CharacterId == characterId
                  && x.Status != TowerRallyApplicationStatus.Withdrawn
                  && (x.Status != TowerRallyApplicationStatus.Accepted || isParticipant))
            .OrderBy(x => x.AppliedAt)
            .Select(x => new TowerRallyApplicationDto(
                x.Id,
                x.CharacterId,
                x.CharacterName,
                x.GuildName,
                x.PowerRating,
                x.Status,
                x.AppliedAt,
                x.CharacterId == characterId))
            .ToArray();
        return new TowerRallyDto(
            rally.Id,
            rally.FloorNumber,
            definition.GuardianName,
            rally.Mode,
            rally.Status,
            rally.CreatedByCharacterId,
            rally.RequiredSlots,
            WorldTowerPartyRules.GetPartyCount(rally.RequiredSlots),
            WorldTowerPartyRules.MaximumPartySize,
            rally.CreatedAt,
            rally.Participants
                .OrderBy(x => x.PartySlot.HasValue)
                .ThenBy(x => x.PartySlot)
                .ThenBy(x => x.JoinedAt)
                .Select(x => new TowerRallyParticipantDto(
                    x.CharacterId,
                    x.CharacterName,
                    x.GuildName,
                    x.PowerRating,
                    x.JoinedAt,
                    x.CharacterId == rally.CreatedByCharacterId,
                    x.CharacterId == characterId,
                    x.PartySlot,
                    x.PartySlot.HasValue
                        ? WorldTowerPartyRules.GetPartyNumber(x.PartySlot.Value)
                        : null))
                .ToArray(),
            visibleApplications,
            CreateReadiness(rally.Participants, definition),
            !accountOccupiesSlot
                && !accountHasPendingApplication
                && rally.Status == TowerRallyStatus.Recruiting
                && rally.Participants.Count < rally.RequiredSlots,
            isLeader && rally.Status == TowerRallyStatus.Recruiting,
            isLeader && rally.Status is (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready),
            (isParticipant || visibleApplications.Any(x => x.IsCurrentCharacter && x.Status == TowerRallyApplicationStatus.Pending))
                && rally.Status is (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready),
            isLeader
                && rally.Status == TowerRallyStatus.Ready
                && WorldTowerPartyRules.HasCompletePartyLayout(rally),
            (isParticipant || visibleApplications.Any(x => x.IsCurrentCharacter && x.Status == TowerRallyApplicationStatus.Pending))
                && rally.Status is (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready),
            isLeader
                && rally.Status is (TowerRallyStatus.Recruiting or TowerRallyStatus.Ready)
                && rally.Participants.Any(x => x.CharacterId != characterId),
            rally.Attempt is null
                ? null
                : new TowerAttemptSummaryDto(
                    rally.Attempt.Id,
                    rally.Attempt.Status,
                    rally.Attempt.Succeeded,
                    rally.Attempt.FightDurationSeconds,
                    rally.Attempt.FailureReason,
                    isParticipant
                        && rally.Attempt.Status is TowerAttemptStatus.Succeeded or TowerAttemptStatus.Failed
                        && !string.IsNullOrWhiteSpace(rally.Attempt.CombatResultJson),
                    rally.Attempt.Playback is null || !canWatchPlayback
                        ? null
                        : ToPlaybackDto(rally.Attempt.Playback, _timeProvider.GetUtcNow()),
                    string.IsNullOrWhiteSpace(rally.Attempt.BattleReportJson)
                        || rally.Attempt.Status is TowerAttemptStatus.Started or TowerAttemptStatus.Playback
                        ? null
                        : JsonSerializer.Deserialize<TowerBattleReportDto>(rally.Attempt.BattleReportJson, _jsonOptions)));
    }

    private TowerCombatPlaybackDto ToPlaybackDto(
        TowerCombatPlayback playback,
        DateTimeOffset now)
    {
        var completed = playback.TowerAttempt.Status is TowerAttemptStatus.Succeeded or TowerAttemptStatus.Failed;
        if (playback.SchemaVersion == TowerCombatPlayback.CompactBundleSchemaVersion)
        {
            var elapsedTicks = Math.Clamp(
                (int)Math.Floor((now - playback.PlaybackStartedAt).TotalSeconds * playback.TicksPerSecond),
                0,
                playback.TotalTicks);
            var currentSequence = elapsedTicks >= playback.TotalTicks
                ? Math.Max(0, playback.FrameCount - 1)
                : Math.Min(
                    Math.Max(0, playback.FrameCount - 1),
                    elapsedTicks / Math.Max(1, playback.TicksPerFrame));
            return new TowerCombatPlaybackDto(
                playback.TowerAttemptId,
                playback.TowerAttempt.TowerRallyId,
                playback.PlaybackStartedAt,
                playback.PlaybackEndsAt,
                playback.TicksPerSecond,
                playback.TicksPerFrame,
                playback.TotalTicks,
                playback.FrameCount,
                currentSequence,
                null,
                completed,
                playback.SchemaVersion,
                now,
                playback.BundleHash);
        }

        var frames = DeserializeFrames(playback);
        var current = GetCurrentFrame(playback, frames, now, completed);
        return new TowerCombatPlaybackDto(
            playback.TowerAttemptId,
            playback.TowerAttempt.TowerRallyId,
            playback.PlaybackStartedAt,
            playback.PlaybackEndsAt,
            playback.TicksPerSecond,
            playback.TicksPerFrame,
            playback.TotalTicks,
            playback.FrameCount,
            current.Sequence,
            current,
            completed,
            playback.SchemaVersion,
            now,
            null);
    }

    private TowerCombatFrameDto[] DeserializeFrames(TowerCombatPlayback playback) =>
        _timelineCache.GetOrCreate(
            $"world-tower:timeline:{playback.TowerAttemptId}:{playback.SchemaVersion}",
            entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                return JsonSerializer.Deserialize<TowerCombatFrameDto[]>(
                           playback.TimelineJson
                           ?? throw new InvalidOperationException(
                               $"Tower playback for attempt '{playback.TowerAttemptId}' has no version 1 timeline."),
                           _jsonOptions)
                    ?? throw new InvalidOperationException(
                        $"Tower playback for attempt '{playback.TowerAttemptId}' is invalid.");
            })!;

    private TowerPlaybackBundleDto CreatePlaybackBundle(TowerCombatOutcome outcome)
    {
        var entityById = new Dictionary<string, TowerPlaybackEntityDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var checkpoint in outcome.Checkpoints)
        {
            AddEntities(checkpoint.Friendly, true);
            AddEntities(checkpoint.Hostile, false);
        }

        var entities = entityById.Values.OrderBy(x => x.Index).ToArray();
        var abilityKeys = outcome.Checkpoints
            .SelectMany(x => x.EntityStats)
            .Where(x => entityById.ContainsKey(x.EntityId))
            .SelectMany(entity => entity.Abilities.Select(ability =>
                (EntityIndex: entityById[entity.EntityId].Index, ability.Name)))
            .Distinct()
            .OrderBy(x => x.EntityIndex)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();
        var abilities = abilityKeys
            .Select((key, index) => new TowerPlaybackAbilityDto(index, key.EntityIndex, key.Name))
            .ToArray();
        var abilityIndex = abilities.ToDictionary(
            x => (x.EntityIndex, x.Name),
            x => x.Index);

        var frames = outcome.Checkpoints.Select(checkpoint =>
        {
            var state = checkpoint.Friendly
                .Concat(checkpoint.Hostile)
                .Select(entity => new TowerPlaybackEntityStateDto(
                    entityById[entity.Id].Index,
                    entity.Health,
                    entity.Barrier))
                .OrderBy(x => x.EntityIndex)
                .ToArray();
            var totals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .Select(entity => new TowerPlaybackEntityTotalsDto(
                    entityById[entity.EntityId].Index,
                    entity.DamageDone,
                    entity.DamageTaken,
                    entity.HealingDone,
                    entity.HealingReceived,
                    entity.HealthRegenerated,
                    entity.BarrierGenerated,
                    entity.DamageBlocked))
                .OrderBy(x => x.EntityIndex)
                .ToArray();
            var abilityTotals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .SelectMany(entity => entity.Abilities.Select(ability =>
                    new TowerPlaybackAbilityTotalsDto(
                        abilityIndex[(entityById[entity.EntityId].Index, ability.Name)],
                        ability.Uses,
                        ability.TotalDamage,
                        ability.TotalHealing,
                        ability.TotalBarrier,
                        ability.DamageByType)))
                .OrderBy(x => x.AbilityIndex)
                .ToArray();
            return new TowerPlaybackBundleFrameDto(
                checkpoint.Sequence,
                checkpoint.Tick,
                state,
                totals,
                abilityTotals,
                checkpoint.IsFinal,
                checkpoint.IsFinal ? outcome.CombatResult.Outcome : null);
        }).ToArray();

        return new TowerPlaybackBundleDto(
            TowerCombatPlayback.CompactBundleSchemaVersion,
            FastCombatEngine.TicksPerSecond,
            _options.CombatTicksPerFrame,
            outcome.CombatResult.Duration,
            entities,
            abilities,
            frames);

        void AddEntities(IEnumerable<SimpleCombatEntity> source, bool isFriendly)
        {
            foreach (var entity in source)
            {
                if (entityById.ContainsKey(entity.Id))
                    continue;
                entityById[entity.Id] = new TowerPlaybackEntityDto(
                    entityById.Count,
                    entity.Id,
                    entity.Name,
                    entity.ImagePath,
                    isFriendly,
                    entity.MaxHealth,
                    entity.Level,
                    entity.PartyNumber);
            }
        }
    }

    private static byte[] CompressPlaybackBundle(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            brotli.Write(bytes);
        return output.ToArray();
    }

    private static DateTimeOffset GetNextFrameDueAt(
        TowerCombatPlayback playback,
        IReadOnlyList<TowerCombatFrameDto> frames,
        TowerCombatFrameDto publishedFrame)
    {
        var nextIndex = publishedFrame.Sequence + 1;
        var nextFrame = nextIndex >= 0
                        && nextIndex < frames.Count
                        && frames[nextIndex].Sequence == nextIndex
            ? frames[nextIndex]
            : frames.FirstOrDefault(frame => frame.Sequence > publishedFrame.Sequence);
        return nextFrame is null
            ? DateTimeOffset.MaxValue
            : playback.PlaybackStartedAt.AddSeconds(
                nextFrame.Tick / (double)playback.TicksPerSecond);
    }

    private static TowerCombatFrameDto GetCurrentFrame(
        TowerCombatPlayback playback,
        IReadOnlyList<TowerCombatFrameDto> frames,
        DateTimeOffset now,
        bool revealFinal)
    {
        if (frames.Count == 0)
            throw new InvalidOperationException("Tower playback has no frames.");

        var elapsedTicks = Math.Max(0, (int)Math.Floor(
            (now - playback.PlaybackStartedAt).TotalSeconds * playback.TicksPerSecond));
        var low = 0;
        var high = frames.Count - 1;
        while (low < high)
        {
            var middle = low + (high - low + 1) / 2;
            if (frames[middle].Tick <= elapsedTicks)
                low = middle;
            else
                high = middle - 1;
        }
        var dueIndex = low;
        if (!revealFinal && frames[dueIndex].IsFinal)
            dueIndex = Math.Max(0, dueIndex - 1);
        return frames[dueIndex];
    }

    private TowerCombatFrameDto ToFrameDto(CombatCheckpoint checkpoint, BattleOutcome? outcome) =>
        new(
            checkpoint.Sequence,
            checkpoint.Tick,
            checkpoint.Friendly.Select(_mapper.Map<SimpleCombatEntityDto>).ToArray(),
            checkpoint.Hostile.Select(_mapper.Map<SimpleCombatEntityDto>).ToArray(),
            checkpoint.EntityStats,
            checkpoint.Events.Select(item => new CombatEventDto(
                item.Source,
                item.StatsSource,
                item.CountsAsActivation,
                item.Timestamp,
                item.ActorId,
                item.TargetId,
                item.EventType,
                item.Magnitude,
                item.Details)).ToArray(),
            checkpoint.IsFinal,
            outcome);

    private Task EnqueueTowerBattleChatAnnouncementAsync(
        TowerRally rally,
        TowerFloorDefinition definition,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken)
    {
        var body = $"The Expedition against {definition.GuardianName} - Floor {definition.FloorNumber} is starting!";

        return _outbox.EnqueueAsync(
            GameEventTypes.WorldTowerChatAnnouncement,
            new WorldTowerChatAnnouncementPayload(
                rally.Id,
                CreateTowerAnnouncementMessageId(rally.Id, "battle-started"),
                body,
                string.Format(
                    CultureInfo.InvariantCulture,
                    TowerExpeditionTargetUrlFormat,
                    rally.Id),
                sentAt),
            characterId: null,
            accountId: null,
            cancellationToken: cancellationToken);
    }

    private Task EnqueueFloorConqueredChatAnnouncementAsync(
        TowerRally rally,
        TowerFloorDefinition definition,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken)
    {
        var body = $"{definition.GuardianName} has fallen - Floor {definition.FloorNumber} "
            + "of the World Tower has been conquered!";

        return _outbox.EnqueueAsync(
            GameEventTypes.WorldTowerChatAnnouncement,
            new WorldTowerChatAnnouncementPayload(
                rally.Id,
                CreateTowerAnnouncementMessageId(rally.Id, "floor-conquered"),
                body,
                TowerHallOfFameTargetUrl,
                sentAt),
            characterId: null,
            accountId: null,
            cancellationToken: cancellationToken);
    }

    private static Guid CreateTowerAnnouncementMessageId(Guid rallyId, string announcementKey)
    {
        var hash = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"world-tower:{rallyId:N}:{announcementKey}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private async Task EnqueueRallyUpdateAsync(
        TowerRally rally,
        string eventName,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        await _outbox.EnqueueAsync(
            GameEventTypes.WorldTowerRallyUpdated,
            new WorldTowerRallyUpdated(
                rally.Id,
                rally.FloorNumber,
                eventName,
                rally.Status.ToString(),
                rally.Participants.Count,
                rally.RequiredSlots,
                rally.Applications.Count(x => x.Status == TowerRallyApplicationStatus.Pending),
                occurredAt),
            characterId: null,
            accountId: null,
            cancellationToken);
    }

    private static TowerRosterReadinessDto CreateReadiness(
        IEnumerable<TowerRallyParticipant> participants,
        TowerFloorDefinition definition)
    {
        var list = participants.ToArray();
        var average = list.Length == 0 ? 0 : (int)Math.Round(list.Average(x => x.PowerRating));
        var ratio = definition.RecommendedPowerRating <= 0
            ? 1m
            : (decimal)average / definition.RecommendedPowerRating;
        var rating = ratio switch
        {
            < 0.70m => "VeryWeak",
            < 0.90m => "Weak",
            < 1.10m => "Fair",
            < 1.30m => "Good",
            < 1.60m => "Excellent",
            _ => "Overwhelming"
        };
        var warnings = new List<string>();
        if (list.Length < definition.RequiredSlots)
            warnings.Add($"{definition.RequiredSlots - list.Length} Expedition slot(s) are still empty.");
        var benched = list.Count(participant => !participant.PartySlot.HasValue);
        if (benched > 0)
            warnings.Add($"{benched} participant(s) must be assigned from the bench before the Expedition can start.");
        if (average < definition.RecommendedPowerRating)
            warnings.Add("Average Power Rating is below the floor recommendation.");
        if (list.Length > 0 && list.Min(x => x.PowerRating) < definition.RecommendedPowerRating * 0.75m)
            warnings.Add("At least one build is well below the recommended rating.");

        return new TowerRosterReadinessDto(
            rating,
            average,
            definition.RecommendedPowerRating,
            warnings);
    }

    private static TowerRallySummaryDto ToRallySummary(TowerRally rally) =>
        new(
            rally.Id,
            rally.FloorNumber,
            rally.Mode,
            rally.Participants.Single(x => x.CharacterId == rally.CreatedByCharacterId).CharacterName,
            rally.Status,
            rally.Participants.Count,
            rally.RequiredSlots,
            rally.Applications.Count(x => x.Status == TowerRallyApplicationStatus.Pending),
            rally.CreatedAt,
            rally.StartedAt);

    private bool IsEchoUnlocked(IReadOnlyDictionary<int, TowerFloorProgress> progress) =>
        progress.GetValueOrDefault(_echoModeUnlockFloor)?.IsCleared == true;

    private TowerFloorDefinition GetRequiredFloor(int floorNumber) =>
        _definitions.GetFloor(floorNumber)
        ?? throw new InvalidOperationException($"World Tower floor {floorNumber} is missing from the catalog.");

    private static int GetWeekKey(DateTimeOffset value)
    {
        var date = value.UtcDateTime;
        return ISOWeek.GetYear(date) * 100 + ISOWeek.GetWeekOfYear(date);
    }

    private static DateTimeOffset GetWeekStart(DateTimeOffset value)
    {
        var date = value.UtcDateTime.Date;
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return new DateTimeOffset(date.AddDays(-daysSinceMonday), TimeSpan.Zero);
    }

    private sealed record JoinEligibility(
        Guid CharacterId,
        Guid AccountId,
        string CharacterName,
        Guid? GuildId,
        string? GuildName,
        int PowerRating,
        string? Error)
    {
        public static JoinEligibility Fail(string error) =>
            new(Guid.Empty, Guid.Empty, string.Empty, null, null, 0, error);
    }

    private sealed record PreparationModifiers(
        decimal PlayerDamagePercent,
        decimal GuardianDamageReductionPercent,
        decimal WeakPointPercent);

    private sealed record TowerCombatOutcome(
        CombatResult CombatResult,
        IReadOnlyList<CombatCheckpoint> Checkpoints,
        TowerBattleReportDto Report,
        bool Succeeded,
        int FightDurationSeconds,
        string? FailureReason);
}
