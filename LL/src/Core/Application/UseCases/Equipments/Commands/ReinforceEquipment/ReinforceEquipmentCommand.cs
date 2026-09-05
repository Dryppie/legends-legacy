using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ReinforceEquipment;

public sealed record ReinforceEquipmentCommand(
    Guid CharacterId,
    Guid OperationId,
    Guid ItemInstanceId,
    string QuoteToken) : ICommand<Response<EquipmentUpgradeMutationDto>>;

public sealed class ReinforceEquipmentCommandHandler(
    IEquipmentUpgradeService service,
    IMapper mapper)
    : IRequestHandler<ReinforceEquipmentCommand, Response<EquipmentUpgradeMutationDto>>
{
    public async Task<Response<EquipmentUpgradeMutationDto>> Handle(
        ReinforceEquipmentCommand request,
        CancellationToken cancellationToken) =>
        EquipmentUpgradeMutationDto.From(await service.ExecuteAsync(
            request.CharacterId,
            request.OperationId,
            new EquipmentUpgradeRequest(
                EquipmentUpgradeOperationKind.Reinforce,
                request.ItemInstanceId),
            request.QuoteToken,
            cancellationToken), mapper);
}
