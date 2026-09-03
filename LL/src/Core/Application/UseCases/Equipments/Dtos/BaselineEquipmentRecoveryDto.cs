using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;
public sealed class BaselineEquipmentRecoveryDto : IMapFrom<BaselineEquipmentRecovery>
{
    public Guid OperationId { get; set; }
    public StarterEquipmentGrantKind Kind { get; set; }
    public DateTimeOffset RecoveredAtUtc { get; set; }
    public IReadOnlyList<EquipmentProgressionItemDto> Equipment { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<BaselineEquipmentRecovery, BaselineEquipmentRecoveryDto>();
}
