using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class ForgeStyleOptionDto : IMapFrom<ForgeStyleOption>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemBaseId { get; set; } = string.Empty;
    public bool IsLearned { get; set; }
    public bool FreeApplicationAvailable { get; set; }
    public bool IsCompatible { get; set; }
    public bool IsNative { get; set; }
    public bool IsActive { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<ForgeStyleOption, ForgeStyleOptionDto>();
}
