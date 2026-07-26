using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Services.LL.Interfaces;

namespace Services.LL.PowerRatings;

public sealed record PowerBuildSnapshot(
    string Fingerprint,
    IReadOnlyList<CombatEntity> Combatants,
    CombatRatingBreakdown Rating);

public sealed class PowerBuildSnapshotFactory
{
    private readonly ICharacterRepository _characters;
    private readonly ICombatSetupService _combatSetup;
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;

    public PowerBuildSnapshotFactory(
        ICharacterRepository characters,
        ICombatSetupService combatSetup,
        IEssenceCombatLoadoutResolver essenceLoadouts)
    {
        _characters = characters;
        _combatSetup = combatSetup;
        _essenceLoadouts = essenceLoadouts;
    }

    public async Task<PowerBuildSnapshot?> CreateAsync(
        Guid characterId,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken)
    {
        if (partySelection.CompanionIds.Count > 0)
            return null;

        var character = await _characters.GetCharacterOverviewByCharacterIdAsync(
            characterId,
            cancellationToken);
        if (character is null)
            return null;

        var activeLoadout = character.EssenceLoadouts.FirstOrDefault(x => x.IsActive);
        var equippedEssences = activeLoadout?.Slots
            .OrderBy(x => x.SlotIndex)
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<Domain.Models.Essences.PlayerEssence>()
            .ToList() ?? [];

        var equipment = character.EquipmentSlots
            .Where(slot => slot.EquipmentInstance is not null)
            .Select(slot => slot.EquipmentInstance!)
            .DistinctBy(equipment => equipment.Id)
            .ToList();
        var essenceAttributeSources = equippedEssences
            .DistinctBy(essence => essence.Id)
            .Select(essence =>
            {
                var loadout = _essenceLoadouts.Resolve(character.Id, [essence]);
                return new CombatRatingModifierSource(
                    Math.Clamp(
                        essence.PotentialTier,
                        Domain.Models.Professions.Crafting.V2.EquipmentStatBudgetCatalog.MinimumTier,
                        Domain.Models.Professions.Crafting.V2.EquipmentStatBudgetCatalog.MaximumTier),
                    loadout.AttributeModifiers);
            })
            .ToList();
        var rating = CombatRatingCalculator.Calculate(
            character.BaseAttributes,
            equipment,
            essenceAttributeSources);

        var combatant = new CombatEntity(character)
        {
            EquippedEssences = equippedEssences,
            HasEquippedEssenceSnapshot = true
        };
        await _combatSetup.PrepareEntitiesForCombat([combatant]);

        return new PowerBuildSnapshot(
            CreateFingerprint(character),
            [combatant],
            rating);
    }

    public static string CreateFingerprint(Character character)
    {
        var value = new StringBuilder()
            .Append("algorithm:").Append(PowerRatingAlgorithm.Version)
            .Append("|combat:").Append(PowerRatingAlgorithm.CombatRulesVersion)
            .Append("|level:").Append(character.Level);

        foreach (var attribute in character.BaseAttributes.OrderBy(x => x.AttributeType))
        {
            value.Append("|attribute:").Append(attribute.AttributeType)
                .Append('=').Append(attribute.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        foreach (var slot in character.EquipmentSlots.OrderBy(x => x.EquipmentSlotType))
        {
            value.Append("|slot:").Append(slot.EquipmentSlotType);
            if (slot.EquipmentInstance is not { } item)
                continue;

            value.Append(':').Append(item.Id)
                .Append(':').Append(item.ItemBaseId)
                .Append(':').Append(item.Tier)
                .Append(':').Append(item.Quality)
                .Append(':').Append(item.Rarity)
                .Append(':').Append(item.Potential)
                .Append(':').Append(item.MaxPotential)
                .Append(':').Append(item.TemperingProgress)
                .Append(':').Append(item.IsMasterpiece);

            foreach (var modifier in item.AttributeModifiers
                         .OrderBy(x => x.AttributeType)
                         .ThenBy(x => x.ModifierType)
                         .ThenBy(x => x.Amount))
            {
                value.Append(":modifier:").Append(modifier.AttributeType)
                    .Append(':').Append(modifier.ModifierType)
                    .Append(':').Append(modifier.Amount.ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (var affinity in item.AffinityTags.Order(StringComparer.Ordinal))
                value.Append(":affinity:").Append(affinity);
        }

        var activeLoadout = character.EssenceLoadouts.FirstOrDefault(x => x.IsActive);
        foreach (var slot in activeLoadout?.Slots.OrderBy(x => x.SlotIndex) ?? Enumerable.Empty<Domain.Models.Essences.EssenceLoadoutSlot>())
        {
            value.Append("|essence-slot:").Append(slot.SlotIndex);
            if (slot.PlayerEssence is not { } essence)
                continue;

            value.Append(':').Append(essence.EssenceDefinitionId)
                .Append(':').Append(essence.Level)
                .Append(':').Append(essence.PotentialTier)
                .Append(':').Append(essence.AscensionTier)
                .Append(':').Append(essence.IsEvolved);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString()))).ToLowerInvariant();
    }
}
