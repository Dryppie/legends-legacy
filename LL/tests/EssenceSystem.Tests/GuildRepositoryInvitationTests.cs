using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildRepositoryInvitationTests
{
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, 1, false)]
    [InlineData(false, 2, true)]
    [InlineData(true, 2, true)]
    public async Task Invite_RespectsGuildHallCapacity_AfterReload(bool byName, int hallLevel, bool expected)
    {
        await using var db = CreateDbContext();
        var (inviterId, guildId, _) = SeedGuilds(db);
        var invitee = CreateCharacter("Invitee");
        db.Characters.Add(invitee);
        var guild = db.Guilds.Local.Single(x => x.Id == guildId);
        guild.MaxMembers = 0;
        guild.Buildings.Add(new GuildBuilding
        {
            GuildId = guildId,
            Type = GuildBuildingType.GuildHall,
            Level = hallLevel
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new GuildRepository(db);
        var invited = byName
            ? await repository.InviteCharacterByNameAsync(inviterId, guildId, invitee.Name, CancellationToken.None)
            : await repository.InviteAsync(inviterId, guildId, invitee.Id, CancellationToken.None);
        Assert.Equal(expected, invited);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var invitations = await db.GuildInvites.Where(x => x.GuildId == guildId).ToListAsync();
        if (expected)
        {
            var invitation = Assert.Single(invitations);
            Assert.Equal(invitee.Id, invitation.CharacterId);
            Assert.True(invitation.IsInvite);
        }
        else
            Assert.Empty(invitations);
        Assert.False(await db.GuildMembers.AnyAsync(x => x.CharacterId == invitee.Id));
    }

    [Fact]
    public async Task ApplyToGuildAsync_AddsApplication_WithoutChangingExistingInvitesOrMembers()
    {
        await using var db = CreateDbContext();
        var (_, guildId, invitedCharacter) = SeedGuilds(db);
        var applicant = CreateCharacter("Applicant");
        db.Characters.Add(applicant);
        var existingInvite = new GuildInvite
        {
            GuildId = guildId,
            CharacterId = invitedCharacter.Id,
            IsInvite = true
        };
        db.GuildInvites.Add(existingInvite);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.True(await new GuildRepository(db)
            .ApplyToGuildAsync(applicant.Id, guildId, CancellationToken.None));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var invites = await db.GuildInvites.Where(x => x.GuildId == guildId).ToListAsync();
        Assert.Equal(2, invites.Count);
        Assert.Contains(invites, x => x.CharacterId == invitedCharacter.Id && x.IsInvite);
        Assert.Contains(invites, x => x.CharacterId == applicant.Id && !x.IsInvite);
        Assert.Single(await db.GuildMembers.Where(x => x.GuildId == guildId).ToListAsync());
        Assert.False(await db.GuildMembers.AnyAsync(x => x.CharacterId == applicant.Id));
    }

    [Fact]
    public async Task InviteAsync_ReturnsFalse_WhenInvitedCharacterAlreadyBelongsToGuild()
    {
        await using var db = CreateDbContext();
        var (invitingCharacterId, invitingGuildId, invitedCharacter) = SeedGuilds(db);
        await db.SaveChangesAsync();
        var repository = new GuildRepository(db);

        var result = await repository.InviteAsync(
            invitingCharacterId,
            invitingGuildId,
            invitedCharacter.Id,
            CancellationToken.None);

        Assert.False(result);
        Assert.Empty(db.GuildInvites);
    }

    [Fact]
    public async Task InviteCharacterByNameAsync_ReturnsFalse_WhenInvitedCharacterAlreadyBelongsToGuild()
    {
        await using var db = CreateDbContext();
        var (invitingCharacterId, invitingGuildId, invitedCharacter) = SeedGuilds(db);
        await db.SaveChangesAsync();
        var repository = new GuildRepository(db);

        var result = await repository.InviteCharacterByNameAsync(
            invitingCharacterId,
            invitingGuildId,
            invitedCharacter.Name,
            CancellationToken.None);

        Assert.False(result);
        Assert.Empty(db.GuildInvites);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static (Guid InvitingCharacterId, Guid InvitingGuildId, Character InvitedCharacter) SeedGuilds(
        LLDbContext db)
    {
        var invitingCharacter = CreateCharacter("Inviter");
        var invitedCharacter = CreateCharacter("AlreadyMember");
        var invitingGuildId = Guid.NewGuid();
        var existingGuildId = Guid.NewGuid();

        db.Characters.AddRange(invitingCharacter, invitedCharacter);
        db.Guilds.AddRange(
            new Guild
            {
                Id = invitingGuildId,
                Name = "Inviting Guild",
                OwnerId = invitingCharacter.Id,
                Members =
                {
                    new GuildMember
                    {
                        GuildId = invitingGuildId,
                        CharacterId = invitingCharacter.Id,
                        Role = GuildRole.Leader
                    }
                }
            },
            new Guild
            {
                Id = existingGuildId,
                Name = "Existing Guild",
                OwnerId = invitedCharacter.Id,
                Members =
                {
                    new GuildMember
                    {
                        GuildId = existingGuildId,
                        CharacterId = invitedCharacter.Id,
                        Role = GuildRole.Leader
                    }
                }
            });

        return (invitingCharacter.Id, invitingGuildId, invitedCharacter);
    }

    private static Character CreateCharacter(string name) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = name,
        ImagePath = "player",
        Level = 10
    };
}
