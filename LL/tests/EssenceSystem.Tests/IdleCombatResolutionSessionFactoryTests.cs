using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Combat;
using Domain.Models.CombatStyles;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Idle;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace EssenceSystem.Tests;

public sealed class IdleCombatResolutionSessionFactoryTests
{
    [Theory]
    [InlineData(CombatStyleIds.Bastion, 0, 1)]
    [InlineData(CombatStyleIds.Bastion, 2, 3)]
    [InlineData(CombatStyleIds.Conduit, 8, 9)]
    public void Idle_mastery_advancement_updates_the_next_encounter_bonus_without_changing_captured_rules(
        string styleId, int previousLevel, int nextLevel)
    {
        var catalog = CombatStyleFoundationTests.LoadCatalog();
        var definition = catalog.Styles.Single(style => style.Id == styleId);
        var character = new Character { Id = Guid.NewGuid(), Name = "Player" };
        var captured = CombatStyleRules.Snapshot(catalog, definition,
            new CharacterCombatStyle { Level = previousLevel }, new(styleId, null, [], null), null);
        var template = new CombatEntity(character) { CombatStyle = captured };
        var session = new IdleCombatResolutionSession(new UnusedCombatEngineExecutor(), new UnusedCombatEncounterResultFactory())
        {
            Catalog = new IdleCombatTemplateCatalog(
                new Dictionary<Guid, Entity> { [character.Id] = character },
                new Dictionary<Guid, CombatEntity> { [character.Id] = template },
                new Dictionary<Guid, CombatEntity>())
        };

        session.AdvanceCombatStyle(character.Id, nextLevel);

        var next = session.CapturedCombatStyles[character.Id];
        Assert.Equal(nextLevel, next.Level);
        Assert.Equal(nextLevel * .01, next.BarrierMasteryBonus, 6);
        Assert.Equal(nextLevel * .01, next.ChanneledMasteryBonus, 6);
        Assert.Equal(previousLevel, captured.Level);
        Assert.Equal(previousLevel * .01, captured.BarrierMasteryBonus, 6);
        Assert.Equal(previousLevel * .01, captured.ChanneledMasteryBonus, 6);
        Assert.Same(captured.Tuning, next.Tuning);
    }

    [Fact]
    public async Task Reuses_hostile_sources_and_templates_across_semantic_batches()
    {
        var character = new Character { Id = Guid.NewGuid(), Name = "Player", Level = 1 };
        var creature = new Creature { Id = Guid.NewGuid(), Name = "Creature", Level = 1 };
        var entities = new RecordingEntityService(character, creature);
        var setup = new RecordingCombatSetupService();
        var factory = new IdleCombatResolutionSessionFactory(
            entities,
            new CombatPreparationPipeline(new UnusedSnapshotCombatantBuilder(), setup),
            new UnusedCombatEngineExecutor(),
            new UnusedCombatEncounterResultFactory());
        var area = new Area
        {
            Id = "test-area",
            Creatures = [new AreaCreature { CreatureId = creature.Id }]
        };
        var now = DateTimeOffset.UtcNow;
        var plan = new IdleCombatPlan(
            character.Id,
            now,
            now,
            now,
            TimeSpan.FromSeconds(10),
            1,
            [character.Id],
            area,
            1);

        var first = Assert.IsType<IdleCombatResolutionSession>(
            await factory.CreateAsync(plan, CancellationToken.None));
        character.Level = 2;
        var second = Assert.IsType<IdleCombatResolutionSession>(
            await factory.CreateAsync(plan, CancellationToken.None));

        Assert.Equal(2, entities.RequestedIds.Count);
        Assert.Equal(
            new[] { character.Id, creature.Id }.Order().ToArray(),
            entities.RequestedIds[0].Order().ToArray());
        Assert.Equal([character.Id], entities.RequestedIds[1]);
        Assert.Equal([2, 1], setup.PreparedEntityCounts);
        Assert.Same(
            first.Catalog.HostileTemplatesBySourceEntityId[creature.Id],
            second.Catalog.HostileTemplatesBySourceEntityId[creature.Id]);
        Assert.Equal(2, second.Catalog.FriendlyTemplatesBySourceEntityId[character.Id].Level);
    }

    private sealed class RecordingEntityService(params Entity[] entities) : IEntityService
    {
        private readonly IReadOnlyDictionary<Guid, Entity> _entities = entities.ToDictionary(x => x.Id);

        public List<Guid[]> RequestedIds { get; } = [];

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken)
        {
            RequestedIds.Add(entityIds.ToArray());
            return Task.FromResult(entityIds.Select(id => _entities[id]).ToList());
        }

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }

    private sealed class RecordingCombatSetupService : ICombatSetupService
    {
        public List<int> PreparedEntityCounts { get; } = [];

        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) =>
            entities.Select(entity => new CombatEntity(entity)).ToList();

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) =>
            entities.Select(entity => new CombatEntity(entity)).ToList();

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
        {
        }

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities)
        {
            PreparedEntityCounts.Add(entities.Count);
            foreach (var entity in entities)
            {
                entity.BaseCombatAttributes[Domain.Models.Attributes.AttributeType.MaxHealth] = 1;
                entity.CombatAttributes[Domain.Models.Attributes.AttributeType.MaxHealth] = 1;
                entity.SyncCurrentHealthToMax();
            }
            return Task.CompletedTask;
        }

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedSnapshotCombatantBuilder : ISnapshotCombatantBuilder
    {
        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
            IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class UnusedCombatEngineExecutor : ICombatEngineExecutor
    {
        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedCombatEncounterResultFactory : ICombatEncounterResultFactory
    {
        public CombatEncounterResolutionResult Create(
            CombatEncounterRuntime runtime,
            CombatResult combatResult) =>
            throw new NotSupportedException();
    }
}
