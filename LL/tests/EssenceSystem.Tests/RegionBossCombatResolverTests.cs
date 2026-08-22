using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.RegionBosses;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossCombatResolverTests
{
    [Fact]
    public async Task Resolve_builds_participants_from_their_current_characters()
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Name = "Current build",
            Level = 37,
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 1_457 },
                new EntityAttribute { AttributeType = AttributeType.Power, Value = 321 }
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
        var entities = new FixedEntityService(character, creature);
        var setup = new FixedCombatSetupService();
        var engine = new RecordingCombatEngineExecutor();
        var resolver = new RegionBossCombatResolver(
            entities,
            setup,
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
                    CharacterId = character.Id,
                    CharacterName = character.Name,
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
        Assert.Same(character, participant.SourceEntity);
        Assert.Equal(1_457, participant.Combatant.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(321, participant.Combatant.GetAttributeValue(AttributeType.Power));
        Assert.Equal(37, participant.Combatant.Level);
        Assert.Equal([character.Id, creature.Id], entities.RequestedIds);
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
            CombatSimulationOptions options,
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
