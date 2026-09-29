using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.Items;
using Domain.Models.Prophecies;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Interfaces.Combat.Resolution;

namespace BalanceHarness;

public sealed record TowerEntrySourcePlan(string Version, DateTimeOffset Epoch, int CadenceSeconds,
    IReadOnlyList<int> Checkpoints, IReadOnlyList<string> Policies, string DailyDefinition, string WeeklyDefinition,
    string AssemblyDungeon, string Assumptions);
public sealed record TowerEntrySourceCheckpoint(int Encounter, int Victories, int Claims, int FragmentsBeforeAssembly,
    int AssembledNow, TowerEntrySourceState State);
public sealed record TowerEntrySourceSchedule(IReadOnlyList<TowerEntrySourceClaim> Claims,
    IReadOnlyList<TowerEntrySourceCheckpoint> Checkpoints, int DuplicateClaimsRejected,
    IReadOnlyList<TowerProphecyOfferDay>? Offers = null);
public sealed record TowerEntrySourceRequest(string ApiRoot, string Fixtures, string Archive, string Output,
    IReadOnlyDictionary<string,string> InputHashes);

public static class TowerEntrySourceStudy
{
    public const string Version = "tower-entry-sources-v1";
    public const string ArchivePin = "3b4f4870dfc8e10f9b0056788eb006b394ad7618dca963cba1d48d204a453fe7";
    public static TowerEntrySourcePlan Read(string fixtures)
    {
        var plan = HarnessJson.Read<TowerEntrySourcePlan>(Path.Combine(fixtures, "tower-entry-sources.json"));
        if (plan.Version != Version || plan.Epoch != new DateTimeOffset(2026,9,28,0,0,0,TimeSpan.Zero)
            || plan.CadenceSeconds != 10 || !plan.Checkpoints.SequenceEqual(new[] {2160,8640,25920,86400})
            || !plan.Policies.SequenceEqual(new[] {"daily-common", "daily-common-weekly-kills"})
            || plan.DailyDefinition != "daily.combat.kills.common" || plan.WeeklyDefinition != "weekly.combat.kills"
            || plan.AssemblyDungeon != "goblin_mines" || string.IsNullOrWhiteSpace(plan.Assumptions))
            throw new InvalidDataException("Changed source accounting requires a new declaration.");
        return plan;
    }
    public static int Victories(string outcome, int encounters) => outcome switch {
        "perfect" => encounters, "four-of-five" => encounters - encounters / 5,
        _ => throw new InvalidDataException("Unknown idle outcome.")
    };
    public static async Task<TowerEntrySourceSchedule> Schedule(string root, Guid owner, string outcome,
        string policy, int horizon, TowerEntrySourcePlan plan, CancellationToken ct)
    {
        if (!plan.Policies.Contains(policy) || !plan.Checkpoints.Contains(horizon)) throw new InvalidDataException("Unsupported source schedule.");
        var model = new TowerEntrySources(root, owner);
        var active = new List<PlayerProphecyInstance>(); var checkpoints = new List<TowerEntrySourceCheckpoint>();
        var dayLength = 86400 / plan.CadenceSeconds; var from = 0;
        var boundaries = plan.Checkpoints.Concat(Enumerable.Range(1, horizon / dayLength).Select(d => d * dayLength)).Where(i => i <= horizon).Distinct().Order();
        foreach (var until in boundaries)
        {
            if (from % dayLength == 0)
            {
                var day = plan.Epoch.AddSeconds((long)from * plan.CadenceSeconds);
                active.Add(model.AcceptConditionalOffer(plan.DailyDefinition, day, day));
                if (policy == "daily-common-weekly-kills" && day.DayOfWeek == DayOfWeek.Monday)
                    active.Add(model.AcceptConditionalOffer(plan.WeeklyDefinition, day, day));
            }
            while (from < until)
            {
                ct.ThrowIfCancellationRequested();
                var at = plan.Epoch.AddSeconds((long)from * plan.CadenceSeconds);
                var need = active.Where(p => p.Status == ProphecyStatus.Accepted && p.PeriodStart <= at && p.PeriodEnd > at)
                    .Select(p => p.TargetValue - p.CurrentValue).DefaultIfEmpty(int.MaxValue).Min();
                var end = until;
                if (Victories(outcome, until) - Victories(outcome, from) >= need)
                {
                    var low = from + 1; var high = until;
                    while (low < high) { var mid = (low + high) / 2; if (Victories(outcome, mid) - Victories(outcome, from) >= need) high = mid; else low = mid + 1; }
                    end = low;
                }
                var kills = Victories(outcome, end) - Victories(outcome, from);
                var lastVictory = outcome == "four-of-five" && end % 5 == 0 ? end - 1 : end;
                var occurred = plan.Epoch.AddSeconds((long)(lastVictory - 1) * plan.CadenceSeconds);
                if (kills > 0) await model.TrackKills(kills, occurred, ct);
                foreach (var instance in active.Where(p => p.Status == ProphecyStatus.Completed))
                    if (!await model.Claim(instance, occurred, ct)) throw new InvalidDataException("Completed native claim failed.");
                await model.ClaimMilestones(occurred, ct);
                from = end;
            }
            if (!plan.Checkpoints.Contains(until)) continue;
            var fragments = model.State().Items.GetValueOrDefault(SigilFragmentItem.ItemBaseId);
            var quantity = fragments / model.FragmentCost;
            if (quantity > 0 && !await model.Assemble(plan.AssemblyDungeon, quantity, ct)) throw new InvalidDataException("Funded native assembly failed.");
            checkpoints.Add(new(until, Victories(outcome, until), model.Claims.Count, fragments, quantity, model.State()));
        }
        return new(model.Claims.ToArray(), checkpoints, model.DuplicateClaimsRejected);
    }

