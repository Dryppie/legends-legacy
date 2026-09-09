using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Bonuses;
using Domain.Models.CharacterActions;
using Domain.Models.Combat;
using Domain.Models.CombatStyles;
using Domain.Models.Entities;
using Domain.Models.Essences;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.Combat.Resolution.Idle;

namespace EssenceSystem.Tests;

public sealed class CombatStyleRewardTests
{
    [Fact]
    public async Task Offline_mastery_advances_only_between_encounters_without_character_bonus_or_inactive_training()
    {
        var id = Guid.NewGuid();
        var styles = new RecordingStyles();
        var session = new RecordingSession(new Dictionary<Guid, CombatStyleSnapshot> { [id] = Bastion() }, BattleOutcome.Victory);
        var bonus = new FixedBonus(0);
        var orchestrator = new IdleCombatOrchestrator(new FixedPlanner([id], 3), new FixedFactory(session),
            combatStyles: styles, bonuses: bonus, experienceBalance: new FixedExperience(60));

        var result = await orchestrator.OrchestrateAsync(new IdleCombatOrchestrationRequest(new CharacterAction { CharacterId = id }, DateTimeOffset.UtcNow), default);

        Assert.Equal(new[] { 0, 0, 1 }, session.FoughtLevels);
        Assert.Equal(180, styles.Awards.Sum(x => x.Xp));
        Assert.All(styles.Awards, x => Assert.Equal(CombatStyleIds.Bastion, x.Style));
        Assert.Equal(1, bonus.Reads);
        Assert.Equal(3, result.Encounters.Count);
        Assert.Equal(1, result.Encounters[1].Resolution.CombatStyleExperience.Single().Level);
    }

    [Theory]
    [InlineData(BattleOutcome.Defeat, 0, 0)]
    [InlineData(BattleOutcome.Defeat, 2500, 25)]
    [InlineData(BattleOutcome.Draw, 2500, 25)]
    [InlineData(BattleOutcome.Victory, 2500, 101)]
    public async Task Eligibility_and_retention_apply_to_unmodified_base_xp(BattleOutcome outcome, double retention, long expected)
    {
        var id = Guid.NewGuid();
        var styles = new RecordingStyles();
        var session = new RecordingSession(new Dictionary<Guid, CombatStyleSnapshot> { [id] = Bastion() }, outcome);
        var orchestrator = new IdleCombatOrchestrator(new FixedPlanner([id], 1), new FixedFactory(session),
            combatStyles: styles, bonuses: new FixedBonus(retention), experienceBalance: new FixedExperience(101));
        await orchestrator.OrchestrateAsync(new IdleCombatOrchestrationRequest(new CharacterAction { CharacterId = id }, DateTimeOffset.UtcNow), default);
        Assert.Equal(expected, styles.Awards.Single().Xp);
    }

