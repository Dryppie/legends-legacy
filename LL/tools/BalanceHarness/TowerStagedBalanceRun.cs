using System.Text.Json;

namespace BalanceHarness;

public static class TowerStagedBalanceRun
{
    public const string Kind="tower-staged-balance-v1";
    public static async Task<TowerStagedReport> RunAsync(string root,string output,TowerStagedDefinition definition,TowerBulkOptions options,
        bool resume=false,CancellationToken token=default,Action<string>? progress=null,bool verifyOnly=false)
    {
        var d=JsonSerializer.Deserialize<TowerStagedDefinition>(JsonSerializer.SerializeToUtf8Bytes(definition,HarnessJson.Options),HarnessJson.Options)!;
        var maximum=TowerStagedBalance.Validate(d);
        using var campaign=TowerBulkCampaign.Open(root,output,Kind,d,d.ContentHashes,d.SettingsHash,d.ExecutionHash,maximum,d.MaximumBattles,options,resume,verifyOnly,token,progress);
        var runner=new TowerBattleRunner(campaign.Root,new OfflineContent(campaign.Root,campaign.Contract.Scope.Settings.Threat));
        foreach(var cell in d.Cells)
        {
            campaign.Token.ThrowIfCancellationRequested();
            runner.CreateInput(TowerStagedBalance.Scenario(d,cell,1),d.FirstSeeds[0],campaign.Contract.Scope.Settings.Threat,campaign.Contract.Scope.Settings.CheckpointIntervalTicks);
        }
        var batch=0;
        async Task<List<TowerBalanceEvidence>> Execute(IReadOnlyList<TowerStagedCell> cells,int stage)
        {
            var evidence=new List<TowerBalanceEvidence>(); var sources=new List<TowerBalanceRunSource>();
            foreach(var chunk in cells.Chunk(32))
            {
                var id="confirmation-"+(batch++).ToString("D6",System.Globalization.CultureInfo.InvariantCulture);
                var cases=chunk.Select((c,i)=>new TowerCompactCase("cell-"+i.ToString("D3",System.Globalization.CultureInfo.InvariantCulture),TowerStagedBalance.Scenario(d,c,stage))).ToArray();
                var saved=await campaign.BatchAsync(id,cases);
                for(var i=0;i<chunk.Length;i++)
                {
                    evidence.Add(TowerCompactBundle.Evidence(chunk[i].Id,saved,cases[i].Id));
                    sources.Add(new(chunk[i].Id,"batches/"+id,cases[i].Id));
                }
                progress?.Invoke($"Staged confirmation: stage {stage}, {evidence.Count}/{cells.Count} cells verified.");
            }
            campaign.Result($"stage-{stage}-sources.json",sources); campaign.Result($"stage-{stage}-evidence.json",evidence);
            return evidence;
        }
        try
        {
            var anchors=d.AnchorIds.ToHashSet(StringComparer.Ordinal);
            var first=await Execute(d.Cells.Where(c=>!anchors.Contains(c.Id)).ToArray(),1);
            var selection=TowerStagedBalance.Select(d,first);
            var selectionPath=Path.Combine(output,"stage-selection.json");
            if(File.Exists(selectionPath) && HarnessJson.Hash(TowerContractJson.Read<TowerStagedSelection>(selectionPath))!=HarnessJson.Hash(selection))
                throw new InvalidDataException("Frozen stage selection differs from verified first-stage reconstruction.");
            campaign.Result("stage-selection.json",selection); // Frozen and checked before any second-stage attempt, including resume.
            var second=await Execute(selection.Status=="Proceed" ? d.Cells.Where(c=>selection.SecondStageIds.Contains(c.Id)).ToArray() : [],2);
            var report=TowerStagedBalance.Evaluate(d,first,second);
            campaign.Result("assessment.json",report); campaign.TextResult("assessment.md",TowerStagedBalance.Markdown(report));
            campaign.Finish(report.LogicalTrials); return report;
        }
        catch(Exception error)
        {
            campaign.Failure(error is OperationCanceledException ? "Cancelled" : "Invalid",error.Message); throw;
        }
    }
    public static Task<TowerStagedReport> VerifyAsync(string output,CancellationToken token=default)
    {
        var contract=TowerContractJson.Read<TowerBulkContract>(Path.Combine(output,TowerBulkCampaign.ContractFile));
        if(contract.Kind!=Kind) throw new InvalidDataException("Not a staged balance campaign.");
        return RunAsync(Path.Combine(output,"content"),output,contract.Definition.Deserialize<TowerStagedDefinition>(HarnessJson.Options)!,contract.Options,
            resume:true,token:token,verifyOnly:true);
    }
}
