using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentProgressionItemDto : IMapFrom<EquipmentData>
{
    public Guid Id { get; set; }
    public string DefinitionId { get; set; } = string.Empty;
    public string? NativeStyleId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Tier { get; set; }
    public int Rank { get; set; }
    public int BalanceVersion { get; set; }
    public EquipmentRarity Rarity { get; set; }
    public string? ActiveStyleId { get; set; }
    public string? EquipmentSetId { get; set; }
    public EquipmentOwnershipKind Ownership { get; set; }
    public IReadOnlyDictionary<AttributeType, float> Stats { get; set; } = new Dictionary<AttributeType, float>();
    public void Mapping(Profile profile) => profile.CreateMap<EquipmentData, EquipmentProgressionItemDto>()
        .ForMember(x => x.Id, o => o.MapFrom(x => x.State.Id))
        .ForMember(x => x.DefinitionId, o => o.MapFrom(x => x.State.DefinitionId))
        .ForMember(x => x.NativeStyleId, o => o.MapFrom(x => x.State.NativeStyleId))
        .ForMember(x => x.Tier, o => o.MapFrom(x => x.State.Tier))
        .ForMember(x => x.Rank, o => o.MapFrom(x => x.State.Rank))
        .ForMember(x => x.BalanceVersion, o => o.MapFrom(x => x.State.BalanceVersion))
        .ForMember(x => x.ActiveStyleId, o => o.MapFrom(x => x.State.ActiveStyleId))
        .ForMember(x => x.Ownership, o => o.MapFrom(x => x.State.Ownership.Kind));
}
