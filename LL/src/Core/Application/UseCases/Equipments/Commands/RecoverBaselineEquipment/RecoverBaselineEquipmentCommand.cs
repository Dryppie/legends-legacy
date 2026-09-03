using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.RecoverBaselineEquipment;
public sealed record RecoverBaselineEquipmentCommand(Guid CharacterId, Guid OperationId, StarterEquipmentGrantKind Kind) : ICommand<Response<BaselineEquipmentRecoveryDto>>;
public sealed class RecoverBaselineEquipmentCommandHandler(IEquipmentAcquisitionService service, IMapper mapper)
    : IRequestHandler<RecoverBaselineEquipmentCommand, Response<BaselineEquipmentRecoveryDto>>
{
    public async Task<Response<BaselineEquipmentRecoveryDto>> Handle(RecoverBaselineEquipmentCommand request, CancellationToken ct)
    {
        var result = await service.RecoverAsync(request.CharacterId, request.OperationId, request.Kind, ct);
        return result.Recovery == null ? Response<BaselineEquipmentRecoveryDto>.Fail(result.Error ?? "Recovery unavailable.")
            : Response<BaselineEquipmentRecoveryDto>.Success(mapper.Map<BaselineEquipmentRecoveryDto>(result.Recovery));
    }
}
