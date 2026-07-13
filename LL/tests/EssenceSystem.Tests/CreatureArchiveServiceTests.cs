using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
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
        var essence = Assert.Single(creature.Essences);
        Assert.Equal("Cave Bat", creature.Name);
        Assert.Equal("essence.cave_bat", essence.EssenceDefinitionId);
        Assert.Equal("Cave Bat Essence", essence.Name);
        Assert.True(essence.IsAbsorbed);
        Assert.False(creature.IsEssenceFocus);
        Assert.Equal(0, creature.EssenceFocusTotalDurationSeconds);
        Assert.Equal(0, creature.CurrentEssenceFocusDurationSeconds);
        Assert.Contains("Species.Beast", creature.Tags);
    }

    [Fact]
    public async Task GetCreatureArchive_includes_total_and_current_focus_duration()
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
            IsEssenceFocus = true,
            EssenceFocusSetAtUtc = DateTimeOffset.UtcNow.AddMinutes(-90),
            EssenceFocusTotalDurationSeconds = 300,
            FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            LastDefeatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var archive = await service.GetCreatureArchiveAsync(characterId, CancellationToken.None);

        var creature = Assert.Single(archive.Creatures);
        Assert.True(creature.IsEssenceFocus);
        Assert.InRange(creature.CurrentEssenceFocusDurationSeconds, 89 * 60, 91 * 60);
        Assert.InRange(creature.EssenceFocusTotalDurationSeconds, 300 + (89 * 60), 300 + (91 * 60));
    }

    [Fact]
    public async Task SetEssenceFocus_marks_one_known_creature_as_focused()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Set<CharacterCreatureArchiveEntry>().AddRange(
            new CharacterCreatureArchiveEntry
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                CreatureDefinitionId = "monster.cave_bat",
                CreatureName = "Cave Bat",
                KillCount = 3,
                FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                LastDefeatedAtUtc = DateTimeOffset.UtcNow
            },
            new CharacterCreatureArchiveEntry
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                CreatureDefinitionId = "monster.forest_wolf",
                CreatureName = "Forest Wolf",
                KillCount = 5,
                FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                LastDefeatedAtUtc = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var archive = await service.SetEssenceFocusAsync(characterId, "monster.forest_wolf", CancellationToken.None);

        Assert.Contains(archive.Creatures, creature => creature.CreatureId == "monster.forest_wolf" && creature.IsEssenceFocus);
        Assert.Contains(archive.Creatures, creature => creature.CreatureId == "monster.cave_bat" && !creature.IsEssenceFocus);
        Assert.False(archive.CanChangeEssenceFocus);
        Assert.NotNull(archive.EssenceFocusSetAtUtc);
        Assert.NotNull(archive.EssenceFocusAvailableAtUtc);
        Assert.True(await service.IsEssenceFocusAsync(characterId, "monster.forest_wolf", CancellationToken.None));
        Assert.False(await service.IsEssenceFocusAsync(characterId, "monster.cave_bat", CancellationToken.None));
    }

    [Fact]
    public async Task SetEssenceFocus_blocks_new_target_until_cooldown_expires()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Set<CharacterCreatureArchiveEntry>().AddRange(
            new CharacterCreatureArchiveEntry
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                CreatureDefinitionId = "monster.cave_bat",
                CreatureName = "Cave Bat",
                KillCount = 3,
                FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                LastDefeatedAtUtc = DateTimeOffset.UtcNow
            },
            new CharacterCreatureArchiveEntry
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                CreatureDefinitionId = "monster.forest_wolf",
                CreatureName = "Forest Wolf",
                KillCount = 5,
                FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                LastDefeatedAtUtc = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.SetEssenceFocusAsync(characterId, "monster.cave_bat", CancellationToken.None);
        var blocked = await service.SetEssenceFocusAsync(characterId, "monster.forest_wolf", CancellationToken.None);

        Assert.Contains(blocked.Creatures, creature => creature.CreatureId == "monster.cave_bat" && creature.IsEssenceFocus);
        Assert.Contains(blocked.Creatures, creature => creature.CreatureId == "monster.forest_wolf" && !creature.IsEssenceFocus);

        var caveBat = await db.Set<CharacterCreatureArchiveEntry>()
            .SingleAsync(entry => entry.CharacterId == characterId && entry.CreatureDefinitionId == "monster.cave_bat");
        caveBat.EssenceFocusSetAtUtc = DateTimeOffset.UtcNow.AddHours(-9);
        await db.SaveChangesAsync();

        var changed = await service.SetEssenceFocusAsync(characterId, "monster.forest_wolf", CancellationToken.None);

        Assert.Contains(changed.Creatures, creature => creature.CreatureId == "monster.forest_wolf" && creature.IsEssenceFocus);
        Assert.Contains(changed.Creatures, creature => creature.CreatureId == "monster.cave_bat" && !creature.IsEssenceFocus);
        Assert.Contains(changed.Creatures, creature =>
            creature.CreatureId == "monster.cave_bat" &&
            creature.EssenceFocusTotalDurationSeconds >= 8 * 60 * 60 &&
            creature.CurrentEssenceFocusDurationSeconds == 0);
        Assert.False(changed.CanChangeEssenceFocus);
    }

    [Fact]
    public async Task SetEssenceFocus_with_null_keeps_current_focus()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Set<CharacterCreatureArchiveEntry>().Add(new CharacterCreatureArchiveEntry
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            CreatureDefinitionId = "monster.cave_bat",
            CreatureName = "Cave Bat",
            KillCount = 3,
            IsEssenceFocus = true,
            EssenceFocusSetAtUtc = DateTimeOffset.UtcNow,
            FirstDefeatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            LastDefeatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var archive = await service.SetEssenceFocusAsync(characterId, null, CancellationToken.None);

        Assert.Contains(archive.Creatures, creature => creature.CreatureId == "monster.cave_bat" && creature.IsEssenceFocus);
        Assert.False(archive.CanChangeEssenceFocus);
        Assert.True(await service.IsEssenceFocusAsync(characterId, "monster.cave_bat", CancellationToken.None));
    }

    [Fact]
    public async Task GetEssenceCodex_reveals_collection_when_one_member_has_been_absorbed()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.PlayerEssences.Add(new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = "essence.cave_bat"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var codex = await service.GetEssenceCodexAsync(characterId, CancellationToken.None);

        var entry = Assert.Single(codex.Entries);
        Assert.Equal("codex.collection.beasts", entry.Id);
        Assert.Equal(1, entry.Current);
        Assert.Equal(3, entry.Required);
        Assert.False(entry.IsUnlocked);
        Assert.Equal(BonusKind.EssenceDropRateRelativeBps, entry.BonusKind);
        Assert.Equal(50, entry.BaseBonusValue);
        Assert.Equal(50, entry.BonusValue);
        Assert.Equal(0, entry.CollectionAscensionTier);
        Assert.Equal(10, entry.BonusValuePerCollectionAscensionTier);
        Assert.Contains(entry.Essences, member => member.EssenceDefinitionId == "essence.cave_bat" && member.IsAbsorbed && member.AscensionTier == 0);
        Assert.Contains(entry.Essences, member => member.EssenceDefinitionId == "essence.forest_wolf" && !member.IsAbsorbed);
    }

    [Fact]
    public async Task GetEssenceCodex_completes_collection_when_all_members_are_absorbed()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.PlayerEssences.AddRange(
            CreatePlayerEssence(characterId, "essence.cave_bat"),
            CreatePlayerEssence(characterId, "essence.forest_wolf"),
            CreatePlayerEssence(characterId, "essence.stone_boar"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var codex = await service.GetEssenceCodexAsync(characterId, CancellationToken.None);

        var entry = Assert.Single(codex.Entries);
        Assert.True(entry.IsUnlocked);
        Assert.Equal(3, entry.Current);
        Assert.Equal(0, entry.CollectionAscensionTier);
        Assert.Equal(50, entry.BonusValue);
        Assert.All(entry.Essences, member => Assert.True(member.IsAbsorbed));
    }

    [Fact]
    public async Task GetEssenceCodex_upgrades_collection_bonus_from_lowest_member_ascension_tier()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.PlayerEssences.AddRange(
            CreatePlayerEssence(characterId, "essence.cave_bat", ascensionTier: 2),
            CreatePlayerEssence(characterId, "essence.forest_wolf", ascensionTier: 1),
            CreatePlayerEssence(characterId, "essence.stone_boar", ascensionTier: 3));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var codex = await service.GetEssenceCodexAsync(characterId, CancellationToken.None);

        var entry = Assert.Single(codex.Entries);
        Assert.True(entry.IsUnlocked);
        Assert.Equal(1, entry.CollectionAscensionTier);
        Assert.Equal(EssenceProgressionConstants.MaxAscensionTier, entry.MaxCollectionAscensionTier);
        Assert.Equal(60, entry.BonusValue);
        Assert.Contains(entry.Essences, member => member.EssenceDefinitionId == "essence.cave_bat" && member.AscensionTier == 2);
        Assert.Contains(entry.Essences, member => member.EssenceDefinitionId == "essence.forest_wolf" && member.AscensionTier == 1);
        Assert.Contains(entry.Essences, member => member.EssenceDefinitionId == "essence.stone_boar" && member.AscensionTier == 3);
    }

    [Fact]
    public async Task EssenceCodexBonusProvider_returns_bonus_for_completed_collections()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.PlayerEssences.AddRange(
            CreatePlayerEssence(characterId, "essence.cave_bat", ascensionTier: 2),
            CreatePlayerEssence(characterId, "essence.forest_wolf", ascensionTier: 2),
            CreatePlayerEssence(characterId, "essence.stone_boar", ascensionTier: 2));
        await db.SaveChangesAsync();
        var definitions = new FakeDefinitionRepository();
        var collectionService = CreateCodexCollectionService(db, definitions);
        var provider = new EssenceCodexBonusProvider(collectionService);

        var bonuses = await provider.GetBonusesAsync(characterId, DateTimeOffset.UtcNow, CancellationToken.None);

        var bonus = Assert.Single(bonuses);
        Assert.Equal(BonusKind.EssenceDropRateRelativeBps, bonus.Kind);
        Assert.Equal(70, bonus.Value);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static CreatureArchiveService CreateService(LLDbContext db)
    {
        var definitions = new FakeDefinitionRepository();
        return new(
            db,
            definitions,
            new FakeCreatureEssenceLootTableRepository(definitions),
            CreateCodexCollectionService(db, definitions));
    }

    private static EssenceCodexCollectionService CreateCodexCollectionService(
        LLDbContext db,
        IEssenceDefinitionRepository definitions) =>
        new(db, new FakeCollectionDefinitionProvider(), definitions);

    private static PlayerEssence CreatePlayerEssence(
        Guid characterId,
        string essenceDefinitionId,
        int ascensionTier = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = essenceDefinitionId,
            AscensionTier = ascensionTier
        };

    private sealed class FakeCollectionDefinitionProvider : IEssenceCodexCollectionDefinitionProvider
    {
        public IReadOnlyList<EssenceCodexCollectionDefinition> GetAll() =>
        [
            new()
            {
                Id = "codex.collection.beasts",
                Title = "Beasts",
                Description = "Absorb the beast study set.",
                Category = "Creature Families",
                EssenceDefinitionIds =
                [
                    "essence.cave_bat",
                    "essence.forest_wolf",
                    "essence.stone_boar"
                ],
                Bonus = new EssenceCodexCollectionBonusDefinition
                {
                    Kind = BonusKind.EssenceDropRateRelativeBps,
                    Value = 50,
                    ValuePerCollectionAscensionTier = 10,
                    Description = "+0.5% Essence drop chance."
                }
            }
        ];
    }

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

    private sealed class FakeCreatureEssenceLootTableRepository(
        IEssenceDefinitionRepository definitions) : ICreatureEssenceLootTableRepository
    {
        private readonly IReadOnlyList<CreatureEssenceLootTableDefinition> _tables = definitions
            .GetAll()
            .GroupBy(definition => definition.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CreatureEssenceLootTableDefinition
            {
                CreatureId = group.Key,
                BaseDropChance = 0.5,
                PassiveAbilityId = group.First().PassiveAbilityId,
                Variants = group
                    .Select(definition => new CreatureEssenceVariantDefinition
                    {
                        EssenceDefinitionId = definition.Id,
                        ActiveAbilityId = definition.ActiveAbilityId,
                        Weight = 1
                    })
                    .ToList()
            })
            .ToList();

        public IReadOnlyList<CreatureEssenceLootTableDefinition> GetAll() => _tables;

        public CreatureEssenceLootTableDefinition? GetByCreatureId(string creatureId) =>
            _tables.FirstOrDefault(x => x.CreatureId.Equals(creatureId, StringComparison.OrdinalIgnoreCase));

        public CreatureEssenceLootTableDefinition? GetByEssenceDefinitionId(string essenceDefinitionId) =>
            _tables.FirstOrDefault(table => table.Variants.Any(variant =>
                variant.EssenceDefinitionId.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase)));
    }
}
