using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentDto : IMapFrom<EquipmentData>
{
    public int ModelVersion { get; set; }
    public int BalanceVersion { get; set; }
    public string DefinitionId { get; set; } = string.Empty;
    public string ArchetypeId { get; set; } = string.Empty;
    public int Rank { get; set; }
    public ItemQuality Quality { get; set; }
    public double AttributeRollMultiplier { get; set; }
    public string? NativeStyleId { get; set; }
    public string? ActiveStyleId { get; set; }
    public EquipmentOwnershipKind Ownership { get; set; }

    public void Mapping(Profile profile) => profile.CreateMap<EquipmentData, EquipmentDto>()
        .ForMember(x => x.ModelVersion, o => o.MapFrom(x => x.State.ModelVersion))
        .ForMember(x => x.BalanceVersion, o => o.MapFrom(x => x.State.BalanceVersion))
        .ForMember(x => x.DefinitionId, o => o.MapFrom(x => x.State.DefinitionId))
        .ForMember(x => x.ArchetypeId, o => o.MapFrom(x => x.State.ArchetypeId))
        .ForMember(x => x.Rank, o => o.MapFrom(x => x.State.Rank))
        .ForMember(x => x.Quality, o => o.MapFrom(x => x.State.Quality))
        .ForMember(x => x.AttributeRollMultiplier, o => o.MapFrom(x => x.State.AttributeRollMultiplier))
        .ForMember(x => x.NativeStyleId, o => o.MapFrom(x => x.State.NativeStyleId))
        .ForMember(x => x.ActiveStyleId, o => o.MapFrom(x => x.State.ActiveStyleId))
        .ForMember(x => x.Ownership, o => o.MapFrom(x => x.State.Ownership.Kind));
}
