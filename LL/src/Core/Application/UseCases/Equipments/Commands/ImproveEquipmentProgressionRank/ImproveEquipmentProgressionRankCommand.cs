using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ImproveEquipmentProgressionRank;

public sealed record ImproveEquipmentProgressionRankCommand(Guid CharacterId, Guid OperationId, Guid ItemInstanceId, string QuoteToken)
    : ICommand<Response<ForgeMutationDto>>;
public sealed class ImproveEquipmentProgressionRankCommandHandler(IForgeService service, IMapper mapper)
    : IRequestHandler<ImproveEquipmentProgressionRankCommand, Response<ForgeMutationDto>>
{
    public async Task<Response<ForgeMutationDto>> Handle(ImproveEquipmentProgressionRankCommand request, CancellationToken ct) =>
        ForgeMutationDto.From(await service.ExecuteAsync(request.CharacterId, request.OperationId,
            new(ForgeOperationKind.ImproveRank, request.ItemInstanceId), request.QuoteToken, ct), mapper);
}

