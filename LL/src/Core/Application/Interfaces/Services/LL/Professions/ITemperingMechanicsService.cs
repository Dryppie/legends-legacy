using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public sealed record TemperingAttemptResult(
    EquipmentInstance Equipment,
    TemperingOutcome Outcome,
    int PotentialSpent,
    Rarity PreviousRarity,
    Rarity NewRarity,
    bool RarityUpgraded,
    bool QualityIncreased = false,
    ItemQuality? PreviousQuality = null,
    ItemQuality? NewQuality = null,
    AttributeType? ImprovedStat = null,
    float? PreviousStatValue = null,
    float? NewStatValue = null,
    int PreviousPotential = 0,
    int NewPotential = 0,
    int PreviousItemXp = 0,
    int NewItemXp = 0,
    bool BecameMasterpiece = false,
    bool BecameLevelingItem = false);

public interface ITemperingMechanicsService
{
    TemperingAttemptResult ApplyTemperingAttempt(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng,
        double negativeOutcomeReductionBps = 0);
}
