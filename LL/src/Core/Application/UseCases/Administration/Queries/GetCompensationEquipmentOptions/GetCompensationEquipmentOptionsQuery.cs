using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetCompensationEquipmentOptions;

public sealed record GetCompensationEquipmentOptionsQuery(Guid CharacterId, string ItemBaseId)
    : IQuery<Response<CompensationEquipmentOptionsDto>>;

public sealed class GetCompensationEquipmentOptionsQueryHandler(ILiveOpsService liveOps, IMapper mapper)
    : IRequestHandler<GetCompensationEquipmentOptionsQuery, Response<CompensationEquipmentOptionsDto>>
{
    public async Task<Response<CompensationEquipmentOptionsDto>> Handle(GetCompensationEquipmentOptionsQuery request, CancellationToken cancellationToken)
    {
        if (await liveOps.GetPlayerAsync(request.CharacterId, cancellationToken) is null)
            return Response<CompensationEquipmentOptionsDto>.Fail("The target character was not found.");
        return Response<CompensationEquipmentOptionsDto>.Success(mapper.Map<CompensationEquipmentOptionsDto>(
            await liveOps.GetCompensationEquipmentOptionsAsync(request.CharacterId, request.ItemBaseId, cancellationToken)));
    }
}
