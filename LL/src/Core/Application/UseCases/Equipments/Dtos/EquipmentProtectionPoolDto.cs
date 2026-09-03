using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;
public sealed class EquipmentProtectionPoolDto : IMapFrom<EquipmentProtectionPoolView>
{
    public EquipmentProtectionPool Pool { get; set; } = null!;
    public string? SelectedDefinitionId { get; set; }
    public int Progress { get; set; }
    public bool FirstClearGuaranteeAvailable { get; set; }
    public bool CanSelect { get; set; }
    public IReadOnlyList<string> MissingRequirements { get; set; } = [];
    public IReadOnlyList<ForgeItemDto> Targets { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<EquipmentProtectionPoolView, EquipmentProtectionPoolDto>();
}
