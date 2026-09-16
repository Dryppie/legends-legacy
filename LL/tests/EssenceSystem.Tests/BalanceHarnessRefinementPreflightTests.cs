using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessRefinementComparisonFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessRefinementPreflightTests
{
    static Dictionary<string, string> Gameplay() => new[] { "Application", "Common", "Domain", "Services.LL" }
        .ToDictionary(n => n, _ => new string('a', 64));

    [Fact] public void Changed_gameplay_is_rejected_but_harness_may_change()
    {
        var old = Gameplay(); old["BalanceHarness"] = new string('b', 64);
        var current = Gameplay(); current["BalanceHarness"] = new string('c', 64);
        TowerRefinementComparisonPreflight.VerifyGameplay(old, current);
        current["Domain"] = new string('d', 64);
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyGameplay(old, current));
    }
    [Fact] public void Missing_gameplay_identity_is_rejected()
    {
        var current = Gameplay(); current.Remove("Common");
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyGameplay(Gameplay(), current));
    }
    [Fact] public void Completed_ledger_preserves_union_and_checks_count()
    {
        var ledger = JsonSerializer.SerializeToElement(new { reservationState = "Complete", historical = new[] { 3, 1 }, reserved = new[] { 2, 3 } });
        Assert.Equal(new[] { 1, 2, 3 }, TowerRefinementComparisonPreflight.History(ledger, 3));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.History(ledger, 2));
    }
    [Fact] public void Pending_reservation_fails_even_with_expected_count()
    {
        var ledger = JsonSerializer.SerializeToElement(new { reservationState = "Pending", reserved = new[] { 1 } });
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.History(ledger, 1));
    }
    [Fact] public void Missing_or_relative_pins_are_rejected_before_reading_files()
    {
        var path = Path.Combine(Path.GetTempPath(), "preflight-missing.json");
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyPins(new Dictionary<string, string> { [path] = new string('a', 64) }, [path + ".other"], default));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyPins(new Dictionary<string, string> { ["relative.json"] = new string('a', 64) }, [], default));
    }
    [Fact] public void Cancellation_precedes_input_access()
    {
        Assert.Throws<OperationCanceledException>(() => TowerRefinementComparisonPreflight.VerifyPins(new Dictionary<string, string>(), [], new CancellationToken(true)));
    }
    [Fact] public void Changed_bytes_are_rejected()
    {
        var path = Path.Combine(Path.GetTempPath(), "refinement-preflight-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, "captured");
        try {
            var pins = new Dictionary<string, string> { [path] = HarnessJson.FileHash(path) };
            TowerRefinementComparisonPreflight.VerifyPins(pins, [path], default);
            File.AppendAllText(path, "changed");
            Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyPins(pins, [path], default));
        } finally { File.Delete(path); }
    }
    [Fact] public void Controls_use_fixed_order_and_reject_changed_identity()
    {
        var d = F.Definitions()[0]; var expected = TowerRefinementComparisonPreflight.Controls(d);
        var reordered = d with { References = d.References.Select(r => r with { Scenario = r.Scenario with {
            Party = r.Scenario.Party.Select(p => p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() } }).ToArray() };
        Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(TowerRefinementComparisonPreflight.Controls(reordered)));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.Controls(d with { References = d.References.Take(1).ToArray() }));
    }
}