    public static Task Run(TowerEntrySourceRequest request, CancellationToken ct) => RunCore(request,ct,false);
    public static Task RunOffers(TowerEntrySourceRequest request, CancellationToken ct) => RunCore(request,ct,true);
    private static async Task RunCore(TowerEntrySourceRequest request, CancellationToken ct, bool nativeOffers)
    {
        if (Directory.Exists(request.Output)) throw new InvalidDataException("Fresh output required.");
        void Verify() { foreach (var (path,pin) in request.InputHashes) if (HarnessJson.FileHash(path) != pin) throw new InvalidDataException("Frozen input changed: " + path); }
        Verify();
        if (HarnessJson.FileHash(Path.Combine(request.Archive,"files.json")) != ArchivePin) throw new InvalidDataException("Wrong dungeon-loot manifest pin.");
        var manifest = HarnessJson.Read<Dictionary<string,string>>(Path.Combine(request.Archive,"files.json"));
        var plan = Read(request.Fixtures); var rows = new List<object>(); var preparations = 0;
        var offerPlan = nativeOffers ? TowerProphecyOffers.Read(request.Fixtures) : null;
        var content = OfflineContent.ForTower(request.ApiRoot, TowerBundle.ReadSettings(request.ApiRoot));
        Directory.CreateDirectory(request.Output);
        var histories = manifest.Keys.Where(k => k.EndsWith("--include-dungeon-loot--history.json")).Order().ToArray();
        if (histories.Length != 32) throw new InvalidDataException("Expected 32 archived included-loot histories.");
        foreach (var file in histories)
        {
            var path = Path.Combine(request.Archive,file);
            if (HarnessJson.FileHash(path) != manifest[file]) throw new InvalidDataException("Changed historical ledger.");
            var history = HarnessJson.Read<JsonElement>(path); var summary = history.GetProperty("summary");
            var owner = history.GetProperty("final").GetProperty("id").GetGuid();
            var horizon = summary.GetProperty("encounters").GetInt32(); var outcome = summary.GetProperty("outcome").GetString()!;
            foreach (var policy in nativeOffers ? new[] { TowerProphecyOffers.Policy } : plan.Policies)
            {
                var schedule = nativeOffers ? await TowerProphecyOffers.Schedule(request.ApiRoot,owner,outcome,horizon,offerPlan!,ct)
                    : await Schedule(request.ApiRoot, owner, outcome, policy, horizon, plan, ct);
                var steps = history.GetProperty("steps").Deserialize<TowerActivityAttempt[]>(HarnessJson.Options)!;
                object? firstChange = null;
                foreach (var decision in history.GetProperty("decisions").Deserialize<TowerEntryDecision[]>(HarnessJson.Options)!)
                {
                    var source = schedule.Checkpoints.Single(c => c.Encounter == decision.Encounter);
                    var stock = new Dictionary<string,int> { ["sigil_goblin_mines"] = summary.GetProperty("questAt").GetInt32() <= decision.Encounter ? 1 : 0, ["sigil_forgotten_catacombs"] = 1 };
                    foreach (var window in history.GetProperty("windows").EnumerateArray().Where(w => w.GetProperty("until").GetInt32() <= decision.Encounter))
                        foreach (var item in window.GetProperty("sigils").EnumerateObject()) stock[item.Name] += item.Value.GetInt32();
                    foreach (var step in steps.Take(decision.AttemptOrdinal)) stock["sigil_" + step.Dungeon]--;
                    if (stock.Values.Any(v => v < 0)) throw new InvalidDataException("Historical stock not funded.");
                    var next = steps.FirstOrDefault(s => s.Ordinal == decision.AttemptOrdinal && s.Encounter == decision.Encounter);
                    var character = next?.Before ?? history.GetProperty("checkpoints").EnumerateArray().Single(c => c.GetProperty("encounter").GetInt32() == decision.Encounter)
                        .GetProperty("character").Deserialize<FixtureCharacter>(HarnessJson.Options)!;
                    var earned = steps.Take(decision.AttemptOrdinal).Count(s => s.Award is not null);
                    var baseline = TowerEntryReadiness.Decide("full-slot-ready", character.Equipment, stock, decision.Encounter,
                        summary.GetProperty("questAt").GetInt32() > 0 && summary.GetProperty("questAt").GetInt32() <= decision.Encounter, decision.AttemptOrdinal, earned);
                    if (HarnessJson.Hash(baseline) != HarnessJson.Hash(decision)) throw new InvalidDataException("Historical decision reconstruction failed.");
                    stock["sigil_goblin_mines"] += source.State.Items.GetValueOrDefault("sigil_goblin_mines");
                    var candidate = TowerEntryReadiness.Decide("full-slot-ready", character.Equipment, stock, decision.Encounter,
                        summary.GetProperty("questAt").GetInt32() > 0 && summary.GetProperty("questAt").GetInt32() <= decision.Encounter, decision.AttemptOrdinal, earned);
                    if (candidate.Reason == baseline.Reason && candidate.Dungeon == baseline.Dungeon) continue;
                    var setup = content.CreateSetup(character.Materialize(content.Equipment), character.MaterializeEssences());
                    var preparation = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content, setup), setup);
                    var prepared = await preparation.PrepareAsync(CombatContentType.Dungeon,
                        [new(new("player", owner, CombatSide.Friendly, 1), new SnapshotCombatantPreparationSource(TowerBattleRunner.ToSnapshot(character, content)))], ct);
                    if (!prepared.Single().Combatant.Equipment.Select(e => e.ProgressionData!.State.Id).Order().SequenceEqual(character.Equipment.Select(e => e.Data.State.Id).Order()))
                        throw new InvalidDataException("Changed prepared equipment identity.");
                    preparations++;
                    firstChange = new { baseline, candidate, extraSigils = source.State.Items.GetValueOrDefault("sigil_goblin_mines"),
                        stock, character, preparedEquipment = prepared.Single().Combatant.Equipment.Select(e => e.ProgressionData).ToArray(),
                        stop = "FirstChangedEntryDecisionNoOutcomeTransferred" };
                    break;
                }
                var name = summary.GetProperty("historyKey").GetString() + "--" + policy + ".json";
                HarnessJson.WriteNew(Path.Combine(request.Output,name), new { history = file, historicalHash = manifest[file], owner, outcome, policy, horizon, schedule, firstChange,
                    historicalSupplies = summary.GetProperty("successes").GetInt32(), projectedCombatOutcomes = 0 });
                rows.Add(new { file = name, changedEntry = firstChange is not null, policy, outcome, horizon,
                    extraSigils = schedule.Checkpoints.Last().State.Items.GetValueOrDefault("sigil_goblin_mines"), claims = schedule.Claims.Count });
            }
        }
        Verify();
        HarnessJson.WriteNew(Path.Combine(request.Output,"result.json"), new { version = nativeOffers ? TowerProphecyOffers.Version : Version, status = nativeOffers ? "NativeOfferAffordabilityOnly" : "ConditionalSourceAffordabilityOnly", archivePin = ArchivePin,
            assumptions = offerPlan?.Assumptions ?? plan.Assumptions, histories = rows, preparations, newFights = 0, newCombatSeeds = 0, measuredPlayerSamples = 0 });
        HarnessJson.WriteNew(Path.Combine(request.Output,"files.json"), Directory.GetFiles(request.Output).Order().ToDictionary(Path.GetFileName, HarnessJson.FileHash));
    }
}
