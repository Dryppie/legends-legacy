using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Extensions;
using Services.LL.Interfaces;

namespace Services.LL.Professions.Craftings;
public class TemperingService : ITemperingService
{
    private readonly ITemperingProfileResolver _temperingProfileResolver;
    private readonly ITemperingMechanicsService _temperingMechanics;

    public TemperingService(
        ITemperingProfileResolver temperingProfileResolver,
        ITemperingMechanicsService temperingMechanics)
    {
        _temperingProfileResolver = temperingProfileResolver;
        _temperingMechanics = temperingMechanics;
    }

    public bool CanTemper(CraftingQueueItem current)
    {
        var profile = GetQueuedProfile(current);
        return profile != null &&
               current.EquipmentInstance.Rarity < Rarity.Legacy &&
               (current.EquipmentInstance.Potential ?? 0) >= TemperingConstants.PotentialCost;
    }

    public bool HandleTempering(
        CraftingQueueItem current,
        TemperingSummary temperingSummary,
        Random rng,
        double craftingExperienceGainBps,
        double negativeOutcomeReductionBps)
    {
        var profile = GetQueuedProfile(current);
        if (profile == null ||
            current.EquipmentInstance.Rarity >= Rarity.Legacy ||
            (current.EquipmentInstance.Potential ?? 0) < TemperingConstants.PotentialCost)
            return false;

        var wasMasterpiece = current.EquipmentInstance.IsMasterpiece;
        var wasLevelingItem = current.EquipmentInstance.IsLevelingItem;
        var result = _temperingMechanics.ApplyTemperingAttempt(current.EquipmentInstance, profile, rng, negativeOutcomeReductionBps);
        if (!wasMasterpiece && current.EquipmentInstance.IsMasterpiece)
        {
            temperingSummary.Masterpieces++;
        }

        if (!wasLevelingItem && current.EquipmentInstance.IsLevelingItem)
        {
            temperingSummary.LevelingItems++;
        }

        if (result.QualityIncreased)
        {
            temperingSummary.QualityIncreases++;
        }

        var experience = result.Outcome switch
        {
            TemperingOutcome.Critical => 100,
            _ => 1,
        };
        experience = experience.ApplyPositiveBps(craftingExperienceGainBps);

        AllocateExpBasedOnCraftingProfession(temperingSummary, experience, current.CraftType);
        return true;
    }

    private TemperingProfileDefinition? GetQueuedProfile(CraftingQueueItem current)
    {
        return _temperingProfileResolver.ResolveFor(current.EquipmentInstance);
    }

    private static void AllocateExpBasedOnCraftingProfession(TemperingSummary temperingSummary, int experience, CraftType craftType)
    {
        temperingSummary.CraftingExperience += experience;
    }

}
