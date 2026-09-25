using BalanceHarness;
using System.Text.Json;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessEvidenceStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "proposal-storage-" + Guid.NewGuid().ToString("N"));
    public BalanceHarnessEvidenceStorageTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    internal static ProposalEvidenceStorage Selection()
    {
        var repository = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "../../../../"));
        var analysis = Path.Combine(repository, "Balance Harness/analysis");
        return new(TowerProposalEvidenceCodec.Version, 2L*1073741824, 1073741824, 72,
            HarnessJson.FileHash(Path.Combine(analysis,"proposal_evidence_codec.py")),
            HarnessJson.FileHash(Path.Combine(analysis,"proposal_evidence_storage.py")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Complete_placement_study_reconstructs_every_root_catalogue_and_trajectory(bool compressed)
    {
        var export = Environment.GetEnvironmentVariable("LL_EVIDENCE_STORAGE_EXPORT");
        using var fixture = new BalanceHarnessProposalStudyTests();
        await fixture.FullNativeFixture(false, placement:true, evidenceStorage:compressed ? Selection() : null,
            storageExport:export is null ? null : Path.Combine(export, compressed ? "compressed" : "plain"), accountWrites:true);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("logical")]
    [InlineData("physical")]
    [InlineData("members")]
    [InlineData("module")]
    public void Invalid_request_selections_fail_before_writing(string fault)
    {
        var selection = Selection();
        selection = fault switch {
            "version" => selection with { Version="future" },
            "logical" => selection with { MaximumLogicalBytes=long.MaxValue },
            "physical" => selection with { MaximumPhysicalBytes=0 },
            "members" => selection with { MaximumMembers=73 },
            _ => selection with { CodecSha256="invalid" }
        };
        Assert.Throws<InvalidDataException>(()=>new TowerProposalEvidenceStorage.Writer(root, selection,()=>{}));
        Assert.Empty(Directory.GetFiles(root));
    }

    [Fact]
    public void Root_binding_cannot_be_reused_and_partial_directories_cannot_finish()
    {
        var writer=new TowerProposalEvidenceStorage.Writer(root,Selection(),()=>{});
        Assert.Throws<IOException>(()=>new TowerProposalEvidenceStorage.Writer(root,Selection(),()=>{}));
        Assert.Throws<InvalidDataException>(writer.Finish);
        Assert.Throws<InvalidDataException>(()=>TowerProposalEvidenceStorage.Open(root,null,default));
    }

    [Fact]
    public void Changed_request_binding_rejected_before_payload_access()
    {
        _=new TowerProposalEvidenceStorage.Writer(root,Selection(),()=>{});
        Assert.Throws<InvalidDataException>(()=>new TowerProposalEvidenceStorage.Reader(root,
            Selection() with { MaximumLogicalBytes=1024 },null!));
    }

    [Fact]
    public void Missing_or_changed_reader_module_is_rejected()
    {
        foreach(var name in TowerProposalEvidenceStorage.Modules) File.WriteAllText(Path.Combine(root,name),"tampered");
        Assert.Throws<InvalidDataException>(()=>TowerProposalEvidenceStorage.VerifyModules(root,Selection()));
    }

    [Fact]
    public void Study_preparation_remains_blocked_by_the_resource_protocol()
    {
        var request = new ProposalStudyRequest("unused",null!,null!,null!,null!,null!,null!,"unused","unused","unused",
            null!,null!,null!, EvidenceStorage:Selection());
        Assert.Contains("recovery gate is closed", Assert.Throws<InvalidDataException>(()=>TowerProposalStudy.Inspect(request,default)).Message);
        var legacy = JsonSerializer.SerializeToElement(request with { EvidenceStorage=null }, HarnessJson.Options);
        Assert.False(legacy.TryGetProperty("evidenceStorage",out _));
    }

    [Fact]
    public void Directory_family_and_aggregate_limits_fail_before_an_index_is_published()
    {
        var selection=Selection() with { MaximumLogicalBytes=3 };
        var writer=new TowerProposalEvidenceStorage.Writer(root,selection,()=>{});
        var directory=Path.Combine(root,"search/root-01/control/racing"); Directory.CreateDirectory(directory);
        Assert.Throws<InvalidDataException>(()=>writer.Put(directory,"pair-01.json",new {}));
        writer.Put(directory,"search.json",new {});
        writer.SealDirectory(directory,["search.json"],(n,v)=>HarnessJson.WriteNew(Path.Combine(directory,n),v));
        Assert.Throws<InvalidDataException>(()=>writer.Put(directory,"search.json",new {}));
        var other=Path.Combine(root,"search/root-01/candidate/racing"); Directory.CreateDirectory(other);
        Assert.Throws<InvalidDataException>(()=>writer.Put(other,"search.json",new {}));
        Assert.False(File.Exists(Path.Combine(other,TowerProposalEvidenceStorage.IndexName)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("version")]
    [InlineData("digest")]
    [InlineData("plain")]
    [InlineData("mapping")]
    public void Resealed_index_tampering_is_rejected_by_the_native_reader(string fault)
    {
        var selection=Selection(); var writer=new TowerProposalEvidenceStorage.Writer(root,selection,()=>{});
        var hash=new string('a',64);
        var freeze=new ProposalStudyFreeze("synthetic",hash,hash,hash,hash,hash,
            Enumerable.Range(1,12).Select(n=>new ProposalStudyFamily(n,[1],[new(hash,null!,null!,["control"])])).ToArray());
        foreach(var relative in TowerProposalEvidenceStorage.Directories)
        {
            var folder=Path.Combine(root,relative); Directory.CreateDirectory(folder);
            var names=relative=="study" ? TowerProposalEvidenceStorage.Names(freeze) : new[]{"search.json"};
            foreach(var name in names) writer.Put(folder,name,new{});
            writer.SealDirectory(folder,names,(n,v)=>HarnessJson.WriteNew(Path.Combine(folder,n),v));
            TowerProposalStudy.Seal(relative=="study" ? folder : Path.GetDirectoryName(folder)!,default);
        }
        writer.Finish();
        _=new TowerProposalEvidenceStorage.Reader(root,selection,freeze);
        var directory=Path.Combine(root,"search/root-01/candidate/racing");
        var path=Path.Combine(directory,TowerProposalEvidenceStorage.IndexName);
        var index=HarnessJson.Read<ProposalEvidenceIndex>(path); var entry=index.Entries[0];
        index=fault switch {
            "missing"=>index with { Entries=[] },
            "duplicate"=>index with { Entries=[entry,entry] },
            "version"=>index with { Version="future" },
            "digest"=>index with { Entries=[entry with { PhysicalSha256=new string('0',64) }] },
            "mapping"=>index with { Entries=[entry with { PhysicalPath="../search.json.gz" }] },
            _=>index
        };
        File.WriteAllText(path,JsonSerializer.Serialize(index,HarnessJson.Options));
        if(fault=="plain") File.WriteAllText(Path.Combine(directory,"search.json"),"{}");
        File.Delete(Path.Combine(Path.GetDirectoryName(directory)!,"files.json"));
        TowerProposalStudy.Seal(Path.GetDirectoryName(directory)!,default);
        Assert.Throws<InvalidDataException>(()=>new TowerProposalEvidenceStorage.Reader(root,selection,freeze));
    }
}
