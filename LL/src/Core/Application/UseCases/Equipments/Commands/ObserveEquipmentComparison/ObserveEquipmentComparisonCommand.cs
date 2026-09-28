using Application.Interfaces.Outbox;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Queries.CompareEquipment;
using Application.UseCases.Outbox;
using Common.Primitives;
using Domain.Models.Analytics;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Slots;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ObserveEquipmentComparison;

public sealed record ObserveEquipmentComparisonCommand(Guid CharacterId, Guid RequestId, Guid EquipmentInstanceId,
    EquipmentSlotType? SlotType, EssenceCombatActivity Activity) : ICommand<Response<EquipmentComparisonDto>>;
public sealed class ObserveEquipmentComparisonCommandHandler(IMediator mediator, IGameEventOutbox outbox,
    IItemizationChoiceRepository? choices = null)
    : IRequestHandler<ObserveEquipmentComparisonCommand, Response<EquipmentComparisonDto>>
{
    public async Task<Response<EquipmentComparisonDto>> Handle(ObserveEquipmentComparisonCommand request, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty) return Response<EquipmentComparisonDto>.Fail("A comparison request ID is required.");
        var result = await mediator.Send(new CompareEquipmentQuery(request.CharacterId, request.EquipmentInstanceId, request.SlotType, request.Activity), ct);
        if (result.IsSuccess && result.Data is { } comparison)
        {
            var observation = new ItemizationObservation(ItemizationObservation.StableId("compared", request.RequestId.ToString("N"), request.CharacterId),
                "compared", request.CharacterId, DateTimeOffset.UtcNow, request.Activity.ToString(), comparison.AttributeRulesVersion,
                request.EquipmentInstanceId)
            {
                Choices = choices is null ? null : (await choices.CaptureAsync(request.CharacterId, ct))?.ForItem(request.EquipmentInstanceId, comparison.SlotType),
                Comparison = new(request.RequestId, request.EquipmentInstanceId, comparison.SlotType.ToString(), comparison.CharacterLevel,
                    comparison.Doctrine, comparison.EssenceIds, comparison.EffectiveAttributes.Select(x =>
                        new ItemizationComparisonDelta(x.AttributeType, x.Before, x.After)).ToArray())
            };
            await outbox.EnqueueAsync(GameEventTypes.ItemizationObserved, observation, request.CharacterId, null, ct);
        }
        return result;
    }
}
