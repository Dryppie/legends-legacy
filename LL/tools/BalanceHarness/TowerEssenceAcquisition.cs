using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Essences;

namespace BalanceHarness;

/// <summary>Retained native item/absorption identities, linked to the independently audited quest evidence.</summary>
public sealed record TowerEssenceAcquisition(Guid Owner,string Quest,string Token,string Option,string Item,
    Guid TokenInstance,Guid UnboundInstance,Guid EssenceId,string Definition,DateTimeOffset At,string EvidenceHash);

public sealed partial class TowerGrowthProgression
{
    public void RetainAcquisition(TowerEssenceAcquisition receipt,PlayerEssence essence,bool attune=true)
    {
        var token=ShenicEssenceTokenCatalog.Definitions.Single(t=>t.ItemBaseId=="item.essence_token.old_forest");
        var option=token.Options.SingleOrDefault(o=>o.Id==receipt.Option);
        if(Character.Level<40||essences.Count!=4||acquisitions.Count!=0||receipt.Owner!=Character.Id
            ||receipt.Quest!="quest.shenic.roots_remember"||receipt.Token!=token.ItemBaseId||option is null||option.ItemId!=receipt.Item
            ||receipt.Definition!="essence."+receipt.Option||receipt.TokenInstance==Guid.Empty||receipt.UnboundInstance==Guid.Empty
            ||receipt.TokenInstance==receipt.UnboundInstance||receipt.EssenceId==Guid.Empty||receipt.At<TowerJourneyProgression.Epoch
            ||receipt.EvidenceHash.Length!=64||!receipt.EvidenceHash.All(Uri.IsHexDigit)
            ||essence.Id!=receipt.EssenceId||essence.CharacterId!=receipt.Owner||essence.EssenceDefinitionId!=receipt.Definition
            ||essence.Level!=1||essence.CurrentXp!=0||essence.AscensionTier!=0||essence.IsEvolved
            ||essences.Any(e=>e.Id==essence.Id||e.EssenceDefinitionId==essence.EssenceDefinitionId))
            throw new InvalidDataException("Unqualified or repeated fifth-Essence acquisition.");
        acquisitions.Add(receipt);essences.Add(essence);if(attune)Attune(5);
    }
}
