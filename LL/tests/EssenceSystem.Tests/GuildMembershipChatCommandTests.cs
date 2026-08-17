using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.UseCases.Guilds.Commands.AcceptInvite;
using Application.UseCases.Guilds.Commands.ApproveApplication;
using Application.UseCases.Guilds.Commands.ChangeGuildMemberRole;
using Application.UseCases.Guilds.Commands.Invite;
using Application.UseCases.Guilds.Commands.InviteCharacterByName;
using Application.UseCases.Guilds.Commands.KickGuildMember;
using Application.UseCases.Guilds.Commands.LeaveGuild;
using Application.UseCases.Guilds.Dtos.Requests;
using Application.WebSockets.Contracts;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildMembershipChatCommandTests
{
    [Fact]
    public async Task AcceptInvitePublishesJoinedMessage()
    {
        var fixture = new CommandFixture();
        var handler = new AcceptInviteCommandHandler(
            fixture.GuildService,
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new AcceptInviteCommand(fixture.SubjectId, fixture.GuildId.ToString()),
            CancellationToken.None);

        fixture.AssertChat(GuildSystemChatEvent.Joined);
    }

    [Fact]
    public async Task ApproveApplicationPublishesJoinedMessage()
    {
        var fixture = new CommandFixture();
        var handler = new ApproveApplicationCommandHandler(
            fixture.GuildService,
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new ApproveApplicationCommand(
                fixture.ActorId,
                fixture.SubjectId.ToString()),
            CancellationToken.None);

        fixture.AssertChat(GuildSystemChatEvent.Joined);
    }

    [Fact]
    public async Task LeaveGuildPublishesLeftMessage()
    {
        var fixture = new CommandFixture();
        var handler = new LeaveGuildCommandHandler(
            fixture.GuildService,
            fixture.Events,
            fixture.Outbox,
            fixture.Chat);

        await handler.Handle(
            new LeaveGuildCommand(fixture.SubjectId),
            CancellationToken.None);

        fixture.AssertChat(GuildSystemChatEvent.Left);
    }

    [Fact]
    public async Task KickMemberPublishesKickedMessage()
    {
        var fixture = new CommandFixture();
        var handler = new KickGuildMemberCommandHandler(
            fixture.GuildService,
            fixture.Events,
            fixture.Outbox,
            fixture.Chat);

        await handler.Handle(
            new KickGuildMemberCommand(fixture.ActorId, fixture.SubjectId),
            CancellationToken.None);

        fixture.AssertChat(GuildSystemChatEvent.Kicked);
    }

    [Theory]
    [InlineData(GuildRole.Member, GuildRole.Officer, GuildSystemChatEvent.PromotedToOfficer)]
    [InlineData(GuildRole.Officer, GuildRole.Member, GuildSystemChatEvent.DemotedToMember)]
    public async Task ChangeMemberRolePublishesRoleTransitionMessage(
        GuildRole previousRole,
        GuildRole nextRole,
        GuildSystemChatEvent expectedEvent)
    {
        var fixture = new CommandFixture(previousRole);
        var handler = new ChangeGuildMemberRoleCommandHandler(
            fixture.GuildService,
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new ChangeGuildMemberRoleCommand(
                fixture.ActorId,
                new ChangeGuildMemberRoleDto(fixture.SubjectId, nextRole)),
            CancellationToken.None);

        fixture.AssertChat(expectedEvent);
    }

    [Fact]
    public async Task InviteByIdPublishesInvitedMessage()
    {
        var fixture = new CommandFixture();
        var handler = new InviteCommandHandler(
            fixture.GuildService,
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new InviteCommand(
                fixture.ActorId,
                new InviteToGuildDto(
                    fixture.GuildId.ToString(),
                    fixture.SubjectId.ToString())),
            CancellationToken.None);

        fixture.AssertChat(GuildSystemChatEvent.Invited);
    }

    [Fact]
    public async Task InviteByNamePublishesInvitedMessage()
    {
        var fixture = new CommandFixture();
        var handler = new InviteCharacterByNameCommandHandler(
            fixture.GuildService,
            new StubCharacterService(fixture.SubjectId),
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new InviteCharacterByNameCommand(
                fixture.ActorId,
                new InviteToGuildDto(fixture.GuildId.ToString(), "Ember")),
            CancellationToken.None);

        fixture.AssertChat(GuildSystemChatEvent.Invited);
    }

    private sealed class CommandFixture
    {
        public Guid GuildId { get; } = Guid.NewGuid();
        public Guid ActorId { get; } = Guid.NewGuid();
        public Guid SubjectId { get; } = Guid.NewGuid();
        public RecordingGuildChatPublisher Chat { get; } = new();
        public RecordingEventPublisher Events { get; } = new();
        public RecordingOutbox Outbox { get; } = new();
        public StubGuildService GuildService { get; }

        public CommandFixture(GuildRole subjectRole = GuildRole.Member)
        {
            var guild = new Guild { Id = GuildId };
            guild.Members.Add(new GuildMember
            {
                GuildId = GuildId,
                CharacterId = SubjectId,
                Role = subjectRole
            });
            GuildService = new StubGuildService(guild);
        }

        public void AssertChat(GuildSystemChatEvent expectedEvent)
        {
            var call = Assert.Single(Chat.Calls);
            Assert.Equal(GuildId, call.GuildId);
            Assert.Equal(SubjectId, call.SubjectCharacterId);
            Assert.Equal(expectedEvent, call.EventType);
        }
    }

    private sealed class RecordingGuildChatPublisher : IGuildSystemChatPublisher
    {
        public List<GuildChatCall> Calls { get; } = [];

        public Task PublishAsync(
            Guid guildId,
            Guid subjectCharacterId,
            GuildSystemChatEvent eventType,
            CancellationToken cancellationToken)
        {
            Calls.Add(new GuildChatCall(guildId, subjectCharacterId, eventType));
            return Task.CompletedTask;
        }

        public Task PublishBuildingAsync(
            Guid guildId,
            Guid actorCharacterId,
            string buildingName,
            int buildingLevel,
            GuildBuildingChatEvent eventType,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed record GuildChatCall(
        Guid GuildId,
        Guid SubjectCharacterId,
        GuildSystemChatEvent EventType);

    private sealed class RecordingEventPublisher : IGameEventPublisher
    {
        public Task PublishAsync(Audience audience, GameEventMsg message) =>
            Task.CompletedTask;
    }

    private sealed class RecordingOutbox : IGameEventOutbox
    {
        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubGuildService(Guild guild) : IGuildService
    {
        public Task<Guild?> GetGuildForMemberAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult<Guild?>(guild);
        public Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ApproveApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ChangeMemberRoleAsync(Guid characterId, Guid targetCharacterId, GuildRole role, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> KickMemberAsync(Guid characterId, Guid targetCharacterId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guild?> GetMyGuildAsync(Guid guildId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RejectInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RejectApplicationAsync(Guid characterId, Guid applicationCharacterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> UpdateRolePermissionsAsync(Guid characterId, GuildRolePermission permissions, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> UpdateDescriptionAsync(Guid characterId, string description, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubCharacterService(Guid characterId) : ICharacterService
    {
        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult<Guid?>(characterId);
        public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
