using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Quests;
using AutoMapper;

namespace Application.UseCases.Quests.Dtos;

public sealed class CombatAreaAccessDto : IMapFrom<CombatAreaAccessResult>
{
    public string AreaId { get; set; } = string.Empty;
    public bool CanAccess { get; set; }
    public bool IsVisible { get; set; }
    public int RequiredLevel { get; set; }
    public int? CharacterLevel { get; set; }
    public IReadOnlyList<string> RequiredQuestIds { get; set; } = [];
    public IReadOnlyList<string> UnmetQuestIds { get; set; } = [];
    public int? RequiredTowerFloor { get; set; }
    public bool IsRequiredTowerFloorCleared { get; set; }
    public string? ReasonCode { get; set; }
    public string? PlayerMessage { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<CombatAreaAccessResult, CombatAreaAccessDto>();
}
