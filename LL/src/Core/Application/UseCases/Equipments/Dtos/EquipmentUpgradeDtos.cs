using Application.Common.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentUpgradeQuoteDto : IMapFrom<EquipmentUpgradeQuote>
{
    public Guid OperationId { get; set; }
    public EquipmentUpgradeRequest Request { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool CanExecute { get; set; }
    public string? UnavailableReason { get; set; }
    public EquipmentProgressionItemDto? Before { get; set; }
    public EquipmentProgressionItemDto? After { get; set; }
    public long PartsCost { get; set; }
    public long CinderCost { get; set; }
    public long PartsReturned { get; set; }
    public long AvailableParts { get; set; }
    public long AvailableCinders { get; set; }
    public uint ItemVersion { get; set; }
    public int PriceVersion { get; set; }
    public string? BlueprintItemId { get; set; }
    public long AvailableBlueprints { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<EquipmentUpgradeQuote, EquipmentUpgradeQuoteDto>();
}

public sealed class EquipmentUpgradeOutcomeDto : IMapFrom<EquipmentUpgradeOutcome>
{
    public Guid OperationId { get; set; }
    public EquipmentUpgradeOperationKind Kind { get; set; }
    public Guid ItemInstanceId { get; set; }
    public EquipmentProgressionItemDto? Before { get; set; }
    public EquipmentProgressionItemDto? After { get; set; }
    public long PartsSpent { get; set; }
    public long CindersSpent { get; set; }
    public long PartsReturned { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? BlueprintItemId { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<EquipmentUpgradeOutcome, EquipmentUpgradeOutcomeDto>();
}

public sealed class EquipmentUpgradeMutationDto : IMapFrom<EquipmentUpgradeResult>
{
    public EquipmentUpgradeOutcomeDto? Outcome { get; set; }
    public EquipmentUpgradeQuoteDto? FreshQuote { get; set; }

    public void Mapping(Profile profile) =>
        profile.CreateMap<EquipmentUpgradeResult, EquipmentUpgradeMutationDto>();

    public static Response<EquipmentUpgradeMutationDto> From(
        EquipmentUpgradeResult result,
        IMapper mapper) => new()
    {
        IsSuccess = result.Outcome is not null,
        Data = mapper.Map<EquipmentUpgradeMutationDto>(result),
        ErrorMessage = result.Error ?? string.Empty,
        IsConflict = result.FreshQuote is not null,
        ErrorCode = result.FreshQuote is not null
            ? "equipment_upgrade_quote_changed"
            : Response<EquipmentUpgradeMutationDto>.DefaultErrorCode
    };
}
