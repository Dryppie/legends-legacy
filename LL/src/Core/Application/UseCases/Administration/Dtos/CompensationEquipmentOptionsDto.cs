using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed class CompensationEquipmentOptionsDto : IMapFrom<CompensationEquipmentOptions>
{
    public bool UsesEquipmentProgression { get; set; }
    public int MaximumQuantity { get; set; }
    public IReadOnlyList<CompensationEquipmentOptionDto> Options { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<CompensationEquipmentOptions, CompensationEquipmentOptionsDto>();
}
