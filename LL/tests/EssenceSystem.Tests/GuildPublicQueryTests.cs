using System.Text.Json;
using Application.Common.Mappings;
using Application.UseCases.Guilds.Queries.GetAllGuilds;
using Application.UseCases.Guilds.Queries.GetPublicGuild;
using AutoMapper;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildPublicQueryTests
{
    [Fact]
    public async Task Visitor_can_read_members_and_building_levels_without_private_guild_data()
    {
        await using var db = CreateContext();
        var member = new Character { Id = Guid.NewGuid(), Name = "Guild Leader", Level = 42 };
        var guild = new Guild
        {
            Name = "Other Guild",
            Owner = member,
            OwnerId = member.Id,
            Description = "Private guild description",
            Tag = "OG",
            GuildXp = 12345,
            Members = [new GuildMember { Character = member, CharacterId = member.Id, Role = GuildRole.Leader }],
            Buildings = [new GuildBuilding { Type = GuildBuildingType.GuildHall, Level = 3 }],
            Resources = [new GuildResource { Resource = GuildResourceType.GuildSupplies, Amount = 999 }],
        };
        db.Guilds.Add(guild);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new GuildRepository(db);
        var publicGuild = await repository.GetPublicGuildAsync(guild.Id, CancellationToken.None);
        Assert.NotNull(publicGuild);
        Assert.Empty(publicGuild.Resources);
        Assert.Empty(db.ChangeTracker.Entries());

        var dto = await CreateHandler(db).Handle(new GetPublicGuildQuery(guild.Id), CancellationToken.None);
        Assert.NotNull(dto);
        Assert.Equal("Other Guild", dto.Name);
        Assert.Equal("OG", dto.Tag);
        Assert.True(dto.MaxMembers > 0);
        var publicMember = Assert.Single(dto.Members);
        Assert.Equal(member.Id, publicMember.CharacterId);
        Assert.Equal("Guild Leader", publicMember.Name);
        Assert.Equal(42, publicMember.Level);
        Assert.Equal(GuildRole.Leader, publicMember.Role);
        var building = Assert.Single(dto.Buildings);
        Assert.Equal(GuildBuildingType.GuildHall, building.Type);
        Assert.Equal(3, building.Level);

        // Keep the public wire contract limited even if the domain grows new private fields.
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(new[] { "buildings", "id", "maxMembers", "members", "name", "tag" }, json.RootElement.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(new[] { "characterId", "level", "name", "role" }, json.RootElement.GetProperty("members")[0].EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(new[] { "level", "type" }, json.RootElement.GetProperty("buildings")[0].EnumerateObject().Select(p => p.Name).Order());

        var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var service = new GuildService(repository);
        var directory = await new GetAllGuildsQueryHandler(service, mapper)
            .Handle(new GetAllGuildsQuery(), CancellationToken.None);
        using var directoryJson = JsonDocument.Parse(JsonSerializer.Serialize(Assert.Single(directory), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.False(directoryJson.RootElement.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task Unknown_guild_returns_no_profile()
    {
        await using var db = CreateContext();
        Assert.Null(await CreateHandler(db).Handle(new GetPublicGuildQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private static GetPublicGuildQueryHandler CreateHandler(LLDbContext db)
    {
        var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        return new GetPublicGuildQueryHandler(new GuildService(new GuildRepository(db)), mapper);
    }

    private static LLDbContext CreateContext() => new(new DbContextOptionsBuilder<LLDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
