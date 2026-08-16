using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions.Crafting.V2;
using Domain.Components.Attributes;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : ItemInstanceDto, IMapFrom<EquipmentInstance>
{
    public string DisplayName { get; set; } = string.Empty;
    public Rarity Rarity { get; set; } = Rarity.Common;
    public ItemQuality Quality { get; set; } = ItemQuality.Standard;
    public string? BaseRecipeId { get; set; }
    public string? BlueprintId { get; set; }
    public EquipmentCraftingDesignMetadataDto? CraftingDesign { get; set; }
    public string? CraftedName { get; set; }
    public int Tier { get; set; } = 1;
    public int StatModelVersion { get; set; } = EquipmentStatBudgetCatalog.LegacyBalanceVersion;
    public int? Potential { get; set; } = null;
    public int? MaxPotential { get; set; } = null;
    public int TemperingProgress { get; set; } = 0;
    public EquipmentBase EquipmentBase { get; set; } = null!;
    public int ItemXp { get; set; } = 0;
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers { get; set; } = [];
    public List<InstanceAttributeModifier> InstanceModifiers { get; set; } = [];
    public List<AttributeModifierBase> AttributeModifiers { get; set; } = [];
    public IReadOnlyList<AttributeModifierBase> EffectiveAttributeModifiers { get; set; } = [];
    public List<ToolBonusModifier> ToolAffixes { get; set; } = [];
    public IReadOnlyList<ToolBonusModifier> EffectiveToolBonuses { get; set; } = [];
    public List<string> AffinityTags { get; set; } = [];
    public double ItemBudget { get; set; }
    public int ItemBudgetTier { get; set; }
    public EquipmentRollRangeDto? RollRange { get; set; }
    public bool IsGuildBorrowed { get; set; }
    public Guid? GuildVaultItemId { get; set; }
    public string? BorrowedFromGuildName { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>()
            .BeforeMap((source, _) => EquipmentStatModelMigrator.MigrateToCurrent(source))
            .ForMember(
                destination => destination.ItemBudget,
                options => options.MapFrom(source =>
                    EquipmentBudgetEvaluator.Evaluate(
                        source.AttributeModifiers,
                        source.Tier)))
            .ForMember(
                destination => destination.ItemBudgetTier,
                options => options.MapFrom(source => source.Tier))
            .ForMember(
                destination => destination.EffectiveAttributeModifiers,
                options => options.MapFrom(source => AttributeCalculator.ProjectEquipmentModifiers(
                    new[] { source },
                    EquipmentTierBudgetCurve.GetFirstCharacterLevelForTier(source.Tier))))
            .ForMember(
                destination => destination.CraftingDesign,
                options => options.MapFrom<EquipmentCraftingDesignMetadataResolver>())
            .ForMember(
                destination => destination.RollRange,
                options => options.MapFrom<EquipmentRollRangeResolver>())
            .ForMember(
                destination => destination.IsGuildBorrowed,
                options => options.MapFrom(source => source.GuildVaultItem != null && source.GuildVaultItem.BorrowedByCharacterId != null))
            .ForMember(
                destination => destination.GuildVaultItemId,
                options => options.MapFrom(source => source.GuildVaultItem == null ? (Guid?)null : source.GuildVaultItem.Id))
            .ForMember(
                destination => destination.BorrowedFromGuildName,
                options => options.MapFrom(source => source.GuildVaultItem == null ? null : source.GuildVaultItem.Guild.Name));
    }
}

public sealed class EquipmentRollRangeDto
{
    public int MinimumPotential { get; init; }
    public int MaximumPotential { get; init; }
    public IReadOnlyList<EquipmentAttributeRollRangeDto> Attributes { get; init; } = [];
}

public sealed class EquipmentAttributeRollRangeDto
{
    public AttributeType AttributeType { get; init; }
    public float MinimumAmount { get; init; }
    public float MaximumAmount { get; init; }
}

public sealed class EquipmentRollRangeResolver
    : IValueResolver<EquipmentInstance, EquipmentInstanceDto, EquipmentRollRangeDto?>
{
    private readonly IEquipmentRollRangeService? _rollRanges;

    public EquipmentRollRangeResolver()
    {
    }

    public EquipmentRollRangeResolver(IEquipmentRollRangeService rollRanges)
    {
        _rollRanges = rollRanges;
    }

    public EquipmentRollRangeDto? Resolve(
        EquipmentInstance source,
        EquipmentInstanceDto destination,
        EquipmentRollRangeDto? destinationMember,
        ResolutionContext context)
    {
        var range = _rollRanges?.Resolve(source);
        if (range is null) return null;

        return new EquipmentRollRangeDto
        {
            MinimumPotential = range.MinimumPotential,
            MaximumPotential = range.MaximumPotential,
            Attributes = range.Attributes
                .Select(attribute => new EquipmentAttributeRollRangeDto
                {
                    AttributeType = attribute.AttributeType,
                    MinimumAmount = attribute.MinimumAmount,
                    MaximumAmount = attribute.MaximumAmount
                })
                .ToList()
        };
    }
}
