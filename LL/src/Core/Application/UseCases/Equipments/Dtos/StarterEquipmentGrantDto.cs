using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class StarterEquipmentGrantDto : IMapFrom<StarterEquipmentGrant>
{
    public StarterEquipmentGrantKind Kind { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; }
    public IReadOnlyList<Guid> EquipmentIds { get; set; } = [];
    public IReadOnlyList<string> DefinitionIds { get; set; } = [];

    public void Mapping(Profile profile) => profile.CreateMap<StarterEquipmentGrant, StarterEquipmentGrantDto>()
        .ForMember(x => x.EquipmentIds, o => o.MapFrom(x => x.Equipment.Select(e => e.State.Id)))
        .ForMember(x => x.DefinitionIds, o => o.MapFrom(x => x.Equipment.Select(e => e.State.DefinitionId)));
}
