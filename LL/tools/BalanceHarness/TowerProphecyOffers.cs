using System.Text.Json;
using Domain.Models.Items;
using Domain.Models.Prophecies;

namespace BalanceHarness;

public sealed record TowerProphecyOffer(string Definition, string Slot, string Objective, int Target,
    int Progress, string Status, DateTimeOffset? AcceptedAt, ProphecyRewardSnapshot Reward);
public sealed record TowerProphecyOfferDay(DateTimeOffset Day, IReadOnlyList<TowerProphecyOffer> Daily,
    TowerProphecyOffer Weekly, string? Selected, string ChoiceReason,
    IReadOnlyList<TowerProphecyOffer> FinalDaily, TowerProphecyOffer FinalWeekly);
public sealed record TowerProphecyOfferPlan(string Version, string Policy, DateTimeOffset Epoch, int CadenceSeconds,
    IReadOnlyList<int> Checkpoints, string Assumptions);

/// <summary>Production offer generation and acceptance; fixed visible choice, no rerolls or outcome lookahead.</summary>
public static class TowerProphecyOffers
{
    public const string Version = "tower-prophecy-offers-v1";
    public const string Policy = "offered-kills-no-reroll";
    public static TowerProphecyOfferPlan Read(string fixtures)
    {
        var p = HarnessJson.Read<TowerProphecyOfferPlan>(Path.Combine(fixtures,"tower-prophecy-offers.json"));
        if (p.Version != Version || p.Policy != Policy || p.CadenceSeconds != 10
            || p.Epoch != new DateTimeOffset(2026,9,28,0,0,0,TimeSpan.Zero)
            || !p.Checkpoints.SequenceEqual(new[] {2160,8640,25920,86400}) || string.IsNullOrWhiteSpace(p.Assumptions))
            throw new InvalidDataException("Changed native offer schedule requires a new declaration.");
        return p;
    }
    public static TowerSourcePolicyPlan ReadCombat(string fixtures)
    {
        var p = HarnessJson.Read<TowerSourcePolicyPlan>(Path.Combine(fixtures,"tower-prophecy-combat.json"));
        if (p.Version != Version || p.ActivityVersion != TowerActivityInventory.Version
            || !p.Policies.SequenceEqual(new[] {"no-prophecy-credit", Policy}) || p.PanelIndex != "family-attempt-ordinal"
            || string.IsNullOrWhiteSpace(p.Assumptions)) throw new InvalidDataException("Changed prophecy combat declaration.");
        Read(fixtures); return p;
    }
    public static TowerProphecyOffer Snapshot(PlayerProphecyInstance p) => new(p.ProphecyDefinitionId,p.SlotType.ToString(),
        p.ProphecyDefinition!.ObjectiveType,p.TargetValue,p.CurrentValue,p.Status.ToString(),p.AcceptedAt,
        JsonSerializer.Deserialize<ProphecyRewardSnapshot>(p.RewardSnapshotJson,new JsonSerializerOptions(JsonSerializerDefaults.Web))!);
    public static PlayerProphecyInstance? Choose(IReadOnlyList<PlayerProphecyInstance> offers) => offers
        .Where(p => p.Status == ProphecyStatus.Offered && p.ProphecyDefinition!.ObjectiveType == ProphecyObjectiveType.KillCreatures)
        .OrderByDescending(p => Snapshot(p).Reward.SigilFragments).ThenBy(p => p.TargetValue)
        .ThenBy(p => p.ProphecyDefinitionId,StringComparer.Ordinal).FirstOrDefault();

