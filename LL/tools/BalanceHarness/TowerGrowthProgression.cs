using System.Reflection;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Guilds;
using Application.UseCases.Characters.Events;
using Application.UseCases.Prophecies.Events;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using MediatR;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Rewards;
using Services.LL.Essences;
using Services.LL.Interfaces;
using Services.LL.Inventories;
using Services.LL.Levels;
using Services.LL.Regions;

namespace BalanceHarness;

public sealed record TowerGrowingEssence(Guid Id, string Definition, int Level, int CurrentXp, int AscensionTier);
public sealed record TowerGrowthState(int Level, long Experience, long IdleExperience, long ProphecyExperience,
    IReadOnlyList<TowerGrowingEssence> Essences);

/// <summary>Native XP writer, character leveling and activity-selected Essence training, using personal in-memory state.</summary>
public sealed partial class TowerGrowthProgression
{
    public Character Character { get; }
    public LevelingService Leveling { get; }
    public JsonAreaExperienceBalanceProvider Areas { get; }
    public List<CharacterLevelUpEvent> LevelEvents { get; } = [];
    public int LastEssenceExperience { get; private set; }
    public long IdleExperience { get; private set; }
    public long DungeonExperience { get; private set; }
    public Services.LL.Interfaces.Combat.Reward.IExperienceRewardWriter ExperienceWriter => writer;
    private readonly List<PlayerEssence> essences;
    private readonly EssenceLoadout loadout;
    private readonly CharacterExperienceRewardWriter writer;
    private int attuned;
    private readonly List<TowerEssenceAcquisition> acquisitions = [];

    public TowerGrowthProgression(string root, OfflineContent content, FixtureCharacter reference)
    {
        if (reference.Level != 30 || reference.Essences.Count != 4 || reference.Essences.Any(e => e.Level != 1 || e.AscensionTier != 0 || e.IsEvolved))
            throw new InvalidDataException("Growth qualification requires the declared level-30, four unascended level-1 Essences.");
        Character = reference.Materialize(content.Equipment); Character.UserId = Character.Id;
        essences = reference.MaterializeEssences().ToList();
        loadout = new() { CharacterId = Character.Id, PresetSlot = 1, AutoUseActivities = EssenceCombatActivity.IdleCombat | EssenceCombatActivity.Dungeon };
        Attune(3);
        var config = new ConfigurationBuilder().Build();
        Areas = new(config, root, HarnessJson.Options);
        var publisher = Boundary<IPublisher>((m,a) => {
            if (m.Name != "Publish") throw Unexpected(m);
            if (a[0] is CharacterLevelUpEvent level) LevelEvents.Add(level);
            else if (a[0] is ProphecyProgressNotification progress) LastEssenceExperience = checked(LastEssenceExperience + progress.ProgressEvent.Amount);
            else throw new InvalidDataException("Unexpected progression notification.");
            return Task.CompletedTask;
        });
        Leveling = new(publisher, new JsonCharacterExperienceProgressionProvider(config, root, HarnessJson.Options));
        var repository = Boundary<IEssenceRepository>((m,a) => m.Name == "GetLoadoutsWithSlotsAsync" && (Guid)a[0]! == Character.Id
            ? Task.FromResult(new List<EssenceLoadout> { loadout }) : throw Unexpected(m));
        var essenceService = new EssenceSystemService(repository, Boundary<IInventoryRepository>(), Boundary<IItemBaseRepository>(), content.Essences,
            Boundary<ICreatureEssenceLootTableRepository>(), new EssenceProgressionService(), new EssenceSlotUnlockService(), new EssenceLoadoutLimitService(),
            new InventoryItemFactory(), Boundary<IRandomProvider>(), Boundary<IGameEventOutbox>(), Boundary<IGuildMissionService>(), publisher);
        writer = new(Boundary<ICharacterService>((m,a) => m.Name == "GetCharacterByCharacterIdAsync" && (Guid)a[0]! == Character.Id
            ? Task.FromResult<Character?>(Character) : throw Unexpected(m)), Leveling,
            Boundary<IEntityService>((m,a) => m.Name == "UpdateEntities" ? null : throw Unexpected(m)), essenceService);
    }

    public void Attune(int count)
    {
        if (count is <3 or >5 || count > essences.Count || count < attuned || count > EssenceSlotProgression.GetUnlockedSlotCount(Character.Level))
            throw new InvalidDataException("Illegal or regressing personal loadout.");
        attuned = count;
        loadout.Slots = essences.Take(count).Select((e,i) => new EssenceLoadoutSlot { SlotIndex = i, PlayerEssenceId = e.Id, PlayerEssence = e }).ToArray();
    }

    public async Task AwardIdle(int amount, CancellationToken ct)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        LastEssenceExperience = 0;
        await writer.AddSplitExperienceAsync([Character.Id], amount, EssenceCombatActivity.IdleCombat, ct);
        IdleExperience = checked(IdleExperience + amount);
    }
    public void BeginDungeonClaim() => LastEssenceExperience = 0;
    public void RecordDungeonClaim(int amount) => DungeonExperience = checked(DungeonExperience + amount);
    public TowerGrowthState State(long prophecyExperience) => new(Character.Level, Character.Experience, IdleExperience, prophecyExperience,
        essences.Take(attuned).Select(e => new TowerGrowingEssence(e.Id,e.EssenceDefinitionId,e.Level,e.CurrentXp,e.AscensionTier)).ToArray());
    public FixtureCharacter Snapshot(FixtureCharacter equipment) => equipment with {
        Level = Character.Level, BaseAttributes = Character.BaseAttributes.ToDictionary(a => a.AttributeType,a => a.Value),
        Essences = essences.Take(attuned).Select(e => new FixtureEssence(e.EssenceDefinitionId,e.Level,e.AscensionTier,e.IsEvolved,
            acquisitions.Any(a=>a.EssenceId==e.Id)?e.Id:null)).ToArray()
    };
    public static T Boundary<T>(Func<MethodInfo, object?[], object?>? call = null) where T : class
    {
        var proxy = DispatchProxy.Create<T, FileBoundary>();
        ((FileBoundary)(object)proxy).Call = call ?? ((m,_) => throw Unexpected(m)); return proxy;
    }
    private static Exception Unexpected(MethodInfo m) => new InvalidOperationException("Unmodeled growth boundary: " + m.DeclaringType?.Name + "." + m.Name);
    public class FileBoundary : DispatchProxy
    {
        internal Func<MethodInfo, object?[], object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!,args ?? []);
    }
}
