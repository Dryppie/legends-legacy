using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Guilds;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Items.Equipments.Progression;

namespace Domain.Models.Items.Equipments;
public class EquipmentInstance : ItemInstance
{
    public EquipmentData? ProgressionData { get; private set; }
    [NotMapped] public bool HasEquipmentProgression => ProgressionData is not null;

    public void ApplyProgressionData(EquipmentData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.State.Id != Id || data.ItemBaseId != ItemBaseId
            || (ItemBase is EquipmentBase equipmentBase && equipmentBase.EquipmentType != data.EquipmentType))
            throw new InvalidOperationException("Equipment progression descriptor does not match its equipment instance.");
        ProgressionData = data;
        Tier = data.State.Tier;
        Rarity = (Rarity)data.Rarity;
        CraftedName = data.DisplayName;
        BaseRecipeId = null;
        BlueprintId = data.State.ActiveStyleId;
        EquipmentSetId = data.EquipmentSetId;
        StatModelVersion = EquipmentBalance.StatUnitVersion;
        Quality = ItemQuality.Standard;
        Potential = MaxPotential = null;
        ItemXp = TemperingProgress = 0;
        IsMasterpiece = IsLevelingItem = false;
        InstanceModifiers = data.Stats.OrderBy(x => x.Key)
            .Select(x => new InstanceAttributeModifier(x.Key, x.Value) { ItemInstanceId = Id }).ToList();
    }

    public void BindEquipmentProgressionForEquip(Guid characterId)
    {
        if (ProgressionData is not { } data) return;
        if (data.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned
            && GuildVaultItem is { } loan && loan.EquipmentInstanceId == Id
            && loan.GuildId == data.State.Ownership.OwnerId && loan.BorrowedByCharacterId == characterId)
            return; // The repository verifies current membership before equipping this loan.
        if (data.State.Ownership.OwnerId != characterId
            || data.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned)
            throw new InvalidOperationException("This Equipment progression equipment is not owned by the character.");
        ProgressionData = data.BindForPersonalUse();
    }
    public void DonateEquipmentProgressionToGuild(Guid expectedOwnerId, Guid guildId)
    {
        if (ProgressionData is { } data) ProgressionData = data.DonateToGuild(expectedOwnerId, guildId);
    }
    public void TransferEquipmentProgressionToCharacter(Guid expectedOwnerId, Guid recipientId)
    {
        if (ProgressionData is { } data)
            ProgressionData = data.TransferToCharacter(expectedOwnerId, recipientId);
    }
    public Rarity Rarity { get; set; } = Rarity.Common;
    public ItemQuality Quality { get; set; } = ItemQuality.Standard;
    public string? BaseRecipeId { get; set; }
    public string? BlueprintId { get; set; }
    public string? EquipmentSetId { get; set; }
    public string? CraftedName { get; set; }
    public int Tier { get; set; } = 1;
    public int StatModelVersion { get; set; } = EquipmentStatBudgetCatalog.LegacyBalanceVersion;
    public int? Potential { get; set; } = null;
    public int? MaxPotential { get; set; } = null;
    public int TemperingProgress { get; set; } = 0;
    public uint Version { get; set; }
    public int ItemXp { get; set; } = 0;
    public bool IsMasterpiece { get; set; } = false;
    public bool IsLevelingItem { get; set; } = false;

    /// <summary>
    /// Carries the owning character's favorite preference while this item is equipped.
    /// The value is copied from the inventory row on equip and back to the new row on unequip.
    /// </summary>
    public bool IsFavorite { get; set; }

    [NotMapped]
    public EquipmentBase EquipmentBase => (EquipmentBase)ItemBase;

    [NotMapped]
    public string DisplayName => ProgressionData?.DisplayName ?? (!string.IsNullOrWhiteSpace(CraftedName)
        ? CraftedName.Trim()
        : EquipmentBase.EquipmentType == EquipmentType.Tool
            ? ToolInstanceNaming.GetDisplayName(EquipmentBase.Name, Rarity)
            : EquipmentBase.Name);

    [NotMapped]
    public bool UsesRecipeStatBudget => !string.IsNullOrWhiteSpace(BaseRecipeId);

    [NotMapped]
    public bool UsesProgressionNormalizedRatings =>
        (HasEquipmentProgression || UsesRecipeStatBudget)
        && StatModelVersion >= EquipmentStatBudgetCatalog.BalanceVersion;

    /// <summary>
    /// Authored item-base modifiers are retained for legacy and directly granted equipment.
    /// Crafted equipment receives its complete combat budget through recipe-generated instance
    /// modifiers, so applying these modifiers as well would budget the same item twice.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers =>
        HasEquipmentProgression || UsesRecipeStatBudget
            ? []
            : EquipmentBase?.AttributeModifiers
            .Select(attr => new ItemAttributeModifier(
                attr.AttributeType,
                GetBoostedBaseModifierAmount(
                    attr.AttributeType,
                    attr.Amount,
                    Rarity),
                attr.ModifierType))
            .ToList()
              ?? [];


    /// <summary>Modifiers that were added to *this* item as it levelled up.</summary>
    public List<InstanceAttributeModifier> InstanceModifiers { get; set; } = [];
    public List<ToolBonusModifier> ToolAffixes { get; set; } = [];
    public List<string> AffinityTags { get; set; } = [];
    public GuildVaultItem? GuildVaultItem { get; set; }

    [NotMapped]
    public List<AttributeModifierBase> AttributeModifiers => ProgressionData is { } data
        ? data.Stats.OrderBy(x => x.Key).Select(x => (AttributeModifierBase)new InstanceAttributeModifier(x.Key, x.Value)).ToList()
        : [
        .. BaseModifiers,
        .. InstanceModifiers,
    ];

    [NotMapped]
    public IReadOnlyList<ToolBonusModifier> EffectiveToolBonuses =>
    [
        .. EquipmentBase.ToolBonuses,
        .. ToolAffixes,
    ];

    public float Boost => HasEquipmentProgression ? 1f : GetRarityBoost(Rarity);

    public static float GetRarityBoost(Rarity rarity) =>
        (float)CalculateRarityBoost(rarity);

    private static decimal CalculateRarityBoost(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1m,
        Rarity.Uncommon => 1.1m,
        Rarity.Rare => 1.3m,
        Rarity.Epic => 1.6m,
        Rarity.Unique => 2m,
        Rarity.Legendary => 2.5m,
        Rarity.Legacy => 3m,
        _ => 1m
    };

    public static int GetBoostedBaseModifierAmount(
        AttributeType attribute,
        float amount,
        Rarity rarity)
    {
        var definition = AttributeCatalog.Get(attribute);
        if (definition.Unit == AttributeUnit.PercentagePoints
            && definition.CapKind == AttributeCapKind.Fixed)
        {
            return Math.Min(
                (int)Math.Ceiling(amount),
                (int)Math.Floor(definition.MaximumValue!.Value));
        }

        var boostedAmount = (decimal)amount * CalculateRarityBoost(rarity);
        var boosted = (int)decimal.Ceiling(boostedAmount);
        return definition.CapKind == AttributeCapKind.Fixed
               && definition.MaximumValue is { } maximum
            ? Math.Min(boosted, (int)Math.Floor(maximum))
            : boosted;
    }
}
