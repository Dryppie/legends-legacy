using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Equipments.Commands.SelectEquipmentProgressionTarget;
public sealed record SelectEquipmentProgressionTargetCommand(Guid CharacterId, string PoolId, string? DefinitionId) : ICommand<Response<EquipmentProtectionPoolDto>>;
public sealed class SelectEquipmentProgressionTargetCommandHandler(IEquipmentAcquisitionService service, IMapper mapper)
    : IRequestHandler<SelectEquipmentProgressionTargetCommand, Response<EquipmentProtectionPoolDto>>
{
    public async Task<Response<EquipmentProtectionPoolDto>> Handle(SelectEquipmentProgressionTargetCommand request, CancellationToken ct)
    {
        var result = await service.SelectAsync(request.CharacterId, request.PoolId, request.DefinitionId, ct);
        return result.Error != null ? Response<EquipmentProtectionPoolDto>.Fail(result.Error)
            : Response<EquipmentProtectionPoolDto>.Success(mapper.Map<EquipmentProtectionPoolDto>(result.Pool));
    }
}
