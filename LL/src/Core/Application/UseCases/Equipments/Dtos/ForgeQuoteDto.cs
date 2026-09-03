using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class ForgeQuoteDto : IMapFrom<ForgeQuote>
{
    public Guid OperationId { get; set; }
    public ForgeRequest Request { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool CanExecute { get; set; }
    public string? UnavailableReason { get; set; }
    public ForgeItemDto? Before { get; set; }
    public ForgeItemDto? After { get; set; }
    public long ScrapCost { get; set; }
    public long CinderCost { get; set; }
    public long ScrapReturned { get; set; }
    public bool UsesFreeApplication { get; set; }
    public bool IsNoOp { get; set; }
    public uint ItemVersion { get; set; }
    public int PriceVersion { get; set; }
    public ForgeLoadoutImpactDto? EquippedImpact { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<ForgeQuote, ForgeQuoteDto>();
}
