using System.Text.Json;
using System.IO.Compression;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.WorldTower;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.WorldTower;

namespace BalanceHarness;

public sealed record TowerEarnedPoint(string History, string Policy, string Outcome, string Recipe, int Path,
    int Horizon, int Encounter, int SupplyItems, DateTimeOffset AvailableAt, FixtureCharacter Character,
    IReadOnlyList<EquipmentData> Owned);
public sealed record TowerEarnedSeat(string Recipe, int PathOffset);
public sealed record TowerEarnedPlan(string Version, IReadOnlyList<int> Checkpoints,
    IReadOnlyList<TowerEarnedSeat> Roster, string Assumptions);
public sealed record TowerEarnedParty(string Id, int Rotation, string Policy, string Outcome, int Horizon,
    DateTimeOffset StartsAt, TowerFloorDefinition Floor, IReadOnlyList<TowerEarnedPoint> Members);
public sealed record TowerEarnedCombatRequest(string ApiRoot, string Qualification, string QualificationPin,
    string Output, IReadOnlyDictionary<string,string> InputHashes, IReadOnlyDictionary<string,int[]> Panels);

/// <summary>Read-only bridge from pinned personal acquisition receipts to production Tower preparation.
/// No reference-build materialization, inventory transfer, level grants or Tower unlocks.</summary>
public static class TowerEarnedPartyStudy
{
    public const string Version = "tower-earned-party-v1";
    public const string ArchivePin = "f8f05eb4cf6d1e9c28a87b72bfeb0f1a9ff9f1229c5ad687c0413b042d7fbdc5";
    public static readonly string[] Policies = ["fixed-progression", "earned-progression"];
    public static readonly string[] Outcomes = ["perfect", "four-of-five"];
    public static void Verify(IReadOnlyDictionary<string,string> hashes)
    {
        if (hashes.Count == 0 || hashes.Any(p => HarnessJson.FileHash(p.Key) != p.Value))
            throw new InvalidDataException("Frozen earned-party input changed.");
    }

    public static TowerEarnedPoint Extract(JsonElement history, int horizon, TowerActivityInventory inventory)
    {
        if (horizon is not (2160 or 8640 or 25920 or 86400)) throw new InvalidDataException("Undeclared checkpoint.");
        var s = history.GetProperty("summary");
        var checkpoint = history.GetProperty("checkpoints").EnumerateArray()
            .Where(c => c.GetProperty("encounter").GetInt32() <= horizon).Last();
        var encounter = checkpoint.GetProperty("encounter").GetInt32();
        if (encounter != horizon && (encounter != s.GetProperty("encounters").GetInt32() || !s.GetProperty("completed").GetBoolean()))
            throw new InvalidDataException("Only completed histories may stop activity before the party horizon.");
        var c = checkpoint.GetProperty("character").Deserialize<FixtureCharacter>(HarnessJson.Options)!;
        var steps = history.GetProperty("steps").EnumerateArray().Where(x => x.GetProperty("encounter").GetInt32() <= encounter).ToArray();
        var owned = history.GetProperty("starting").Deserialize<List<EquipmentData>>(HarnessJson.Options)!;
        owned.AddRange(history.GetProperty("windows").EnumerateArray().Where(w => w.GetProperty("until").GetInt32() <= encounter)
            .SelectMany(w => w.GetProperty("equipment").Deserialize<EquipmentData[]>(HarnessJson.Options)!));
        foreach (var step in steps)
        {
            if (step.GetProperty("award").ValueKind != JsonValueKind.Null)
                owned.Add(step.GetProperty("award").Deserialize<EquipmentData>(HarnessJson.Options)!);
            owned.AddRange(step.GetProperty("dungeonLoot").GetProperty("equipment").Deserialize<EquipmentData[]>(HarnessJson.Options)!);
        }
        ValidateInventory(c, owned, inventory);
        var supplyItems = steps.Count(x => x.GetProperty("award").ValueKind != JsonValueKind.Null);
        if (supplyItems != checkpoint.GetProperty("supplyItems").GetInt32()) throw new InvalidDataException("Supply receipt mismatch.");
        if (encounter == s.GetProperty("encounters").GetInt32()
            && (HarnessJson.Hash(c) != HarnessJson.Hash(history.GetProperty("final").Deserialize<FixtureCharacter>(HarnessJson.Options)!)
                || HarnessJson.Hash(owned.OrderBy(e => e.State.Id).ToArray()) != HarnessJson.Hash(history.GetProperty("owned").Deserialize<EquipmentData[]>(HarnessJson.Options)!.OrderBy(e => e.State.Id).ToArray())))
            throw new InvalidDataException("Terminal checkpoint differs from retained final state.");
        var ticks = steps.Sum(x => checked((long)Math.Round(x.GetProperty("combatSeconds").GetDouble() * 10)));
        var at = TowerJourneyProgression.Epoch.AddSeconds((long)encounter * 10).AddTicks(ticks * TimeSpan.TicksPerSecond / 10);
        return new(s.GetProperty("key").GetString()!,s.GetProperty("policy").GetString()!,s.GetProperty("outcome").GetString()!,
            s.GetProperty("recipe").GetString()!,s.GetProperty("path").GetInt32(),horizon,encounter,supplyItems,at,c,owned);
    }