    public static async Task<TowerEntrySourceSchedule> Schedule(string root, Guid owner, string outcome,
        int horizon, TowerProphecyOfferPlan plan, CancellationToken ct)
    {
        if (!plan.Checkpoints.Contains(horizon)) throw new InvalidDataException("Undeclared offer horizon.");
        var model = new TowerEntrySources(root,owner); var checkpoints = new List<TowerEntrySourceCheckpoint>();
        var days = new List<TowerProphecyOfferDay>(); var from = 0; var dayLength = 86400 / plan.CadenceSeconds;
        while (from < horizon)
        {
            var day = plan.Epoch.AddSeconds((long)from * plan.CadenceSeconds);
            var endOfDay = Math.Min(horizon,from + dayLength);
            var overview = await model.Overview(day,ct);
            var daily = overview.DailyProphecies.Select(Snapshot).ToArray(); var weekly = Snapshot(overview.GreaterProphecy);
            var replay = await model.Overview(day,ct);
            if (HarnessJson.Hash(daily) != HarnessJson.Hash(replay.DailyProphecies.Select(Snapshot).ToArray())
                || HarnessJson.Hash(weekly) != HarnessJson.Hash(Snapshot(replay.GreaterProphecy))) throw new InvalidDataException("Offer read rerolled definitions.");
            var choice = Choose(overview.DailyProphecies);
            if (choice is not null)
            {
                if (!await model.AcceptGeneratedOffer(choice.Id,day,ct)) throw new InvalidDataException("Offered choice was rejected.");
                var other = overview.DailyProphecies.First(p => p.Id != choice.Id);
                if (await model.AcceptGeneratedOffer(other.Id,day,ct)) throw new InvalidDataException("Second daily choice accepted.");
            }
            foreach (var until in plan.Checkpoints.Where(c => from < c && c < endOfDay).Append(endOfDay).Distinct().Order())
            {
                while (from < until)
                {
                    ct.ThrowIfCancellationRequested();
                    var active = overview.DailyProphecies.Append(overview.GreaterProphecy).Where(p => p.Status == ProphecyStatus.Accepted
                        && p.ProphecyDefinition!.ObjectiveType is ProphecyObjectiveType.KillCreatures or ProphecyObjectiveType.WinEncounters).ToArray();
                    // Every credited win has one enemy. Other objectives receive no fabricated activity.
                    if (active.Any(p => p.ProphecyDefinition!.ObjectiveType == ProphecyObjectiveType.WinEncounters && p.ObjectiveParameterSnapshotJson != "{}"))
                        throw new InvalidDataException("Review changed weekly win requirements.");
                    var need = active.Select(p => p.TargetValue - p.CurrentValue).DefaultIfEmpty(int.MaxValue).Min();
                    var end = until;
                    if (TowerEntrySourceStudy.Victories(outcome,until) - TowerEntrySourceStudy.Victories(outcome,from) >= need)
                    {
                        var low = from + 1; var high = until;
                        while (low < high) { var mid = (low + high) / 2; if (TowerEntrySourceStudy.Victories(outcome,mid) - TowerEntrySourceStudy.Victories(outcome,from) >= need) high = mid; else low = mid + 1; }
                        end = low;
                    }
                    var wins = TowerEntrySourceStudy.Victories(outcome,end) - TowerEntrySourceStudy.Victories(outcome,from);
                    var lastVictory = outcome == "four-of-five" && end % 5 == 0 ? end - 1 : end;
                    var occurred = plan.Epoch.AddSeconds((long)(lastVictory - 1) * plan.CadenceSeconds);
                    if (wins > 0) { await model.TrackKills(wins,occurred,ct); await model.TrackWins(wins,occurred,ct); }
                    foreach (var p in overview.DailyProphecies.Append(overview.GreaterProphecy).Where(p => p.Status == ProphecyStatus.Completed))
                        if (!await model.Claim(p,occurred,ct)) throw new InvalidDataException("Native completion claim rejected.");
                    await model.ClaimMilestones(occurred,ct); from = end;
                }
                if (!plan.Checkpoints.Contains(until)) continue;
                var fragments = model.State().Items.GetValueOrDefault(SigilFragmentItem.ItemBaseId); var quantity = fragments / model.FragmentCost;
                if (quantity > 0 && !await model.Assemble("goblin_mines",quantity,ct)) throw new InvalidDataException("Funded native assembly failed.");
                checkpoints.Add(new(until,TowerEntrySourceStudy.Victories(outcome,until),model.Claims.Count,fragments,quantity,model.State()));
            }
            days.Add(new(day,daily,weekly,choice?.ProphecyDefinitionId,choice is null ? "NoOfferedKillObjective" : "HighestFragmentsThenLowestTargetThenId",
                overview.DailyProphecies.Select(Snapshot).ToArray(),Snapshot(overview.GreaterProphecy)));
        }
        return new(model.Claims.ToArray(),checkpoints,model.DuplicateClaimsRejected,days);
    }
}
