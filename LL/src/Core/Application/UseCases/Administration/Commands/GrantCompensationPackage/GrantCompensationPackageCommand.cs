using Application.Interfaces.Services.LL.Administration;
using Application.Interfaces.Outbox;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Items;
using MediatR;
namespace Application.UseCases.Administration.Commands.GrantCompensationPackage;
public sealed record GrantCompensationPackageCommand(Guid OperationId, Guid CharacterId, Guid PackageId, int Version,
    AdministrationActor Actor, string Reason, string? InternalNotes) : ICommand<Response<CompensationPackageReceiptDto>>;
public sealed class GrantCompensationPackageCommandHandler(ICompensationPackageService service, IGameEventOutbox outbox, IMapper mapper)
    : IRequestHandler<GrantCompensationPackageCommand, Response<CompensationPackageReceiptDto>>
{
    public async Task<Response<CompensationPackageReceiptDto>> Handle(GrantCompensationPackageCommand r, CancellationToken ct)
    {
        var result = await service.GrantAsync(r.OperationId, r.CharacterId, r.PackageId, r.Version, r.Actor, r.Reason, r.InternalNotes, ct);
        if (!result.IsSuccess || result.Data is null) return result.IsConflict ? Response<CompensationPackageReceiptDto>.Conflict(result.ErrorMessage, result.ErrorCode) : Response<CompensationPackageReceiptDto>.Fail(result.ErrorMessage);
        if (!result.Data.WasAlreadyProcessed)
        {
            var items = mapper.Map<IReadOnlyList<InventoryItemDto>>(result.Data.Grants.SelectMany(x => x.GrantedItems).ToArray());
            await outbox.EnqueueAsync(GameEventTypes.InventoryItemsGranted, new InventoryItemsGrantedPayload(r.OperationId,
                r.CharacterId, items, ItemAcquisitionSources.AdminCompensation, "Support compensation package"),
                r.CharacterId, result.Data.Action.TargetAccountId, ct);
        }
        return Response<CompensationPackageReceiptDto>.Success(mapper.Map<CompensationPackageReceiptDto>(result.Data));
    }
}
