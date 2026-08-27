using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace EssenceSystem.Tests;

public sealed class CombatPreparationPipelineTests
{
    [Fact]
    public async Task Prepares_mixed_sources_once_in_request_order_with_stable_identity()
    {
        var liveCharacter = new Character { Id = Guid.NewGuid(), Name = "Live", Level = 5 };
        var snapshot = new CharacterSnapshot
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            Name = "Snapshot",
            Level = 7
        };
        var creature = new Creature { Id = Guid.NewGuid(), Name = "Creature", Level = 9 };
        var area = new Area { Id = "pipeline-area", DifficultyTier = 4 };
        var setup = new RecordingCombatSetupService();
        var snapshots = new RecordingSnapshotCombatantBuilder();
        var pipeline = new CombatPreparationPipeline(snapshots, setup);
        var requests = new[]
        {
            CreateRequest("live-slot", liveCharacter.Id, CombatSide.Friendly,
                new LiveCombatantPreparationSource(liveCharacter)),
            CreateRequest("snapshot-slot", snapshot.CharacterId, CombatSide.Friendly,
                new SnapshotCombatantPreparationSource(snapshot)),
            CreateRequest("creature-slot", creature.Id, CombatSide.Hostile,
                new LiveCombatantPreparationSource(creature, area))
        };

        var participants = await pipeline.PrepareAsync(
            CombatContentType.Tournament,
            requests,
            CancellationToken.None);

        Assert.Equal(["live-slot", "snapshot-slot", "creature-slot"],
            participants.Select(x => x.Slot.SlotId));
        Assert.Equal(participants.Select(x => x.Slot.SlotId),
            participants.Select(x => x.Combatant.Id));
        Assert.Equal(participants.Select(x => x.Slot.SourceEntityId),
            participants.Select(x => x.Combatant.OriginalId));
        Assert.All(participants, participant =>
        {
            Assert.Contains("configured-before", participant.Combatant.NativeAbilityIds);
            Assert.Equal(40, participant.Combatant.GetCurrentHealthValue());
        });
        Assert.Equal(1, setup.PrepareCallCount);
        Assert.Equal(EssenceCombatActivity.Tournament, setup.PreparedActivity);
        Assert.True(setup.AllCombatantsConfiguredBeforePreparation);
        Assert.Same(area, setup.CreatureArea);
        Assert.Single(snapshots.Requests);
        Assert.Equal(snapshot.CharacterId, snapshots.Requests[0].Snapshot.CharacterId);
    }

    [Fact]
    public async Task Rejects_a_live_creature_without_an_explicit_scaling_area()
    {
        var creature = new Creature { Id = Guid.NewGuid(), Name = "Creature" };
        var pipeline = new CombatPreparationPipeline(
            new RecordingSnapshotCombatantBuilder(),
            new RecordingCombatSetupService());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.PrepareAsync(
            CombatContentType.Raid,
            [new CombatantPreparationRequest(
                new CombatParticipantSlot("creature-slot", creature.Id, CombatSide.Hostile),
                new LiveCombatantPreparationSource(creature))],
            CancellationToken.None));

        Assert.Contains("explicit scaling area", exception.Message);
    }

    [Fact]
    public async Task Rejects_duplicate_runtime_slots_before_building_combatants()
    {
        var first = new Character { Id = Guid.NewGuid(), Name = "First" };
        var second = new Character { Id = Guid.NewGuid(), Name = "Second" };
        var setup = new RecordingCombatSetupService();
        var pipeline = new CombatPreparationPipeline(new RecordingSnapshotCombatantBuilder(), setup);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.PrepareAsync(
            CombatContentType.Arena,
            [
                new CombatantPreparationRequest(
                    new CombatParticipantSlot("same-slot", first.Id, CombatSide.Friendly),
                    new LiveCombatantPreparationSource(first)),
                new CombatantPreparationRequest(
                    new CombatParticipantSlot("SAME-SLOT", second.Id, CombatSide.Hostile),
                    new LiveCombatantPreparationSource(second))
            ],
            CancellationToken.None));

        Assert.Contains("duplicated", exception.Message);
        Assert.Equal(0, setup.PrepareCallCount);
    }

    private static CombatantPreparationRequest CreateRequest(
        string slotId,
        Guid sourceId,
        CombatSide side,
        CombatantPreparationSource source) => new(
            new CombatParticipantSlot(slotId, sourceId, side),
            source,
            combatant => combatant.NativeAbilityIds.Add("configured-before"),
            combatant => combatant.SetCurrentHealth(40));

    private sealed class RecordingSnapshotCombatantBuilder : ISnapshotCombatantBuilder
    {
        public IReadOnlyList<SnapshotCombatantRequest> Requests { get; private set; } = [];

        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
            IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken)
        {
            Requests = requests;
            return Task.FromResult<IReadOnlyList<CombatRuntimeParticipant>>(requests.Select(request =>
            {
                var source = new Character
                {
                    Id = request.Snapshot.CharacterId,
                    Name = request.Snapshot.Name,
                    Level = request.Snapshot.Level
                };
                var combatant = new CombatEntity(source)
                {
                    Id = request.Slot.SlotId,
                    OriginalId = request.Slot.SourceEntityId
                };
                return new CombatRuntimeParticipant(request.Slot, source, combatant);
            }).ToArray());
        }
    }

    private sealed class RecordingCombatSetupService : ICombatSetupService
    {
        public int PrepareCallCount { get; private set; }
        public EssenceCombatActivity? PreparedActivity { get; private set; }
        public bool AllCombatantsConfiguredBeforePreparation { get; private set; }
        public Area? CreatureArea { get; private set; }

        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) =>
            entities.Select(entity => new CombatEntity(entity)).ToList();

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area)
        {
            CreatureArea = area;
            return entities.Select(entity => new CombatEntity(entity)).ToList();
        }

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
        {
        }

        public Task PrepareEntitiesForCombat(List<CombatEntity> entities) =>
            PrepareEntitiesForCombat(entities, EssenceCombatActivity.IdleCombat);

        public Task PrepareEntitiesForCombat(
            List<CombatEntity> entities,
            EssenceCombatActivity activity)
        {
            PrepareCallCount++;
            PreparedActivity = activity;
            AllCombatantsConfiguredBeforePreparation = entities.All(x =>
                x.NativeAbilityIds.Contains("configured-before"));
            foreach (var entity in entities)
            {
                entity.BaseCombatAttributes[AttributeType.MaxHealth] = 100;
                entity.CombatAttributes[AttributeType.MaxHealth] = 100;
                entity.SyncCurrentHealthToMax();
            }
            return Task.CompletedTask;
        }

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) =>
            throw new NotSupportedException();
    }
}
