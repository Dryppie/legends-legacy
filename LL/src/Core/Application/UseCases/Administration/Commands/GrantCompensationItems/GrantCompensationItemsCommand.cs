using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Administration.Commands.GrantCompensationItems;

public sealed record GrantCompensationItemsCommand(
    Guid OperationId,
    Guid CharacterId,
    AdministrationActor Actor,
    string ItemBaseId,
    int Quantity,
    string Reason,
    string? InternalNotes,
    EquipmentGrantRequest? Equipment = null) : ICommand<Response<CompensationItemGrantResultDto>>;

public sealed class GrantCompensationItemsCommandHandler(
    ILiveOpsService liveOps,
    IGameEventOutbox outbox,
    IMapper mapper)
    : IRequestHandler<GrantCompensationItemsCommand, Response<CompensationItemGrantResultDto>>
{
    public async Task<Response<CompensationItemGrantResultDto>> Handle(
        GrantCompensationItemsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await liveOps.GrantCompensationItemsAsync(
            request.OperationId,
            request.CharacterId,
            request.Actor,
            request.ItemBaseId,
            request.Quantity,
            request.Reason,
            request.InternalNotes,
            cancellationToken, request.Equipment);
        if (!result.IsSuccess || result.Value is null)
        {
            return Response<CompensationItemGrantResultDto>.Fail(result.ErrorMessage);
        }

        var grantedItems = mapper.Map<IReadOnlyList<InventoryItemDto>>(
            result.Value.GrantedItems);
        if (!result.Value.WasAlreadyProcessed && grantedItems.Count > 0)
        {
            await outbox.EnqueueAsync(
                GameEventTypes.InventoryItemsGranted,
                new InventoryItemsGrantedPayload(
                    request.OperationId,
                    request.CharacterId,
                    grantedItems,
                    ItemAcquisitionSources.AdminCompensation,
                    "Support compensation"),
                request.CharacterId,
                result.Value.AccountId,
                cancellationToken);
        }

        return Response<CompensationItemGrantResultDto>.Success(
            new CompensationItemGrantResultDto(
                request.OperationId,
                result.Value.AccountId,
                result.Value.CharacterId,
                result.Value.ItemBaseId,
                result.Value.Quantity,
                grantedItems,
                result.Value.WasAlreadyProcessed));
    }
}
