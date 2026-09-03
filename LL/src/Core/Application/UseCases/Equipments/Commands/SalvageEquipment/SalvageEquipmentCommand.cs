using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.SalvageEquipment;

public sealed record SalvageEquipmentCommand(Guid CharacterId, Guid OperationId, Guid ItemInstanceId, bool AllowFavoriteSalvage, string QuoteToken)
    : ICommand<Response<ForgeMutationDto>>;
public sealed class SalvageEquipmentCommandHandler(IForgeService service, IMapper mapper)
    : IRequestHandler<SalvageEquipmentCommand, Response<ForgeMutationDto>>
{
    public async Task<Response<ForgeMutationDto>> Handle(SalvageEquipmentCommand request, CancellationToken ct) =>
        ForgeMutationDto.From(await service.ExecuteAsync(request.CharacterId, request.OperationId,
            new(ForgeOperationKind.Salvage, request.ItemInstanceId, AllowFavoriteSalvage: request.AllowFavoriteSalvage), request.QuoteToken, ct), mapper);
}

