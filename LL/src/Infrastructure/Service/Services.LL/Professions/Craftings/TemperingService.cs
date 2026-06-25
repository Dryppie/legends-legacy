using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
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
        return profile != null && (current.EquipmentInstance.Potential ?? 0) >= TemperingConstants.PotentialCost;
    }

    public bool HandleTempering(CraftingQueueItem current, TemperingSummary temperingSummary, Random rng, Dictionary<TemperingOutcome, double> temperingBonuses)
    {
        var profile = GetQueuedProfile(current);
        if (profile == null || (current.EquipmentInstance.Potential ?? 0) < TemperingConstants.PotentialCost)
            return false;

        var result = _temperingMechanics.ApplyTemperingAttempt(current.EquipmentInstance, profile, rng);

        temperingBonuses.TryGetValue(TemperingOutcome.Positive, out var doubleProfessionExperienceChance);
        var experience = result.Outcome switch
        {
            TemperingOutcome.Critical => 100,
            _ => 1,
        };
        if (rng.NextDouble() < doubleProfessionExperienceChance / 100)
            experience *= 2;

        AllocateExpBasedOnCraftingProfession(temperingSummary, experience, current.CraftType);
        return true;
    }

    private TemperingProfileDefinition? GetQueuedProfile(CraftingQueueItem current)
    {
        return _temperingProfileResolver.ResolveFor(current.EquipmentInstance);
    }

    private static void AllocateExpBasedOnCraftingProfession(TemperingSummary temperingSummary, int experience, CraftType craftType)
    {
        switch (craftType)
        {
            case CraftType.ArmorForging:
                temperingSummary.ArmorForgingExperience += experience;
                break;
            case CraftType.JewelryCrafting:
                temperingSummary.JewelryCraftingExperience += experience;
                break;
            case CraftType.WeaponSmithing:
                temperingSummary.WeaponSmithingExperience += experience;
                break;
            default:
                break;
        }
    }
}
