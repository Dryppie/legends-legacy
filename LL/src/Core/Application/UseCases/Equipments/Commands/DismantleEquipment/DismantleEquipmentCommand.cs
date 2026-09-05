using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.DismantleEquipment;

public sealed record DismantleEquipmentCommand(
    Guid CharacterId,
    Guid OperationId,
    Guid ItemInstanceId,
    bool AllowFavoriteDismantle,
    string QuoteToken) : ICommand<Response<EquipmentUpgradeMutationDto>>;

public sealed class DismantleEquipmentCommandHandler(
    IEquipmentUpgradeService service,
    IMapper mapper)
    : IRequestHandler<DismantleEquipmentCommand, Response<EquipmentUpgradeMutationDto>>
{
    public async Task<Response<EquipmentUpgradeMutationDto>> Handle(
        DismantleEquipmentCommand request,
        CancellationToken cancellationToken) =>
        EquipmentUpgradeMutationDto.From(await service.ExecuteAsync(
            request.CharacterId,
            request.OperationId,
            new EquipmentUpgradeRequest(
                EquipmentUpgradeOperationKind.Dismantle,
                request.ItemInstanceId,
                request.AllowFavoriteDismantle),
            request.QuoteToken,
            cancellationToken), mapper);
}
