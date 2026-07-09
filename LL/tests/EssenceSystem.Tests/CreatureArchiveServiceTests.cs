using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class CreatureArchiveServiceTests
{
    [Fact]
    public async Task RecordDefeatedCreatures_creates_and_increments_grouped_entries()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var firstDefeatedAt = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        var lastDefeatedAt = firstDefeatedAt.AddMinutes(5);
        var service = CreateService(db);

        await service.RecordDefeatedCreaturesAsync(
            characterId,
            [new Creature { Name = "Cave Bat" }, new Creature { Name = "Cave Bat" }],
            firstDefeatedAt,
            CancellationToken.None);
        await service.RecordDefeatedCreaturesAsync(
            characterId,
            [new Creature { Name = "Cave Bat" }],
            lastDefeatedAt,
            CancellationToken.None);

        var entry = Assert.Single(await db.Set<CharacterCreatureArchiveEntry>().ToListAsync());
        Assert.Equal("monster.cave_bat", entry.CreatureDefinitionId);
        Assert.Equal("Cave Bat", entry.CreatureName);
        Assert.Equal(3, entry.KillCount);
        Assert.Equal(firstDefeatedAt, entry.FirstDefeatedAtUtc);
        Assert.Equal(lastDefeatedAt, entry.LastDefeatedAtUtc);
    }

    [Fact]
    public async Task GetCreatureArchive_links_essence_definition_and_absorbed_state()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Set<CharacterCreatureArchiveEntry>().Add(new CharacterCreatureArchiveEntry
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            CreatureDefinitionId = "monster.cave_bat",
            CreatureName = "Cave Bat",
            KillCount = 7,
            FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            LastDefeatedAtUtc = DateTimeOffset.UtcNow
        });
        db.PlayerEssences.Add(new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = "essence.cave_bat"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var archive = await service.GetCreatureArchiveAsync(characterId, CancellationToken.None);

        var creature = Assert.Single(archive.Creatures);
        Assert.Equal("Cave Bat", creature.Name);
        Assert.Equal("essence.cave_bat", creature.EssenceDefinitionId);
        Assert.Equal("Cave Bat Essence", creature.EssenceName);
        Assert.True(creature.IsEssenceAbsorbed);
        Assert.Contains("Species.Beast", creature.Tags);
    }

    [Fact]
    public async Task GetEssenceCodex_tracks_collection_family_region_and_evolution_milestones()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var evolvedEssence = new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                EssenceDefinitionId = "essence.cave_bat",
                IsEvolved = true
            };

        db.PlayerEssences.AddRange(
            evolvedEssence,
            new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                EssenceDefinitionId = "essence.forest_wolf"
            },
            new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                EssenceDefinitionId = "essence.stone_boar"
            });
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Active",
            IsActive = true,
            Slots =
            [
                new EssenceLoadoutSlot
                {
                    Id = Guid.NewGuid(),
                    SlotIndex = 0,
                    PlayerEssenceId = evolvedEssence.Id
                }
            ]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var codex = await service.GetEssenceCodexAsync(characterId, CancellationToken.None);

        Assert.All(codex.Entries, entry => Assert.True(entry.IsUnlocked));
        Assert.Contains(codex.Entries, entry =>
            entry.Id == "codex.beast-studies-i" &&
            entry.Current == 3 &&
            entry.Required == 3);
        Assert.Contains(codex.Entries, entry =>
            entry.Id == "codex.regional-survey-i" &&
            entry.Current == 3 &&
            entry.Required == 3);
        Assert.Contains(codex.Entries, entry =>
            entry.Id == "codex.attunement-practice" &&
            entry.Current == 1 &&
            entry.Required == 1);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static CreatureArchiveService CreateService(LLDbContext db) =>
        new(db, new FakeDefinitionRepository());

    private sealed class FakeDefinitionRepository : IEssenceDefinitionRepository
    {
        private readonly IReadOnlyList<EssenceDefinition> _definitions =
        [
            CreateDefinition("essence.cave_bat", "monster.cave_bat", "Cave Bat Essence", 1),
            CreateDefinition("essence.forest_wolf", "monster.forest_wolf", "Forest Wolf Essence", 2),
            CreateDefinition("essence.stone_boar", "monster.stone_boar", "Stone Boar Essence", 3)
        ];

        public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;

        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            _definitions.FirstOrDefault(definition =>
                definition.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public EssenceDefinition? GetByMonsterId(string monsterId) =>
            _definitions.FirstOrDefault(definition =>
                definition.SourceMonsterId.Equals(monsterId, StringComparison.OrdinalIgnoreCase));

        public AbilitySpec? GetAbilityById(string abilityId) => null;

        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];

        private static EssenceDefinition CreateDefinition(
            string id,
            string monsterId,
            string name,
            int nativeRegion) =>
            new()
            {
                Id = id,
                SourceMonsterId = monsterId,
                Name = name,
                NativeRegion = nativeRegion,
                Tags = ["Species.Beast"]
            };
    }
}