    public static void ValidateInventory(FixtureCharacter character, IReadOnlyList<EquipmentData> owned, TowerActivityInventory inventory)
    {
        if (owned.Select(e => e.State.Id).Distinct().Count() != owned.Count
            || owned.Any(e => e.State.Ownership.OwnerId != character.Id)
            || character.Equipment.Any(e => EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(e.Data.State.Tier) > character.Level)
            || HarnessJson.Hash(inventory.Select(owned)) != HarnessJson.Hash(character.Equipment))
            throw new InvalidDataException("Equipment must be personally earned, unique, legal and selected from the available prefix.");
    }

    public static void ValidateParty(TowerEarnedParty party)
    {
        var members = party.Members;
        if (members.Count != party.Floor.RequiredSlots || members.Select(m => m.Character.Id).Distinct().Count() != members.Count
            || members.Any(m => m.Policy != party.Policy || m.Outcome != party.Outcome || m.Horizon != party.Horizon
                || m.AvailableAt > party.StartsAt)
            || members.SelectMany(m => m.Owned).Select(e => e.State.Id).Distinct().Count() != members.Sum(m => m.Owned.Count))
            throw new InvalidDataException("Cross-policy, future, repeated owner or shared inventory in Tower party.");
        // One account per owner is an explicit study assumption, not a database eligibility assertion.
        var rally = new TowerRally { RequiredSlots = party.Floor.RequiredSlots,
            Participants = members.Select((m,i) => new TowerRallyParticipant
                { CharacterId = m.Character.Id, AccountId = m.Character.Id, PartySlot = i+1 }).ToList() };
        if (!WorldTowerPartyRules.HasCompletePartyLayout(rally)) throw new InvalidDataException("Production party layout rejected.");
    }

