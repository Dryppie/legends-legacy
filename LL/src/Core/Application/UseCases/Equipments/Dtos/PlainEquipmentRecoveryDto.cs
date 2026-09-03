using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;
namespace Application.UseCases.Equipments.Dtos;

public sealed class PlainEquipmentRecoveryDto : IMapFrom<PlainEquipmentRecovery>
{
    public Guid OperationId { get; set; }
    public string DefinitionId { get; set; } = string.Empty;
    public int Tier { get; set; }
    public DateTimeOffset RecoveredAtUtc { get; set; }
    public IReadOnlyList<ForgeItemDto> Equipment { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<PlainEquipmentRecovery, PlainEquipmentRecoveryDto>();
}
