using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ApplyEquipmentVariant;

public sealed record ApplyEquipmentVariantCommand(Guid CharacterId, Guid OperationId,
    Guid ItemInstanceId, string BlueprintStyleId, string QuoteToken) : ICommand<Response<EquipmentUpgradeMutationDto>>;

public sealed class ApplyEquipmentVariantCommandHandler(IEquipmentUpgradeService service, IMapper mapper)
    : IRequestHandler<ApplyEquipmentVariantCommand, Response<EquipmentUpgradeMutationDto>>
{
    public async Task<Response<EquipmentUpgradeMutationDto>> Handle(ApplyEquipmentVariantCommand request, CancellationToken ct) =>
        EquipmentUpgradeMutationDto.From(await service.ExecuteAsync(request.CharacterId, request.OperationId,
            new(EquipmentUpgradeOperationKind.ApplyVariant, request.ItemInstanceId, BlueprintStyleId: request.BlueprintStyleId),
            request.QuoteToken, ct), mapper);
}
