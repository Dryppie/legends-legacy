using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityCreationStudyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "creation-study-contract-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessAffinityCreationStudyTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Request_plan_allocation_and_intent_cannot_cross_study_versions(bool creation)
    {
        var (context, plan, history) = BalanceHarnessProposalStudyTests.Fixture(creation);
        ProposalStudyFile File(string name, object value)
        {
            var path = Path.Combine(root, name + ".json"); HarnessJson.WriteNew(path, value);
            return new(path, HarnessJson.FileHash(path));
        }
        var settings = new TowerSettings(new(), 10);
        var request = new ProposalStudyRequest(plan.Version, File("plan", plan), File("context", context), File("settings", settings),
            File("history", history), File("runtime", new { }), File("auditor", new { }), root, root, Path.Combine(root, "output"),
            new Dictionary<string, string> { [Path.Combine(root, "history.json")] = HarnessJson.FileHash(Path.Combine(root, "history.json")) },
            new Dictionary<string, string>(), new Dictionary<string, string>());
        S.ValidateRequest(request, true);
        var inputs = S.ReadInputs(request);
        var other = creation ? S.Version : S.CreationVersion;
        Assert.Throws<InvalidDataException>(() => S.ReadInputs(request with { Version = other }));
        Assert.Throws<InvalidDataException>(() => S.ValidateRequest(request with { Version = "future-version" }, false));
        var output = Path.Combine(root, "reservation"); Directory.CreateDirectory(output);
        var allocation = S.Reserve(output, inputs, () => { }, () => { }, default,
            bytes => { for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4, 4), 200000+i); });
        Assert.Equal(plan.Version, allocation.Version);
        Assert.Equal(16384, allocation.Reserved.Count);
        Assert.Equal(HarnessJson.Hash(allocation), HarnessJson.Hash(S.VerifyReservation(output, inputs)));
        var path = Path.Combine(output, "allocation.json");
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(allocation with { Version = other }, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => S.VerifyReservation(output, inputs));
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(allocation, HarnessJson.Options));
        var intent = Path.Combine(output, "entropy-intent.json");
        System.IO.File.WriteAllText(intent, System.IO.File.ReadAllText(intent).Replace(plan.Version, other, StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => S.VerifyReservation(output, inputs));
        Assert.Throws<InvalidDataException>(() => S.Classify(new byte[65536], history, "future-version"));
    }

    [Theory]
    [InlineData(null, 9600)]
    [InlineData(S.ResourceV1, 9600)]
    [InlineData(S.ResourceV2, 9000)]
    public void Creation_launch_keeps_independent_resource_version_and_rejects_relabeling(string? resource, int nativeSeconds)
    {
        var start = DateTimeOffset.UnixEpoch; var hash = new string('a', 64);
        var launch = new ExplorationLaunch(S.CreationVersion, hash, start, start.AddSeconds(nativeSeconds), start.AddSeconds(10800),
            10800, 6442450944, nativeSeconds, 5905580032, 1, "suspended-owned-job-v1");
        S.ValidateLaunch(launch, hash, resource, S.CreationVersion);
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch, hash, resource));
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch with { Version = S.Version }, hash, resource, S.CreationVersion));
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch, hash, resource, "future-version"));
    }
}
