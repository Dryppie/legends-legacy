using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;
namespace Application.UseCases.Equipments.Commands.RecoverPlainEquipment;

public sealed record RecoverPlainEquipmentCommand(Guid CharacterId, Guid OperationId, string DefinitionId, int Tier) : ICommand<Response<PlainEquipmentRecoveryDto>>;
public sealed class RecoverPlainEquipmentCommandHandler(IPlainEquipmentRecoveryService service, IMapper mapper)
    : IRequestHandler<RecoverPlainEquipmentCommand, Response<PlainEquipmentRecoveryDto>>
{
    public async Task<Response<PlainEquipmentRecoveryDto>> Handle(RecoverPlainEquipmentCommand request, CancellationToken ct)
    {
        var result = await service.RecoverAsync(request.CharacterId, request.OperationId, request.DefinitionId, request.Tier, ct);
        return result.Error != null ? Response<PlainEquipmentRecoveryDto>.Fail(result.Error)
            : Response<PlainEquipmentRecoveryDto>.Success(mapper.Map<PlainEquipmentRecoveryDto>(result.Recovery));
    }
}
