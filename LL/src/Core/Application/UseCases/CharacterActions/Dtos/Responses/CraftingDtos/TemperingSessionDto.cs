using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.CharacterActions.Sessions;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
public class TemperingSessionDto : IMapFrom<TemperingSession>
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public TemperingSummary TemperingSummary { get; set; } = null!;
    public IReadOnlyList<TemperingOutcomeEntryDto> Outcomes { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<TemperingOutcomeEntry, TemperingOutcomeEntryDto>();
        profile.CreateMap<TemperingSession, TemperingSessionDto>();
    }
}

public sealed class TemperingOutcomeEntryDto
{
    public Guid Id { get; set; }
    public Guid QueueItemId { get; set; }
    public Guid EquipmentInstanceId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Domain.Models.Professions.Crafting.TemperingOutcome Outcome { get; set; }
    public int PotentialSpent { get; set; }
    public int PreviousPotential { get; set; }
    public int NewPotential { get; set; }
    public int PreviousItemXp { get; set; }
    public int NewItemXp { get; set; }
    public bool BecameMasterpiece { get; set; }
    public bool BecameLevelingItem { get; set; }
    public Domain.Models.Items.Rarity PreviousRarity { get; set; }
    public Domain.Models.Items.Rarity NewRarity { get; set; }
    public bool RarityUpgraded { get; set; }
    public bool QualityIncreased { get; set; }
    public Domain.Models.Items.ItemQuality? PreviousQuality { get; set; }
    public Domain.Models.Items.ItemQuality? NewQuality { get; set; }
    public Domain.Models.Attributes.AttributeType? ImprovedStat { get; set; }
    public float? PreviousStatValue { get; set; }
    public float? NewStatValue { get; set; }
}
