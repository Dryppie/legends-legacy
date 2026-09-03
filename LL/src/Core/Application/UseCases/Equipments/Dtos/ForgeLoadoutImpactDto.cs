using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class ForgeLoadoutImpactDto : IMapFrom<ForgeLoadoutImpact>
{
    public IReadOnlyDictionary<AttributeType, float> BeforeAttributes { get; set; } = new Dictionary<AttributeType, float>();
    public IReadOnlyDictionary<AttributeType, float> AfterAttributes { get; set; } = new Dictionary<AttributeType, float>();
    public IReadOnlyList<string> BeforeSetBonusIds { get; set; } = [];
    public IReadOnlyList<string> AfterSetBonusIds { get; set; } = [];
    public IReadOnlyList<string> BeforeAbilityIds { get; set; } = [];
    public IReadOnlyList<string> AfterAbilityIds { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<ForgeLoadoutImpact, ForgeLoadoutImpactDto>();
}
