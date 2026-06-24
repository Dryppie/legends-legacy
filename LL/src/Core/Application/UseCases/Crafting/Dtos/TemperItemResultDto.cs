using Application.Common.Mappings;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed record TemperItemResult(
    EquipmentInstance Equipment,
    TemperingOutcomeType Outcome,
    int PotentialSpent,
    int ProgressGained,
    Rarity PreviousRarity,
    Rarity NewRarity,
    bool RarityUpgraded);

public sealed class TemperItemResultDto : IMapFrom<TemperItemResult>
{
    public EquipmentInstanceDto Equipment { get; init; } = null!;
    public TemperingOutcomeType Outcome { get; init; }
    public int PotentialSpent { get; init; }
    public int ProgressGained { get; init; }
    public Rarity PreviousRarity { get; init; }
    public Rarity NewRarity { get; init; }
    public bool RarityUpgraded { get; init; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TemperItemResult, TemperItemResultDto>();
    }
}