    [Fact]
    public async Task Party_share_keeps_the_same_remainder_order_and_does_not_redistribute_a_no_style_share()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var styles = new RecordingStyles();
        var session = new RecordingSession(new Dictionary<Guid, CombatStyleSnapshot> { [ids[0]] = Bastion(), [ids[2]] = Bastion() }, BattleOutcome.Victory);
        var orchestrator = new IdleCombatOrchestrator(new FixedPlanner(ids, 1), new FixedFactory(session),
            combatStyles: styles, bonuses: new FixedBonus(0), experienceBalance: new FixedExperience(10));
        await orchestrator.OrchestrateAsync(new IdleCombatOrchestrationRequest(new CharacterAction { CharacterId = ids[0] }, DateTimeOffset.UtcNow), default);
        Assert.Equal(new long[] { 4, 3 }, styles.Awards.Select(x => x.Xp));
        Assert.Equal(new[] { ids[0], ids[2] }, styles.Awards.Select(x => x.Character));
    }

    [Fact]
    public async Task Legacy_no_style_session_does_not_load_progression_bonuses()
    {
        var id = Guid.NewGuid();
        var bonus = new FixedBonus(0);
        var styles = new RecordingStyles();
        var orchestrator = new IdleCombatOrchestrator(new FixedPlanner([id], 2), new FixedFactory(new RecordingSession([], BattleOutcome.Victory)),
            combatStyles: styles, bonuses: bonus, experienceBalance: new FixedExperience(100));
        await orchestrator.OrchestrateAsync(new IdleCombatOrchestrationRequest(new CharacterAction { CharacterId = id }, DateTimeOffset.UtcNow), default);
        Assert.Empty(styles.Awards);
        Assert.Equal(0, bonus.Reads);
    }

    private static CombatStyleSnapshot Bastion() => new() { CombatStyleId = CombatStyleIds.Bastion, Kind = CombatStyleKind.Bastion };

    private sealed class FixedFactory(ICombatResolutionSession session) : IIdleCombatResolutionSessionFactory
    {
        public Task<ICombatResolutionSession> CreateAsync(IdleCombatPlan plan, CancellationToken cancellationToken) => Task.FromResult(session);
    }

    private sealed class FixedPlanner(IReadOnlyList<Guid> recipients, int count) : IIdleCombatPlanner
    {
        public IdleCombatPlan CreatePlan(IdleCombatOrchestrationRequest request) =>
            new(recipients[0], request.Now, request.Now, request.Now, TimeSpan.FromSeconds(10), 1, recipients, new Area { Id = "test" }, count);
        public CombatEncounterPlan CreateEncounterPlan(IdleCombatPlan plan, int sequence, DateTimeOffset startsAt) =>
            new(Guid.NewGuid(), CombatMode.Idle, sequence, startsAt,
                recipients.Select(x => new CombatParticipantSlot(x.ToString(), x, CombatSide.Friendly))
                    .Append(new("enemy", Guid.NewGuid(), CombatSide.Hostile)).ToArray(),
                new IdleEncounterSourceContext(recipients[0], plan.Area, plan.EncounterCadence)) { ContentType = CombatContentType.Idle };
    }

    private sealed class RecordingSession(Dictionary<Guid, CombatStyleSnapshot> styles, BattleOutcome outcome) : ICombatResolutionSession
    {
        public IReadOnlyDictionary<Guid, Entity> SourceEntitiesById { get; } = new Dictionary<Guid, Entity>();
        public IReadOnlyDictionary<Guid, CombatStyleSnapshot> CapturedCombatStyles => styles;
        public List<int> FoughtLevels { get; } = [];
        public void AdvanceCombatStyle(Guid characterId, int level) => styles[characterId] = styles[characterId] with { Level = level };
        public Task<CombatEncounterResolutionResult> ResolveAsync(CombatEncounterPlan plan, CancellationToken cancellationToken)
        {
            FoughtLevels.Add(styles.Values.FirstOrDefault()?.Level ?? 0);
            return Task.FromResult(new CombatEncounterResolutionResult(plan.EncounterId, plan.Mode, plan.Sequence, plan.StartsAt, outcome,
                new CombatResult { Outcome = outcome }, [], []) { ContentType = CombatContentType.Idle });
        }
    }

    private sealed class FixedBonus(double retention) : IBonusService
    {
        public int Reads { get; private set; }
        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default)
        {
            Reads++;
            return ValueTask.FromResult<IReadOnlyDictionary<BonusKind, double>>(new Dictionary<BonusKind, double>
            { [BonusKind.CombatExperienceGainBps] = 10_000, [BonusKind.IdleCombatDefeatExperienceRetentionBps] = retention });
        }
    }

    private sealed class FixedExperience(int xp) : IAreaExperienceBalanceProvider
    {
        public decimal GetTargetExperiencePerHour(string areaId) => 0;
        public decimal GetTargetCindersPerHour(string areaId) => 0;
        public int CalculateEncounterExperience(string areaId, int creatureCount) => xp;
        public int CalculateEncounterCinders(string areaId, int creatureCount) => 0;
    }

    internal sealed class RecordingStyles : ICombatStyleService
    {
        public List<(Guid Character, string? Style, long Xp)> Awards { get; } = [];
        private readonly Dictionary<Guid, CharacterCombatStyle> _progress = [];
        public Task<CombatStyleXpGrantResult> GrantCapturedCombatXpAsync(Guid id, string? style, long xp, CancellationToken ct)
        {
            Awards.Add((id, style, xp));
            if (!_progress.TryGetValue(id, out var progress)) _progress[id] = progress = new() { CombatStyleId = style ?? "" };
            return Task.FromResult(CombatStyleProgression.Grant(progress, xp, [100, 200, 400, 700, 1100, 1600, 2300, 3200, 4400, 6000]));
        }
        public Task<CombatStyleOverview> GetOverviewAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<CombatStyleOverview> PreviewAsync(Guid id, CombatStyleSelectionRequest selection, CancellationToken ct) => throw new NotSupportedException();
        public Task<CombatStyleSnapshot?> ResolveAsync(Guid id, EssenceCombatActivity activity, CancellationToken ct, IReadOnlyList<PlayerEssence>? equippedEssences = null) => throw new NotSupportedException();
        public Task<CombatStyleOperationResult> SelectAsync(Guid id, CombatStyleSelectionRequest selection, CancellationToken ct) => throw new NotSupportedException();
    }
}
