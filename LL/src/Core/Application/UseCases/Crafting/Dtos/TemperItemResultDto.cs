using Application.UseCases.Equipments.Dtos;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class TemperItemResultDto
{
    public EquipmentInstanceDto Equipment { get; init; } = null!;
    public TemperingOutcomeType Outcome { get; init; }
    public int PotentialSpent { get; init; }
    public int ProgressGained { get; init; }
    public Rarity PreviousRarity { get; init; }
    public Rarity NewRarity { get; init; }
    public bool RarityUpgraded { get; init; }
}
