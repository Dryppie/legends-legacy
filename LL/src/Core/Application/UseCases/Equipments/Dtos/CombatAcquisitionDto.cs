using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;
public sealed class CombatAcquisitionDto : IMapFrom<CombatAcquisitionView>
{
    public string PoolId { get; set; } = string.Empty;
    public string RulesVersion { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public int EquipmentTier { get; set; }
    public bool HasEnteredRegion { get; set; }
    public string? SelectedDefinitionId { get; set; }
    public int PlainVictories { get; set; }
    public int RequiredPlainVictories { get; set; }
    public string? SelectedSigilFamilyId { get; set; }
    public int SigilVictories { get; set; }
    public int RequiredSigilVictories { get; set; }
    public int ScrapRemainder { get; set; }
    public double DiscoveryChance { get; set; }
    public IReadOnlyList<StarterEquipmentOptionDto> Targets { get; set; } = [];
    public IReadOnlyList<CombatAcquisitionSigilOption> Sigils { get; set; } = [];
    public void Mapping(Profile profile) => profile.CreateMap<CombatAcquisitionView, CombatAcquisitionDto>();
}
