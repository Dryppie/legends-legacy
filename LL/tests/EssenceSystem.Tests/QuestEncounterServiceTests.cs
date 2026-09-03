using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed class QuestEncounterServiceTests
{
    [Fact]
    public async Task StartAsync_gives_the_training_enemy_ten_health_without_changing_the_creature()
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Name = "New adventurer",
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 140 },
                new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 }
            ]
        };
        var creature = new Creature
        {
            Id = Guid.NewGuid(),
            Name = "Skeleton",
            BaseAttributesDict = new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 96,
                [AttributeType.Power] = 10
            }
        };
        var option = new QuestChoiceOptionDefinition
        {
            Key = "skeleton",
            CreatureId = creature.Id,
            EncounterKey = "training"
        };
        var executor = new CapturingCombatEngineExecutor();
        var setup = new FixedCombatSetupService();
        var service = new QuestEncounterService(
            new FixedEntityService(character, creature),
            new FixedAreaService(),
            new CombatPreparationPipeline(new UnusedSnapshotCombatantBuilder(), setup),
            executor,
            new PassthroughCombatEncounterResultFactory(),
            new AllowedCombatAreaAccessService(),
            new FixedQuestRepository(character.Id, option.Key),
            new FixedQuestDefinitionProvider(option),
            new EmptyQuestProgressionService());

        var result = await service.StartAsync(
            character.Id,
            QuestConstants.TrainingDay,
            option.EncounterKey,
            CancellationToken.None);

        Assert.NotNull(result);
        var enemy = Assert.Single(executor.Runtime!.HostileParticipants).Combatant;
        Assert.Equal(10, enemy.GetAttributeValue(AttributeType.MaxHealth));
        Assert.Equal(10, enemy.GetCurrentHealthValue());
        Assert.Equal(96, creature.BaseAttributesDict[AttributeType.MaxHealth]);
    }

    private sealed class UnusedSnapshotCombatantBuilder : ISnapshotCombatantBuilder
    {
        public Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
            IReadOnlyList<SnapshotCombatantRequest> requests,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedEntityService(params Entity[] entities) : IEntityService
    {
        private readonly IReadOnlyDictionary<Guid, Entity> _entities = entities.ToDictionary(x => x.Id);

        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(entityIds.Select(id => _entities[id]).ToList());

        public void UpdateEntities(List<Entity> playerCharacters) => throw new NotSupportedException();
    }

    private sealed class FixedAreaService : IAreaService
    {
        public Task<Area?> GetAreaByIdAsync(string id) => Task.FromResult<Area?>(new Area
        {
            Id = QuestConstants.TrainingGroundsAreaId,
            Name = "Training Grounds"
        });

        public Task<IReadOnlyList<Area>> GetAllAreasAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedCombatSetupService : ICombatSetupService
    {
        public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities) =>
            entities.Select(entity => new CombatEntity(entity)
            {
                BaseAttributes = [.. entity.BaseAttributes]
            }).ToList();

        public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area) =>
            entities.Cast<Creature>().Select(creature => new CombatEntity(creature)
            {
                BaseAttributes = creature.BaseAttributesDict.Select(attribute => new EntityAttribute
                {
                    AttributeType = attribute.Key,
                    Value = attribute.Value
                }).ToList()
            }).ToList();

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

        public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities) =>
            throw new NotSupportedException();

        public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingCombatEngineExecutor : ICombatEngineExecutor
    {
        public CombatEncounterRuntime? Runtime { get; private set; }

        public Task<CombatResult> ExecuteAsync(
            CombatEncounterRuntime runtime,
            CancellationToken cancellationToken)
        {
            Runtime = runtime;
            return Task.FromResult(new CombatResult
            {
                Outcome = BattleOutcome.Victory,
                PlayerTeam = runtime.FriendlyParticipants.Select(ToSimple).ToList(),
                EnemyTeam = runtime.HostileParticipants.Select(ToSimple).ToList()
            });
        }

        private static SimpleCombatEntity ToSimple(CombatRuntimeParticipant participant) => new()
        {
            Id = participant.Combatant.Id,
            Name = participant.Combatant.Name,
            Health = participant.Combatant.GetCurrentHealthValue(),
            MaxHealth = participant.Combatant.GetAttributeValue(AttributeType.MaxHealth)
        };
    }

    private sealed class PassthroughCombatEncounterResultFactory : ICombatEncounterResultFactory
    {
        public CombatEncounterResolutionResult Create(
            CombatEncounterRuntime runtime,
            CombatResult combatResult) => new(
                runtime.Plan.EncounterId,
                runtime.Plan.Mode,
                runtime.Plan.Sequence,
                runtime.Plan.StartsAt,
                combatResult.Outcome,
                combatResult,
                combatResult.PlayerTeam,
                combatResult.EnemyTeam)
            {
                ContentType = runtime.Plan.ContentType
            };
    }

    private sealed class AllowedCombatAreaAccessService : ICombatAreaAccessService
    {
        public Task<CombatAreaAccessResult> GetAccessAsync(
            Guid characterId,
            string areaId,
            CancellationToken cancellationToken) => Task.FromResult(new CombatAreaAccessResult(
                areaId,
                true,
                true,
                1,
                1,
                [],
                [],
                null,
                true,
                null,
                null));

        public Task<IReadOnlyList<CombatAreaAccessResult>> GetAllAccessAsync(
            Guid characterId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedQuestRepository(Guid characterId, string selectedOptionKey) : IQuestRepository
    {
        public Task<CharacterQuestProgress?> GetProgressAsync(
            Guid requestedCharacterId,
            string questId,
            CancellationToken cancellationToken) => Task.FromResult<CharacterQuestProgress?>(new CharacterQuestProgress
            {
                CharacterId = characterId,
                QuestId = QuestConstants.TrainingDay,
                DefinitionVersion = 4,
                Status = QuestStatus.Active,
                SelectedOptionKey = selectedOptionKey
            });

        public Task<IReadOnlyList<CharacterQuestProgress>> GetProgressesAsync(
            Guid requestedCharacterId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int?> GetCharacterLevelAsync(Guid requestedCharacterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> HasProcessedEventAsync(Guid outboxMessageId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlySet<string>> GetOwnedEssenceDefinitionIdsAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> HasEssenceInAnyLoadoutAsync(
            Guid requestedCharacterId,
            string essenceDefinitionId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> HasAnyEssenceInLoadoutAsync(
            Guid requestedCharacterId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> HasQualifyingEquipmentEquippedAsync(
            Guid requestedCharacterId,
            IReadOnlyCollection<string> itemBaseIds,
            int? tier,
            bool mustBeCrafted,
            bool toolSlotOnly,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlySet<string>> GetCraftedRecipeIdsAsync(
            Guid requestedCharacterId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void AddProgress(CharacterQuestProgress progress) => throw new NotSupportedException();
        public void AddEventLedger(QuestEventLedger ledger) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedQuestDefinitionProvider(QuestChoiceOptionDefinition option)
        : IQuestDefinitionProvider
    {
        private readonly QuestDefinition _definition = new()
        {
            Id = QuestConstants.TrainingDay,
            Version = 4,
            Choice = new QuestChoiceDefinition { Options = [option] }
        };

        public QuestDefinition Get(string questId, int? version = null) => _definition;
        public IReadOnlyList<QuestDefinition> GetAll() => [_definition];

        public bool TryGet(string questId, out QuestDefinition definition)
        {
            definition = _definition;
            return true;
        }
    }

    private sealed class EmptyQuestProgressionService : IQuestProgressionService
    {
        public Task<QuestProgressionResult> ProcessAsync(
            Guid characterId,
            QuestTrigger trigger,
            Guid? outboxMessageId,
            string eventType,
            CancellationToken cancellationToken) => Task.FromResult(new QuestProgressionResult(
                new QuestJournal([], null),
                [],
                []));
    }
}
