using Domain.Models.Essences;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;

namespace EssenceSystem.Tests;

public sealed class IdleCombatExperienceSharingTests
{
    [Fact]
    public async Task Offline_batch_preserves_per_encounter_recipient_shares_and_grants_each_recipient_once()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var offline = new RecordingExperience();
        await Apply(offline, ids, [10, 10, 10]);

        var online = new RecordingExperience();
        for (var index = 0; index < 3; index++) await Apply(online, ids, [10]);

        Assert.Equal(3, offline.Awards.Count);
        Assert.Equal(new[] { 12, 9, 9 }, ids.Select(id => offline.TotalFor(id)));
        Assert.Equal(ids.Select(id => online.TotalFor(id)), ids.Select(id => offline.TotalFor(id)));
        Assert.Equal(30, offline.Awards.Sum(award => award.Experience));
        Assert.All(offline.Awards, award => Assert.Equal(EssenceCombatActivity.IdleCombat, award.Activity));
    }

    [Fact]
    public async Task Uses_already_calculated_rewards_without_reapplying_bonuses_or_redistributing_missing_styles()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var writer = new RecordingExperience();
        await Apply(writer, [ids[0], ids[1], ids[0], ids[2]], [11, 0, 5]);

        Assert.Equal(new[] { 6, 6, 4 }, ids.Select(id => writer.TotalFor(id)));
        Assert.Equal(16, writer.Awards.Sum(award => award.Experience));
        Assert.Equal(3, writer.Awards.Count);
    }

    [Fact]
    public async Task Solo_keeps_one_aggregate_grant_and_zero_rewards_are_not_granted()
    {
        var id = Guid.NewGuid();
        var writer = new RecordingExperience();
        await Apply(writer, [id], [10, 0, 20]);
        await Apply(writer, [id], [0]);

        var award = Assert.Single(writer.Awards);
        Assert.Equal(id, award.CharacterId);
        Assert.Equal(30, award.Experience);
    }

    private static async Task Apply(RecordingExperience writer, IReadOnlyList<Guid> recipients, IReadOnlyList<int> rewards)
    {
        var now = DateTimeOffset.UtcNow;
        var facts = new IdleCombatRewardFacts(recipients[0], now, now, now, TimeSpan.Zero,
            new Area { Id = "test" }, recipients, []);
        var encounters = rewards.Select((reward, index) => new IdleEncounterCalculatedOutcome(
            Guid.NewGuid(), index, 1, 10, reward, reward, 0, [])).ToArray();
        var outcome = new IdleCombatCalculatedOutcome(recipients[0], now, now, rewards.Sum(), 0, 0,
            [], [], [], [], [], encounters);
        await new IdleCombatRewardApplier(writer, null!, null!, null!).ApplyProgressionAsync(facts, outcome, default);
    }

    private sealed class RecordingExperience : IExperienceRewardWriter
    {
        public List<(Guid CharacterId, int Experience, EssenceCombatActivity Activity)> Awards { get; } = [];
        public int TotalFor(Guid id) => Awards.Where(award => award.CharacterId == id).Sum(award => award.Experience);

        public Task AddSplitExperienceAsync(IReadOnlyCollection<Guid> recipients, int experience, CancellationToken ct) =>
            AddSplitExperienceAsync(recipients, experience, EssenceCombatActivity.None, ct);

        public Task AddSplitExperienceAsync(IReadOnlyCollection<Guid> recipients, int experience, EssenceCombatActivity activity, CancellationToken ct)
        {
            var ids = recipients.Distinct().ToArray();
            for (var index = 0; index < ids.Length; index++)
                Awards.Add((ids[index], experience / ids.Length + (index < experience % ids.Length ? 1 : 0), activity));
            return Task.CompletedTask;
        }
    }
}
