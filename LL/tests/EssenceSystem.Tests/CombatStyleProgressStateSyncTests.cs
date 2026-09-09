using Application.Interfaces.WebSockets;
using Application.MediatR.Behaviors;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Dungeons.Commands.ClaimDungeonRewards;
using Application.UseCases.Dungeons.Dtos;
using Application.WebSockets.Contracts;
using Common.Primitives;
using Domain.Models.CombatStyles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Services.LL.Synchronization;

namespace EssenceSystem.Tests;

public sealed class CombatStyleProgressStateSyncTests
{
    private static readonly long[] XpRequirements = [100, 200, 400, 700, 1100, 1600, 2300, 3200, 4400, 6000];

    [Theory]
    [InlineData(0, 0, 30, true)]
    [InlineData(0, 90, 30, true)]
    [InlineData(10, 0, 30, false)]
    [InlineData(0, 0, 0, false)]
    public async Task Idle_combat_publishes_mastery_updates_only_when_progress_changes(
        int level, long currentXp, long award, bool shouldInvalidate)
    {
        await using var db = CreateDb();
        var style = new CharacterCombatStyle
        {
            CharacterId = Guid.NewGuid(),
            CombatStyleId = CombatStyleIds.Conduit,
            Level = level,
            CurrentXp = currentXp
        };
        db.CharacterCombatStyles.Add(style);
        await db.SaveChangesAsync();

        var realtime = new RecordingRealtimeBroadcaster();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior<ResolveCharacterActionCommand, Response<CharacterActionDto?>>(db, sync)
            .Handle(new ResolveCharacterActionCommand(style.CharacterId), _ =>
            {
                CombatStyleProgression.Grant(style, award, XpRequirements);
                return Task.FromResult(Response<CharacterActionDto?>.Success(null));
            }, CancellationToken.None);

        var checkpoint = await sync.GetCheckpointAsync(style.CharacterId, CancellationToken.None);
        Assert.Equal(shouldInvalidate ? 1L : 0L, checkpoint.Revisions[StateSyncScopes.CombatStyles]);
        Assert.Equal(shouldInvalidate ? 1 : 0, realtime.Messages.Count(message => IsStyleInvalidation(message, style.CharacterId)));
    }

    [Fact]
    public async Task Batched_combat_notifies_each_style_owner_once_including_party_members()
    {
        await using var db = CreateDb();
        var styles = Enumerable.Range(0, 2).Select(_ => new CharacterCombatStyle
        {
            CharacterId = Guid.NewGuid(),
            CombatStyleId = CombatStyleIds.Bastion
        }).ToArray();
        db.CharacterCombatStyles.AddRange(styles);
        await db.SaveChangesAsync();

        var realtime = new RecordingRealtimeBroadcaster();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior<ResolveCharacterActionCommand, Response<CharacterActionDto?>>(db, sync)
            .Handle(new ResolveCharacterActionCommand(styles[0].CharacterId), _ =>
            {
                for (var encounter = 0; encounter < 10; encounter++)
                {
                    foreach (var style in styles)
                        CombatStyleProgression.Grant(style, 30, XpRequirements);
                }
                return Task.FromResult(Response<CharacterActionDto?>.Success(null));
            }, CancellationToken.None);

        foreach (var style in styles)
        {
            var checkpoint = await sync.GetCheckpointAsync(style.CharacterId, CancellationToken.None);
            Assert.Equal(1L, checkpoint.Revisions[StateSyncScopes.CombatStyles]);
            Assert.Equal(1, realtime.Messages.Count(message => IsStyleInvalidation(message, style.CharacterId)));
        }
        db.ChangeTracker.Clear();
        Assert.All(await db.CharacterCombatStyles.ToListAsync(), style => Assert.Equal(2, style.Level));
    }

    [Fact]
    public async Task Dungeon_claim_publishes_mastery_update_even_when_other_scopes_are_handled_by_response()
    {
        await using var db = CreateDb();
        var style = new CharacterCombatStyle
        {
            CharacterId = Guid.NewGuid(),
            CombatStyleId = CombatStyleIds.Conduit
        };
        db.CharacterCombatStyles.Add(style);
        await db.SaveChangesAsync();

        var realtime = new RecordingRealtimeBroadcaster();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior<ClaimDungeonRewardsCommand, Response<ClaimDungeonRewardsResponseDto>>(db, sync)
            .Handle(new ClaimDungeonRewardsCommand(style.CharacterId), _ =>
            {
                CombatStyleProgression.Grant(style, 30, XpRequirements);
                return Task.FromResult(Response<ClaimDungeonRewardsResponseDto>.Success(null!));
            }, CancellationToken.None);

        var checkpoint = await sync.GetCheckpointAsync(style.CharacterId, CancellationToken.None);
        Assert.Equal(1L, checkpoint.Revisions[StateSyncScopes.CombatStyles]);
        Assert.Equal(1, realtime.Messages.Count(message => IsStyleInvalidation(message, style.CharacterId)));
    }

    private static bool IsStyleInvalidation(GameRealtimeEvent message, Guid characterId) => message switch
    {
        StateInvalidated invalidation => invalidation.CharacterId == characterId
            && invalidation.Scope == StateSyncScopes.CombatStyles,
        StateInvalidations invalidations => invalidations.CharacterId == characterId
            && invalidations.Revisions.ContainsKey(StateSyncScopes.CombatStyles),
        _ => false
    };

    private static TransactionBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        LLDbContext db, StateSyncService sync) where TRequest : notnull => new(db, sync,
        NullLogger<TransactionBehavior<TRequest, TResponse>>.Instance,
        DungeonInventoryStateSyncTests.CreateSync(db));

    private static LLDbContext CreateDb() => new(new DbContextOptionsBuilder<LLDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public List<GameRealtimeEvent> Messages { get; } = [];

        public Task PublishAsync(Audience audience, GameRealtimeEvent message, string sender,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
