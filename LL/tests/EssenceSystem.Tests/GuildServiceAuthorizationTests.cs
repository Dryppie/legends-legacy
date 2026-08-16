using Domain.Models.Guilds;
using Services.LL.Guilds;

public sealed class GuildServiceAuthorizationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("  AB  ")]
    public async Task CreateAsync_ReturnsFalse_WhenTrimmedNameIsShorterThanThreeCharacters(string name)
    {
        var repository = new FakeGuildRepository();
        var service = new GuildService(repository);

        var result = await service.CreateAsync(Guid.NewGuid(), name, CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.CreateCalled);
    }

    [Fact]
    public async Task CreateAsync_TrimsAndForwardsName_WhenNameHasAtLeastThreeCharacters()
    {
        var repository = new FakeGuildRepository();
        var service = new GuildService(repository);

        var result = await service.CreateAsync(Guid.NewGuid(), "  ABC  ", CancellationToken.None);

        Assert.True(result);
        Assert.True(repository.CreateCalled);
        Assert.Equal("ABC", repository.CreatedGuildName);
    }

    [Fact]
    public async Task InviteAsync_ReturnsFalse_WhenRequestedGuildDoesNotMatchMemberGuild()
    {
        var memberGuildId = Guid.NewGuid();
        var requestedGuildId = Guid.NewGuid();
        var currentCharacterId = Guid.NewGuid();
        var invitedCharacterId = Guid.NewGuid();
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = memberGuildId,
                Guild = new Guild { Id = memberGuildId },
                CharacterId = currentCharacterId,
                Role = GuildRole.Officer
            }
        };
        var service = new GuildService(repository);

        var result = await service.InviteAsync(
            currentCharacterId,
            requestedGuildId,
            invitedCharacterId,
            CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.InviteCalled);
    }

    [Fact]
    public async Task InviteAsync_ForwardsInvite_WhenRequestedGuildMatchesMemberGuild()
    {
        var guildId = Guid.NewGuid();
        var currentCharacterId = Guid.NewGuid();
        var invitedCharacterId = Guid.NewGuid();
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = guildId,
                Guild = new Guild { Id = guildId },
                CharacterId = currentCharacterId,
                Role = GuildRole.Officer
            }
        };
        var service = new GuildService(repository);

        var result = await service.InviteAsync(
            currentCharacterId,
            guildId,
            invitedCharacterId,
            CancellationToken.None);

        Assert.True(result);
        Assert.True(repository.InviteCalled);
    }

    [Fact]
    public async Task InviteCharacterByNameAsync_ReturnsFalse_WhenRequestedGuildDoesNotMatchMemberGuild()
    {
        var memberGuildId = Guid.NewGuid();
        var requestedGuildId = Guid.NewGuid();
        var currentCharacterId = Guid.NewGuid();
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = memberGuildId,
                Guild = new Guild { Id = memberGuildId },
                CharacterId = currentCharacterId,
                Role = GuildRole.Leader
            }
        };
        var service = new GuildService(repository);

        var result = await service.InviteCharacterByNameAsync(
            currentCharacterId,
            requestedGuildId,
            "OtherCharacter",
            CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.InviteByNameCalled);
    }

    [Fact]
    public async Task InviteAsync_ReturnsFalse_WhenRoleInvitePermissionIsDisabled()
    {
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var guild = new Guild { Id = guildId };
        guild.RolePermissions.Add(new GuildRolePermission
        {
            GuildId = guildId,
            Role = GuildRole.Officer,
            CanInvite = false
        });
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = guildId,
                Guild = guild,
                CharacterId = characterId,
                Role = GuildRole.Officer
            }
        };

        var result = await new GuildService(repository).InviteAsync(
            characterId,
            guildId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.InviteCalled);
    }

    [Theory]
    [InlineData(GuildRole.Leader)]
    [InlineData(GuildRole.Officer)]
    public async Task UpdateDescriptionAsync_TrimsAndForwardsDescription_ForLeadersAndOfficers(GuildRole role)
    {
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = guildId,
                Guild = new Guild { Id = guildId },
                CharacterId = characterId,
                Role = role
            }
        };

        var result = await new GuildService(repository).UpdateDescriptionAsync(
            characterId,
            "  We raid on weekends.  ",
            CancellationToken.None);

        Assert.True(result);
        Assert.True(repository.UpdateDescriptionCalled);
        Assert.Equal("We raid on weekends.", repository.UpdatedDescription);
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ReturnsFalse_ForPlainMembers()
    {
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = guildId,
                Guild = new Guild { Id = guildId },
                CharacterId = characterId,
                Role = GuildRole.Member
            }
        };

        var result = await new GuildService(repository).UpdateDescriptionAsync(
            characterId,
            "Let me in charge",
            CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.UpdateDescriptionCalled);
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ReturnsFalse_WhenCharacterHasNoGuild()
    {
        var repository = new FakeGuildRepository();

        var result = await new GuildService(repository).UpdateDescriptionAsync(
            Guid.NewGuid(),
            "Hello",
            CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.UpdateDescriptionCalled);
    }

    [Fact]
    public async Task UpdateDescriptionAsync_ReturnsFalse_WhenDescriptionExceedsMaximumLength()
    {
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var repository = new FakeGuildRepository
        {
            Member = new GuildMember
            {
                GuildId = guildId,
                Guild = new Guild { Id = guildId },
                CharacterId = characterId,
                Role = GuildRole.Leader
            }
        };

        var result = await new GuildService(repository).UpdateDescriptionAsync(
            characterId,
            new string('a', GuildService.MaximumGuildDescriptionLength + 1),
            CancellationToken.None);

        Assert.False(result);
        Assert.False(repository.UpdateDescriptionCalled);
    }

    private sealed class FakeGuildRepository : IGuildRepository
    {
        public GuildMember? Member { get; init; }
        public bool CreateCalled { get; private set; }
        public string? CreatedGuildName { get; private set; }
        public bool InviteCalled { get; private set; }
        public bool InviteByNameCalled { get; private set; }
        public bool UpdateDescriptionCalled { get; private set; }
        public string? UpdatedDescription { get; private set; }

        public Task<bool> CreateAsync(Guid characterId, string name, CancellationToken cancellationToken)
        {
            CreateCalled = true;
            CreatedGuildName = name;
            return Task.FromResult(true);
        }

        public Task<bool> InviteAsync(Guid currentCharacterId, Guid guildId, Guid invitedCharacterId, CancellationToken cancellationToken)
        {
            InviteCalled = true;
            return Task.FromResult(true);
        }

        public Task<bool> InviteCharacterByNameAsync(Guid currentCharacterId, Guid guildId, string invitedCharacterName, CancellationToken cancellationToken)
        {
            InviteByNameCalled = true;
            return Task.FromResult(true);
        }

        public Task<bool> AcceptInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> LeaveGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> DisbandGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<Guild?> GetMyGuildAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<Guild?>(null);

        public Task<List<Guild>> GetAllGuildsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new List<Guild>());

        public Task<GuildMember?> GetGuildMember(Guid currentCharacterId, CancellationToken cancellationToken) =>
            Task.FromResult(Member);

        public Task<List<GuildInvite>> GetMyInvitesAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new List<GuildInvite>());

        public Task<bool> ApplyToGuildAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> RejectGuildInviteAsync(Guid characterId, Guid guildId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> ApproveApplicationAsync(Guid guildId, Guid applicationCharacterId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<Guild?> GetGuildForMemberAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<Guild?>(null);

        public Task<bool> ChangeMemberRoleAsync(Guid guildId, Guid characterId, GuildRole role, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> KickMemberAsync(Guid guildId, Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> UpdateRolePermissionsAsync(Guid guildId, GuildRolePermission permissions, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> UpdateDescriptionAsync(Guid guildId, string description, CancellationToken cancellationToken)
        {
            UpdateDescriptionCalled = true;
            UpdatedDescription = description;
            return Task.FromResult(true);
        }
    }
}
