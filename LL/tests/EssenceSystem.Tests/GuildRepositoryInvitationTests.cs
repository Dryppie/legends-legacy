using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildRepositoryInvitationTests
{
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
