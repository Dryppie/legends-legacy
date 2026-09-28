using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Colosseum;
using Services.LL.Colosseum;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Resolution;

namespace EssenceSystem.Tests;

public sealed class ColosseumBattleTests
{
    [Theory]
    [InlineData(AttributeRules.LegacyVersion, AttributeRules.CurrentVersion, true, false, false)]
    [InlineData(AttributeRules.CurrentVersion, AttributeRules.LegacyVersion, true, false, false)]
    [InlineData(AttributeRules.CurrentVersion, AttributeRules.CurrentVersion, true, false, true)]
    [InlineData(AttributeRules.LegacyVersion, AttributeRules.LegacyVersion, true, false, true)]
    [InlineData(AttributeRules.CurrentVersion, AttributeRules.CurrentVersion, false, false, false)]
    [InlineData(AttributeRules.CurrentVersion, AttributeRules.CurrentVersion, true, true, false)]
    [InlineData(null, AttributeRules.CurrentVersion, false, false, false)]
    public async Task Battle_uses_saved_defense_only_when_valid_and_compatible(
        int? snapshotVersion,
        int liveVersion,
        bool isValid,
        bool isOutdated,
        bool usesSnapshot)
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var attacker = CreateCharacter("Attacker");
        var defender = CreateCharacter("Live defender");
        db.Characters.AddRange(attacker, defender);
        var tickets = new ArenaTicketStatus
        {
            CharacterId = attacker.Id,
            CurrentTickets = 5,
            LastTicketUpdate = DateTimeOffset.UtcNow
        };
        db.ArenaTicketStatus.Add(tickets);
        if (snapshotVersion.HasValue)
        {
            var snapshotId = Guid.NewGuid();
            db.ArenaDefenseSnapshots.Add(new ArenaDefenseSnapshot
            {
                CharacterId = defender.Id,
                CharacterSnapshotId = snapshotId,
                CharacterSnapshot = new CharacterSnapshot
                {
                    Id = snapshotId,
                    CharacterId = defender.Id,
                    Name = "Saved defender",
                    Level = defender.Level,
                    AttributeRulesVersion = snapshotVersion.Value,
                    BaseAttributes = defender.BaseAttributes.Select(attribute => new EntityAttributeSnapshot
                    {
                        CharacterSnapshotId = snapshotId,
                        AttributeType = attribute.AttributeType,
                        Value = attribute.Value
                    }).ToList()
                },
                IsValid = isValid,
                IsOutdated = isOutdated
            });
        }
        await db.SaveChangesAsync();

        var rules = new AttributeRulesSelection(liveVersion);
        var entities = new RecordingEntityService(attacker, defender);
        var setup = new CombatSetupService(null!, new EmptyEssenceLoadoutResolver(), null!, null!,
            attributeRules: rules);
        var service = new ColosseumService(
            entities,
            null!,
            new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup),
            new ColosseumRepository(db),
            new CombatEngineExecutor(new EmptyAbilityCatalogProvider()),
            new CombatEncounterResultFactory(),
            new RatingService(null!),
            null!, null!, null!, null!, null!,
            attributeRules: rules);

        // Exercise real preparation and compact playback, including the mixed-rules guard.
        var result = await service.StartArenaBattle(attacker.Id, defender.Id, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.Equal(usesSnapshot ? "Saved defender" : "Live defender",
            Assert.Single(result.CombatResult.EnemyTeam).Name);
        Assert.Equal(usesSnapshot ? new[] { attacker.Id } : new[] { attacker.Id, defender.Id },
            entities.RequestedIds);
        Assert.Equal(4, tickets.CurrentTickets);
        Assert.Single(await db.ColosseumMatches.ToListAsync());

        // Fallback must not relabel or overwrite historical snapshot data.
        if (snapshotVersion.HasValue)
        {
            db.ChangeTracker.Clear();
            var saved = await db.ArenaDefenseSnapshots.Include(x => x.CharacterSnapshot).SingleAsync();
            Assert.Equal(snapshotVersion.Value, saved.CharacterSnapshot.AttributeRulesVersion);
            Assert.Equal("Saved defender", saved.CharacterSnapshot.Name);
            Assert.Equal(isValid, saved.IsValid);
            Assert.Equal(isOutdated, saved.IsOutdated);
        }
    }

    private static Character CreateCharacter(string name)
    {
        var id = Guid.NewGuid();
        return new Character
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Name = name,
            Level = 1,
            BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(id),
            ArenaProfile = new CharacterArenaProfile { CharacterId = id }
        };
    }

    private sealed class RecordingEntityService(params Character[] characters) : IEntityService
    {
        public List<Guid> RequestedIds { get; } = [];

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds, CancellationToken cancellationToken)
        {
            RequestedIds.AddRange(entityIds);
            return Task.FromResult(characters.Where(character => entityIds.Contains(character.Id))
                .Cast<Entity>().ToList());
        }

        public void UpdateEntities(List<Entity> playerCharacters) => throw new NotSupportedException();
    }

    private sealed class EmptyEssenceLoadoutResolver : IEssenceCombatLoadoutResolver
    {
        public Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(Resolve(characterId, []));

        public EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences) =>
            new(characterId, [], [], new HashSet<string>());
    }

    private sealed class EmptyAbilityCatalogProvider : IAbilityCatalogProvider
    {
        public AbilityCatalog GetCatalog() => new([], [], [], new Dictionary<string, string>());
    }
}
