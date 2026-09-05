using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Components.Attributes;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : ItemInstanceDto, IMapFrom<EquipmentInstance>
{
    public string DisplayName { get; set; } = string.Empty;
    public EquipmentDto? Progression { get; set; }
    public Rarity Rarity { get; set; } = Rarity.Common;
    public ItemQuality Quality { get; set; } = ItemQuality.Standard;
    public EquipmentSetDto? EquipmentSet { get; set; }
    public int Tier { get; set; } = 1;
    public int RequiredLevel { get; set; } = 1;
    public EquipmentBase EquipmentBase { get; set; } = null!;
    public bool IsFavorite { get; set; }
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers { get; set; } = [];
    public List<InstanceAttributeModifier> InstanceModifiers { get; set; } = [];
    public List<AttributeModifierBase> AttributeModifiers { get; set; } = [];
    public IReadOnlyList<AttributeModifierBase> EffectiveAttributeModifiers { get; set; } = [];
    public List<string> AffinityTags { get; set; } = [];
    public double ItemBudget { get; set; }
    public int ItemBudgetTier { get; set; }
    public bool IsGuildBorrowed { get; set; }
    public Guid? GuildVaultItemId { get; set; }
    public string? BorrowedFromGuildName { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>()
            .ForMember(destination => destination.Progression, options => options.MapFrom(source => source.ProgressionData))
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
                destination => destination.RequiredLevel,
                options => options.MapFrom(source =>
                    EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(source.Tier)))
            .ForMember(
                destination => destination.EffectiveAttributeModifiers,
                options => options.MapFrom(source => AttributeCalculator.ProjectEquipmentModifiers(
                    new[] { source },
                    EquipmentTierBudgetCurve.GetFirstCharacterLevelForTier(source.Tier))))
            .ForMember(
                destination => destination.EquipmentSet,
                options => options.MapFrom<EquipmentSetMetadataResolver>())
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
