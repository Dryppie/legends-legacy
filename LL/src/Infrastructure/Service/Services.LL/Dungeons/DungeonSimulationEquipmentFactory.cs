using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Dungeons;

/// <summary>
/// Creates deterministic, detached equipment for dungeon diagnostics through the
/// same stat-roll and tempering rules used by player-crafted equipment.
/// </summary>
public sealed class DungeonSimulationEquipmentFactory
{
    private const int PositiveTemperingAttemptsPerRarity = 10;

    private readonly ICraftingDefinitionProvider _craftingDefinitions;
    private readonly IItemStatRollService _statRolls;
    private readonly ITemperingMechanicsService _tempering;

    public DungeonSimulationEquipmentFactory(
        ICraftingDefinitionProvider craftingDefinitions,
        IItemStatRollService statRolls,
        ITemperingMechanicsService tempering)
    {
        _craftingDefinitions = craftingDefinitions;
        _statRolls = statRolls;
        _tempering = tempering;
    }

    public EquipmentInstance Create(
        string slotId,
        EquipmentType equipmentType,
        Rarity rarity)
    {
        if (rarity > Rarity.Legendary)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rarity),
                rarity,
                "Dungeon simulation equipment must use a craftable rarity.");
        }

        var recipe = _craftingDefinitions.GetRecipes()
            .Where(candidate =>
                candidate.Enabled &&
                candidate.OutputItemType == equipmentType)
            .OrderBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No crafting recipe exists for simulated {equipmentType} equipment.");
        if (!_craftingDefinitions.GetEquipmentBases().TryGetValue(
                recipe.OutputItemId,
                out var itemBase))
        {
            throw new InvalidOperationException(
                $"Simulation recipe '{recipe.Id}' output '{recipe.OutputItemId}' was not found.");
        }

        var design = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var raritySteps = TemperingConstants.GetRarityUpgradeCount(rarity);
        var requiredPotential =
            raritySteps
            * PositiveTemperingAttemptsPerRarity
            * TemperingConstants.PotentialCost;
        var equipment = new EquipmentInstance
        {
            Id = CreateDeterministicGuid($"dungeon-simulator:{slotId}:{recipe.Id}:{rarity}"),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase,
            BaseRecipeId = recipe.Id,
            CraftedName = design.Name,
            Tier = EquipmentStatBudgetCatalog.MinimumTier,
            StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
            Rarity = Rarity.Common,
            Quality = ItemQuality.Standard,
            Potential = requiredPotential,
            MaxPotential = requiredPotential,
            AffinityTags = [.. design.Tags],
            InstanceModifiers =
            [
                .. _statRolls.RollBaseStats(
                    itemBase,
                    design,
                    EquipmentStatBudgetCatalog.MinimumTier,
                    ItemQuality.Standard,
                    new Random(CreateDeterministicSeed(
                        $"dungeon-simulator-stat-roll:{slotId}:{recipe.Id}")))
            ]
        };

        var temperingRandom = new PositiveTemperingRandom();
        for (var attempt = 0;
             attempt < raritySteps * PositiveTemperingAttemptsPerRarity;
             attempt++)
        {
            _tempering.ApplyTemperingAttempt(
                equipment,
                design.TemperingProfile,
                temperingRandom);
        }

        if (equipment.Rarity != rarity)
        {
            throw new InvalidOperationException(
                $"Simulated item '{equipment.DisplayName}' reached {equipment.Rarity} " +
                $"instead of {rarity}.");
        }

        return equipment;
    }

    public IReadOnlyDictionary<AttributeType, float> GetAttributeBonuses(
        string slotId,
        EquipmentType equipmentType,
        Rarity rarity) =>
        Create(slotId, equipmentType, rarity)
            .AttributeModifiers
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(modifier => modifier.Amount));

    private static int CreateDeterministicSeed(string value) =>
        BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed class PositiveTemperingRandom : Random
    {
        protected override double Sample() => 0.0005d;
    }
}
