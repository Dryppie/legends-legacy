using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Combat;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;

public sealed class EntityStatsDto : IMapFrom<EntityStats>
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public List<AbilityStatsDto> Abilities { get; set; } = [];
    public int DamageDone { get; set; }
    public int DamageTaken { get; set; }
    public int HealingDone { get; set; }
    public int HealingReceived { get; set; }
    public int HealthRegenerated { get; set; }
    public int SelfDamageDone { get; set; }
    public int SelfDamageTaken { get; set; }
    public int AlliedDamageDone { get; set; }
    public int AlliedDamageTaken { get; set; }
    public string Team { get; set; } = string.Empty;
    public int BarrierGenerated { get; set; }
    public int DamageBlocked { get; set; }
    public int IncomingRawDamage { get; set; }
    public int AvoidedDamage { get; set; }
    public int AvoidedAttacks { get; set; }
    public int TypedMitigationPrevented { get; set; }
    public int PhysicalMitigationPrevented { get; set; }
    public int MagicalMitigationPrevented { get; set; }
    public int BlockPrevented { get; set; }
    public int DamageReductionPrevented { get; set; }
    public int DamageAmplified { get; set; }
    public int FinalHealthDamage { get; set; }
    public int HealthRegenerationPotential { get; set; }
    public int HealthRegenerationOverhealed { get; set; }
    public int HealthRegenerationPulses { get; set; }
    public int? Health { get; set; }
    public int? MaxHealth { get; set; }
    public int? Barrier { get; set; }
    public int DamageRedirectedTo { get; set; }
    public int DamageRedirectedAway { get; set; }
    public int TargetedAttacks { get; set; }
    public double AttentionSharePercent { get; set; }
    public int ThreatGenerated { get; set; }
    public int StaggerContributed { get; set; }
    public int StaggerBreaks { get; set; }
    public int Deaths { get; set; }
    public int Revivals { get; set; }
    public int DownedTicks { get; set; }

    public void Mapping(Profile profile) => profile.CreateMap<EntityStats, EntityStatsDto>();
}
