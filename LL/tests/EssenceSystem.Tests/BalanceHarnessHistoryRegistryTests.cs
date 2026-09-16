using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessHistoryRegistryTests
{
    [Fact]
    public void Complete_membership_includes_all_three_names_and_only_excludes_the_current_subtree()
    {
        using var t = new Tree();
        var expected = new[] { t.File("seed-ledger.json"), t.File("old/prior-seed-ledger.json"),
            t.File("old/deep/history-input.json"), t.File("current-other/seed-ledger.json") };
        t.File("current/seed-ledger.json"); t.File("current/nested/history-input.json");
        t.File("not-a-ledger.json"); t.File("old/SEED-LEDGER.JSON");
        var found = TowerCompleteFamilyInputs.HistoryRegistry(t.Root,t.Path("current"),default);
        Assert.True(found.SetEquals(expected));
    }

    [Fact]
    public void New_ledgers_and_removed_ledgers_are_seen_without_a_cached_registry()
    {
        using var t = new Tree();var old=t.File("seed-ledger.json");
        Assert.Single(TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default));
        var added=t.File("new/history-input.json");
        Assert.True(TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default).SetEquals([old,added]));
        System.IO.File.Delete(old);
        Assert.Equal(added,Assert.Single(TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default)));
    }

    [Fact]
    public void Hidden_system_and_temporary_attributes_are_not_filtered_out()
    {
        Assert.True(TowerHistoryRegistry.Include("seed-ledger.json",FileAttributes.Hidden|FileAttributes.System,default));
        Assert.True(TowerHistoryRegistry.Include("history-input.json",FileAttributes.Temporary,default));
        Assert.True(TowerHistoryRegistry.Include("hidden-directory",FileAttributes.Directory|FileAttributes.Hidden,default));
        using var t=new Tree();var path=t.File("hidden/seed-ledger.json");
        System.IO.File.SetAttributes(path,FileAttributes.Hidden|FileAttributes.System);
        Assert.Equal(path,Assert.Single(TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default)));
    }

    [Theory]
    [InlineData("unrelated.bin",FileAttributes.ReparsePoint)]
    [InlineData("seed-ledger.json",FileAttributes.ReparsePoint)]
    [InlineData("folder",FileAttributes.ReparsePoint|FileAttributes.Directory)]
    public void Linked_entries_are_rejected_even_when_their_name_is_irrelevant(string name,FileAttributes attributes)
        => Assert.Throws<InvalidDataException>(()=>TowerHistoryRegistry.Include(name,attributes,default));

    [Fact]
    public void Real_directory_link_is_rejected_at_the_root_and_inside_the_tree()
    {
        using var t=new Tree();t.File("target/seed-ledger.json");var link=t.Path("linked");
        if(OperatingSystem.IsWindows())
        {
            using var command=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe",
                "/c mklink /J \""+link+"\" \""+t.Path("target")+"\"")
                {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})!;
            Assert.True(command.WaitForExit(5000));Assert.Equal(0,command.ExitCode);
        }
        else Directory.CreateSymbolicLink(link,t.Path("target"));
        try
        {
            Assert.Throws<InvalidDataException>(()=>TowerHistoryRegistry.Scan(link,t.Path("excluded"),default));
            Assert.Throws<InvalidDataException>(()=>TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default));
        }
        finally{Directory.Delete(link);}
    }

    [Fact]
    public void Cancellation_is_checked_for_an_ordinary_file_that_would_be_filtered_out()
    {
        using var stop=new CancellationTokenSource();stop.Cancel();
        Assert.Throws<OperationCanceledException>(()=>TowerHistoryRegistry.Include("ordinary.bin",FileAttributes.Normal,stop.Token));
    }

    [Fact]
    public void Already_cancelled_scan_does_not_return_partial_membership_and_persists_trace()
    {
        using var t=new Tree();t.File("seed-ledger.json");using var stop=new CancellationTokenSource();stop.Cancel();
        var trace=new TowerPerformanceTrace();using(trace.Activate())
            Assert.Throws<OperationCanceledException>(()=>TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),stop.Token));
        Assert.Contains(trace.Snapshot(),x=>x.Path=="history.registry" && x.Calls==1);
        Assert.Contains(trace.Snapshot(),x=>x.Path=="history.registry/ledgers" && x.Calls==0);
    }

    [Fact]
    public void Directory_bound_is_enforced_including_the_excluded_directory()
    {
        using var t=new Tree();t.File("excluded/seed-ledger.json");
        Assert.Empty(TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default,2));
        Assert.Throws<InvalidDataException>(()=>TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default,2_000_001));
    }

    [Fact]
    public void Missing_root_fails_instead_of_returning_an_empty_registry()
    {
        using var t=new Tree();
        Assert.ThrowsAny<IOException>(()=>TowerHistoryRegistry.Scan(t.Path("missing"),t.Path("excluded"),default));
    }

    [Fact]
    public void Excluding_the_root_retains_the_previous_empty_registry_contract()
    {
        using var t=new Tree();t.File("seed-ledger.json");
        Assert.Empty(TowerHistoryRegistry.Scan(t.Root,t.Root,default));
    }

    [Fact]
    public void Trace_counts_every_ordinary_file_and_directory_without_changing_membership()
    {
        using var t=new Tree();t.File("seed-ledger.json");t.File("child/data.bin");t.File("child/another.bin");
        var trace=new TowerPerformanceTrace();using(trace.Activate())
            Assert.Single(TowerHistoryRegistry.Scan(t.Root,t.Path("excluded"),default));
        var rows=trace.Snapshot().ToDictionary(x=>x.Path);
        Assert.Equal(2,rows["history.registry/directories"].Calls);
        Assert.Equal(4,rows["history.registry/entries"].Calls);
        Assert.Equal(3,rows["history.registry/files"].Calls);
        Assert.Equal(1,rows["history.registry/ledgers"].Calls);
    }

    [Fact]
    public void Worker_counts_preserve_complete_membership_and_trace_counts()
    {
        using var t = new Tree();
        for (var i = 0; i < 40; i++) { t.File($"branch-{i}/seed-ledger.json"); t.File($"branch-{i}/deep/ordinary.bin"); }
        t.File("excluded/history-input.json");
        var traces = new List<TowerPerformanceTrace>(); var memberships = new List<HashSet<string>>();
        foreach (var workers in new[] { 1, 2, 4 }) {
            var trace = new TowerPerformanceTrace(); traces.Add(trace);
            using (trace.Activate()) memberships.Add(TowerHistoryRegistry.Scan(t.Root, t.Path("excluded"), default, 82, workers));
        }
        Assert.All(memberships, x => Assert.True(x.SetEquals(memberships[0])));
        Assert.Equal(40, memberships[0].Count);
        foreach (var trace in traces) {
            var counts = trace.Snapshot().ToDictionary(x => x.Path, x => x.Calls);
            Assert.Equal(82, counts["history.registry/directories"]);
            Assert.Equal(161, counts["history.registry/entries"]);
            Assert.Equal(80, counts["history.registry/files"]);
        }
    }

    [Fact]
    public void Invalid_worker_counts_cannot_create_an_unbounded_scan()
    {
        using var t = new Tree();
        foreach (var n in new[] { 0, 5, int.MaxValue })
            Assert.Throws<ArgumentOutOfRangeException>(() => TowerHistoryRegistry.Scan(t.Root, t.Path("excluded"), default, maximumConcurrency: n));
    }

    [Fact]
    public void Parallel_cap_failure_joins_workers_and_releases_directory_handles()
    {
        using var t = new Tree();
        for (var i = 0; i < 100; i++) t.File($"branch-{i}/seed-ledger.json");
        Assert.Throws<InvalidDataException>(() => TowerHistoryRegistry.Scan(t.Root, t.Path("excluded"), default, 2));
        Directory.Move(t.Path("branch-99"), t.Path("moved"));
        Assert.Equal(100, TowerHistoryRegistry.Scan(t.Root, t.Path("excluded"), default).Count);
    }

    [Fact]
    public void Cancellation_is_preserved_for_each_supported_worker_count()
    {
        using var t = new Tree(); t.File("child/seed-ledger.json");
        using var stop = new CancellationTokenSource(); stop.Cancel();
        foreach (var n in new[] { 1, 2, 3, 4 })
            Assert.Throws<OperationCanceledException>(() => TowerHistoryRegistry.Scan(t.Root, t.Path("excluded"), stop.Token, maximumConcurrency: n));
    }

    private sealed class Tree:IDisposable
    {
        private readonly string parent=System.IO.Path.GetFullPath(System.IO.Path.Combine(
            Environment.GetEnvironmentVariable("TOWER_REFINEMENT_FIXTURE_ROOT") ?? System.IO.Path.GetTempPath(),"tower-history-registry-tests"));
        public string Root {get;}
        public Tree(){Root=System.IO.Path.Combine(parent,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Root);}
        public string Path(string name)=>System.IO.Path.Combine(Root,name.Replace('/',System.IO.Path.DirectorySeparatorChar));
        public string File(string name){var path=Path(name);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);System.IO.File.WriteAllText(path,"{}");return path;}
        public void Dispose()
        {
            if (!System.IO.Path.GetFullPath(Root).StartsWith(parent+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Fixture escaped its temporary root.");
            foreach(var path in Directory.EnumerateFiles(Root,"*",SearchOption.AllDirectories))System.IO.File.SetAttributes(path,FileAttributes.Normal);
            Directory.Delete(Root,true);
        }
    }
}
