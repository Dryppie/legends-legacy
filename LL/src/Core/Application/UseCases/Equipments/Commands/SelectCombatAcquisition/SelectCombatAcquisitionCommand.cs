using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Equipments.Commands.SelectCombatAcquisition;
public sealed record SelectCombatAcquisitionCommand(Guid CharacterId, Guid OperationId, string PoolId, string? DefinitionId, string? SigilFamilyId)
    : ICommand<Response<CombatAcquisitionDto>>;
public sealed class SelectCombatAcquisitionCommandHandler(ICombatAcquisitionService service, IMapper mapper)
    : IRequestHandler<SelectCombatAcquisitionCommand, Response<CombatAcquisitionDto>>
{
    public async Task<Response<CombatAcquisitionDto>> Handle(SelectCombatAcquisitionCommand request, CancellationToken ct)
    {
        var result = await service.SelectAsync(request.CharacterId, request.OperationId, request.PoolId, request.DefinitionId, request.SigilFamilyId, ct);
        return result.Error != null ? Response<CombatAcquisitionDto>.Fail(result.Error)
            : Response<CombatAcquisitionDto>.Success(mapper.Map<CombatAcquisitionDto>(result.State));
    }
}