    public static async Task<CombatEncounterRuntime> Prepare(string root, OfflineContent content, TowerEarnedParty party, int seed, CancellationToken ct)
    {
        ValidateParty(party);
        var floor = TowerContentProviders.Floors(Path.Combine(root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options).GetFloor(party.Floor.FloorNumber);
        if (HarnessJson.Hash(floor!) != HarnessJson.Hash(party.Floor)) throw new InvalidDataException("Changed production floor.");
        var inventory = new TowerActivityInventory(root,content);
        foreach (var member in party.Members) ValidateInventory(member.Character,member.Owned,inventory);
        var first = party.Members[0].Character;
        var setup = content.CreateSetup(first.Materialize(content.Equipment),first.MaterializeEssences());
        var pipeline = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content,setup),setup);
        var guardian = HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/world/creatures.json")).GetProperty("creatures")
            .EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == party.Floor.GuardianCreatureId).Deserialize<Creature>(HarnessJson.Options)!;
        var request = new WorldTowerCombatRuntimeRequest(StableRandom.Guid(Version,party.Id,seed.ToString()),
            StableRandom.Guid(Version,party.Id),party.Floor,
            party.Members.Select((m,i) => new SnapshotCombatantRequest(TowerBattleRunner.ToSnapshot(m.Character,content),
                new(m.Character.Id.ToString(),m.Character.Id,CombatSide.Friendly,WorldTowerPartyRules.GetPartyNumber(i+1)))).ToArray(),
            guardian,0,0,0,party.StartsAt,seed);
        var runtime = await new WorldTowerCombatRuntimeFactory(pipeline).CreateAsync(request,ct);
        foreach (var (member, prepared) in party.Members.Zip(runtime.FriendlyParticipants))
        {
            var c = member.Character; var actual = prepared.Combatant;
            if (actual.Level != c.Level
                || HarnessJson.Hash(actual.Equipment.Select(e => e.ProgressionData).OrderBy(e => e!.State.Id).ToArray())
                    != HarnessJson.Hash(c.Equipment.Select(e => e.Data).OrderBy(e => e.State.Id).ToArray())
                || !actual.EquippedEssences.Select(e => e.Id).SequenceEqual(c.MaterializeEssences().Select(e => e.Id)))
                throw new InvalidDataException("Production preparation altered earned identities or equipment.");
        }
        return runtime;
    }

    public static async Task Qualify(TowerEntrySourceRequest request, CancellationToken ct)
    {
        Verify(request.InputHashes);
        if (Path.Exists(request.Output) || HarnessJson.FileHash(Path.Combine(request.Archive,"files.json")) != ArchivePin)
            throw new InvalidDataException("Fresh output and pinned growing activity required.");
        var plan = HarnessJson.Read<TowerEarnedPlan>(Path.Combine(request.Fixtures,"tower-earned-party.json"));
        if (plan.Version != Version || !plan.Checkpoints.SequenceEqual(new[] {2160,8640,25920,86400}) || plan.Roster.Count != 15)
            throw new InvalidDataException("Changed earned-party declaration.");
        var manifest = HarnessJson.Read<Dictionary<string,string>>(Path.Combine(request.Archive,"files.json"));
        var histories = manifest.Keys.Where(k => k.EndsWith("--history.json")).Order().ToArray();
        if (histories.Length != 64) throw new InvalidDataException("Expected 64 paired histories, 16 distinct owners.");
        var settings = TowerBundle.ReadSettings(request.ApiRoot);
        var content = OfflineContent.ForTower(request.ApiRoot,settings);
        var inventory = new TowerActivityInventory(request.ApiRoot,content);
        var points = new List<TowerEarnedPoint>();
        foreach (var file in histories)
        {
            var path = Path.Combine(request.Archive,file);
            if (HarnessJson.FileHash(path) != manifest[file]) throw new InvalidDataException("Historical receipt changed.");
            var history = HarnessJson.Read<JsonElement>(path);
            points.AddRange(plan.Checkpoints.Select(h => Extract(history,h,inventory)));
        }
        if (points.Select(p => p.Character.Id).Distinct().Count() != 16) throw new InvalidDataException("Changed personal owner pool.");
        Directory.CreateDirectory(request.Output);
        Save(Path.Combine(request.Output,"points.json"),points);
        var budgets = HarnessJson.Read<JsonElement>(Path.Combine(request.Fixtures,"tower-progression-budget-cycle.json"));
        var floors = TowerContentProviders.Floors(Path.Combine(request.ApiRoot,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var rows = new List<object>();
        foreach (var policy in Policies)
        foreach (var outcome in Outcomes)
        foreach (var rotation in Enumerable.Range(0,4))
        foreach (var horizon in plan.Checkpoints)
        foreach (var floorNumber in horizon == plan.Checkpoints.Last() ? Enumerable.Range(1,11) : [1])
        {
            ct.ThrowIfCancellationRequested();
            var floor = floors.GetFloor(floorNumber)!;
            var members = plan.Roster.Take(floor.RequiredSlots).Select(seat => points.Single(p => p.Policy == policy && p.Outcome == outcome
                && p.Horizon == horizon && p.Recipe == seat.Recipe && p.Path == (rotation + seat.PathOffset) % 4)).ToArray();
            var id = $"{policy}--{outcome}--{rotation}--{horizon}--floor-{floorNumber}";
            var party = new TowerEarnedParty(id,rotation,policy,outcome,horizon,members.Max(m => m.AvailableAt),floor,members);
            var runtime = await Prepare(request.ApiRoot,content,party,0,ct); // Preparation sentinel; no combat or seed reservation.
            var prepared = IdleBattleRunner.DescribeParticipants(runtime);
            var budget = budgets.GetProperty("budgets").EnumerateArray().Single(b => b.GetProperty("priorityFloor").GetInt32() == floorNumber);
            var gaps = members.Select((m,i) => new { slot=i+1,owner=m.Character.Id,
                levelShortfall = Math.Max(0,budget.GetProperty("characterLevel").GetInt32()-m.Character.Level),
                essenceShortfall = Math.Max(0,budget.GetProperty("essenceSlots").GetInt32()-m.Character.Essences.Count),
                targetTier=budget.GetProperty("tier").GetInt32(), equippedTiers=m.Character.Equipment.Select(e => e.Data.State.Tier).ToArray() }).ToArray();
            var file = id+".json.gz";
            Save(Path.Combine(request.Output,file),new { party,prepared,preparedHash=HarnessJson.Hash(prepared),budget,gaps,
                laterFloorUnlocksAssumed = floorNumber>1,liveAccountEligibilityVerified=false });
            rows.Add(new { file, floor=floorNumber,policy,outcome,rotation,horizon,party.StartsAt,
                minimumLevel=members.Min(m => m.Character.Level),maximumLevel=members.Max(m => m.Character.Level),
                supplies=members.Sum(m => m.SupplyItems),preparedHash=HarnessJson.Hash(prepared) });
        }
        Verify(request.InputHashes);
        Save(Path.Combine(request.Output,"result.json"),new { version=Version,status="EarnedPartiesPreparedNotFloorProgression",
            archivePin=ArchivePin,plan,points=points.Count,distinctOwners=16,parties=rows,newFights=0,newCombatSeeds=0,measuredPlayerSamples=0 });
        WriteManifest(request.Output);
    }

    public static string PanelKey(TowerEarnedParty p) => $"{p.Outcome}--{p.Rotation}--{p.Horizon}";
    public static async Task Combat(TowerEarnedCombatRequest request,CancellationToken ct)
    {
        Verify(request.InputHashes);
        if (Path.Exists(request.Output) || HarnessJson.FileHash(Path.Combine(request.Qualification,"files.json")) != request.QualificationPin)
            throw new InvalidDataException("Audited qualification pin and fresh output required.");
        if (request.Panels.Count != 32 || request.Panels.Values.Any(v => v.Length != 16)
            || request.Panels.Values.SelectMany(v => v).Distinct().Count() != 512) throw new InvalidDataException("Fixed 512-seed panel required.");
        var manifest = HarnessJson.Read<Dictionary<string,string>>(Path.Combine(request.Qualification,"files.json"));
        foreach (var pair in manifest) if (HarnessJson.FileHash(Path.Combine(request.Qualification,pair.Key)) != pair.Value) throw new InvalidDataException("Changed qualification.");
        var settings=TowerBundle.ReadSettings(request.ApiRoot); var content=OfflineContent.ForTower(request.ApiRoot,settings);
        Directory.CreateDirectory(request.Output); var rows=new List<object>(); int fights=0,replays=0;
        foreach (var file in manifest.Keys.Where(f => f.EndsWith("--floor-1.json.gz")).Order())
        {
            using var stream=File.OpenRead(Path.Combine(request.Qualification,file));
            using var gzip=new GZipStream(stream,CompressionMode.Decompress);
            var proof=JsonSerializer.Deserialize<JsonElement>(gzip,HarnessJson.Options);
            var party=proof.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
            var trials=new List<object>();
            foreach (var seed in request.Panels[PanelKey(party)])
            {
                async Task<object> Run()
                {
                    ct.ThrowIfCancellationRequested(); if(++fights>1088) throw new InvalidDataException("Combat bound exceeded.");
                    var runtime=await Prepare(request.ApiRoot,content,party,seed,ct);
                    var prepared=IdleBattleRunner.DescribeParticipants(runtime);
                    if (HarnessJson.Hash(prepared)!=proof.GetProperty("preparedHash").GetString()) throw new InvalidDataException("Combat preparation drift.");
                    var result=(await content.CreateExecutor().ExecuteTowerPlaybackAsync(runtime,settings.CheckpointIntervalTicks,ct)).Result;
                    var resolved=new CombatEncounterResultFactory().Create(runtime,result);
                    var guardian=resolved.HostilePostState.Single();
                    return new {seed,succeeded=resolved.Outcome==BattleOutcome.Victory,
                        guardianHealthRemainingPercent=guardian.MaxHealth<=0?0:Math.Round(100m*guardian.Health/guardian.MaxHealth,2),
                        summary=BattleSummary.From(resolved.CombatResult,6000)};
                }
                var trial=await Run(); trials.Add(trial);
                if(seed==request.Panels[PanelKey(party)][0])
                {
                    var replay=await Run(); replays++;
                    Save(Path.Combine(request.Output,party.Id+"--replay.json"),replay);
                    if(HarnessJson.Hash(trial)!=HarnessJson.Hash(replay)) throw new InvalidDataException("Native playback replay mismatch.");
                }
            }
            Save(Path.Combine(request.Output,party.Id+"--trials.json"),new {party=party.Id,qualificationFile=file,
                qualificationHash=manifest[file],preparedHash=proof.GetProperty("preparedHash").GetString(),trials});
            rows.Add(new {party=party.Id,wins=trials.Count(t=>JsonSerializer.SerializeToElement(t,HarnessJson.Options).GetProperty("succeeded").GetBoolean()),trials=16});
        }
        if(rows.Count!=64 || fights!=1088 || replays!=64) throw new InvalidDataException("Incomplete diagnostic panel.");
        Verify(request.InputHashes);
        Save(Path.Combine(request.Output,"result.json"),new {version=Version,status="EarnedFloorOneDiagnosticComplete",
            request.QualificationPin,parties=rows,fights,replays,newSeeds=512,measuredPlayerSamples=0,searchPerformed=false});
        WriteManifest(request.Output);
    }
    private static void Save(string path,object value)
    {
        var bytes=JsonSerializer.SerializeToUtf8Bytes(value,HarnessJson.Options);
        if(path.EndsWith(".gz",StringComparison.Ordinal))
        {
            using var compressed=new MemoryStream();
            using(var gzip=new GZipStream(compressed,CompressionLevel.Fastest,true)) gzip.Write(bytes);
            bytes=compressed.ToArray();
        }
        if(Directory.GetFiles(Path.GetDirectoryName(path)!).Sum(p=>new FileInfo(p).Length)+bytes.Length>256*1048576L)
            throw new InvalidDataException("Earned-party output byte limit exceeded.");
        using var stream=new FileStream(path,FileMode.CreateNew); stream.Write(bytes);
    }
    private static void WriteManifest(string directory) => Save(Path.Combine(directory,"files.json"),
        Directory.GetFiles(directory).Order().ToDictionary(Path.GetFileName,HarnessJson.FileHash));
}
