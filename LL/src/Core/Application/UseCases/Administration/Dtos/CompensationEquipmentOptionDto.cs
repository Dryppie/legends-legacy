using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed class CompensationEquipmentOptionDto : IMapFrom<CompensationEquipmentOption>
{
    public string DefinitionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemBaseId { get; set; } = string.Empty;
    public string ArchetypeId { get; set; } = string.Empty;
    public int MinimumTier { get; set; }
    public int MaximumTier { get; set; }
    public string? NativeStyleId { get; set; }
    public IReadOnlyList<string> CompatibleStyleIds { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<CompensationEquipmentOption, CompensationEquipmentOptionDto>();
}
