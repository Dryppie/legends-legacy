using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.RegionBosses;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossCombatResolverTests
{
    [Fact]
    public async Task Resolve_builds_participants_from_their_signup_snapshots()
    {
        var characterId = Guid.NewGuid();
        var snapshot = new CharacterSnapshot
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Locked build",
            Level = 37,
            BaseAttributes =
            [
                new EntityAttributeSnapshot { AttributeType = AttributeType.MaxHealth, Value = 1_457 },
                new EntityAttributeSnapshot { AttributeType = AttributeType.Power, Value = 321 }
            ]
        };
        var creature = new Creature
        {
            Id = Guid.NewGuid(),
            Name = "The Mad King",
            BaseAttributesDict = new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 5_760,
                [AttributeType.Power] = 100
            }
        };
        var entities = new FixedEntityService(creature);
        var setup = new FixedCombatSetupService();
        var engine = new RecordingCombatEngineExecutor();
        var resolver = new RegionBossCombatResolver(
            entities,
            new CombatPreparationPipeline(new FixedSnapshotCombatantBuilder(setup), setup),
            engine,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero)));
        var run = new RegionBossRun
        {
            Id = Guid.NewGuid(),
            RandomSeed = 42,
            Members =
            [
                new RegionBossSignup
                {
                    CharacterId = characterId,
                    CharacterName = snapshot.Name,
                    CharacterSnapshotId = snapshot.Id,
                    CharacterSnapshot = snapshot,
                    PartySlot = 0
                }
            ]
        };
        var definition = new RegionBossDefinition
        {
            Id = "the-mad-king",
            CreatureId = creature.Id
        };

        await resolver.ResolveAsync(run, definition, CancellationToken.None);

        var participant = Assert.Single(engine.Runtime!.FriendlyParticipants);
        var source = Assert.IsType<Character>(participant.SourceEntity);
        Assert.Equal("Locked build", source.Name);
        Assert.Equal(1_457, participant.Combatant.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(321, participant.Combatant.GetAttributeValue(AttributeType.Power));
        Assert.Equal(37, participant.Combatant.Level);
        Assert.Equal([creature.Id], entities.RequestedIds);
    }

    private sealed class FixedSnapshotCombatantBuilder(ICombatSetupService setup) : ISnapshotCombatantBuilder
    {
        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
            IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CombatRuntimeParticipant> participants = requests.Select(request =>
            {
                var source = new Character
                {
                    Id = request.Snapshot.CharacterId,
                    Name = request.Snapshot.Name,
                    Level = request.Snapshot.Level,
                    BaseAttributes = request.Snapshot.BaseAttributes.Select(x => new EntityAttribute
                    {
                        AttributeType = x.AttributeType,
                        Value = x.Value
                    }).ToList()
                };
                var combatant = setup.CreatePlayerCombatEntities([source]).Single();
                combatant.Id = request.Slot.SlotId;
                combatant.OriginalId = request.Slot.SourceEntityId;
                return new CombatRuntimeParticipant(request.Slot, source, combatant);
            }).ToArray();
            return Task.FromResult(participants);
        }
    }

    private sealed class FixedEntityService(params Entity[] sourceEntities) : IEntityService
    {
        private readonly IReadOnlyDictionary<Guid, Entity> _sources = sourceEntities.ToDictionary(x => x.Id);

        public List<Guid> RequestedIds { get; } = [];

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken)
        {
            RequestedIds.AddRange(entityIds);
            return Task.FromResult(entityIds.Select(x => _sources[x]).ToList());
        }

        public void UpdateEntities(List<Entity> playerCharacters) => throw new NotSupportedException();
    }

    private sealed class FixedCombatSetupService : ICombatSetupService
    {
        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) =>
            entities.Select(x => new CombatEntity(x)).ToList();

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) =>
            entities.Cast<Creature>().Select(creature =>
            {
                var combatant = new CombatEntity(creature)
                {
                    BaseAttributes = creature.BaseAttributesDict.Select(x => new EntityAttribute
                    {
                        AttributeType = x.Key,
                        Value = x.Value
                    }).ToList()
                };
                return combatant;
            }).ToList();

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities) =>
            throw new NotSupportedException();

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities)
        {
            foreach (var entity in entities)
            {
                foreach (var attribute in entity.BaseAttributes)
                {
                    entity.BaseCombatAttributes[attribute.AttributeType] = attribute.Value;
                    entity.CombatAttributes[attribute.AttributeType] = attribute.Value;
                }
                entity.SyncCurrentHealthToMax();
            }
            return Task.CompletedTask;
        }

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingCombatEngineExecutor : ICombatEngineExecutor
    {
        public CombatEncounterRuntime? Runtime { get; private set; }

        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CombatExecutionWithCheckpoints> ExecuteRaidPlaybackAsync(
            CombatEncounterRuntime runtime,
            int checkpointIntervalTicks,
            CombatRuleset options,
            CancellationToken cancellationToken)
        {
            Runtime = runtime;
            var friendly = runtime.FriendlyParticipants.Select(ToSimple).ToArray();
            var hostile = runtime.HostileParticipants.Select(ToSimple).ToArray();
            var result = new CombatResult
            {
                PlayerTeam = [.. friendly],
                EnemyTeam = [.. hostile],
                Duration = 1
            };
            var checkpoint = new CombatCheckpoint(
                1,
                1,
                friendly,
                hostile,
                [],
                [],
                true,
                new CombatCheckpointContext(1, 0, []));
            return Task.FromResult(new CombatExecutionWithCheckpoints(result, [checkpoint]));
        }

        private static SimpleCombatEntity ToSimple(CombatRuntimeParticipant participant) => new()
        {
            Id = participant.Combatant.Id,
            Name = participant.Combatant.Name,
            Health = participant.Combatant.GetCurrentHealthValue(),
            MaxHealth = participant.Combatant.GetAttributeValue(AttributeType.MaxHealth),
            Level = participant.Combatant.Level
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
