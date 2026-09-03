using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class ForgeOutcomeDto : IMapFrom<ForgeOutcome>
{
    public Guid OperationId { get; set; }
    public ForgeOperationKind Kind { get; set; }
    public Guid ItemInstanceId { get; set; }
    public string? StyleId { get; set; }
    public ForgeItemDto? Before { get; set; }
    public ForgeItemDto? After { get; set; }
    public long ScrapSpent { get; set; }
    public long CindersSpent { get; set; }
    public long ScrapReturned { get; set; }
    public bool UsedFreeApplication { get; set; }
    public bool WasNoOp { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<ForgeOutcome, ForgeOutcomeDto>();
}
