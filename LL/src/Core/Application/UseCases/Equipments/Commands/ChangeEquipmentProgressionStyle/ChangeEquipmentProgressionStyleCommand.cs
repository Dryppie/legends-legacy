using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ChangeEquipmentProgressionStyle;

public sealed record ChangeEquipmentProgressionStyleCommand(Guid CharacterId, Guid OperationId, Guid ItemInstanceId, string? StyleId, string QuoteToken)
    : ICommand<Response<ForgeMutationDto>>;
public sealed class ChangeEquipmentProgressionStyleCommandHandler(IForgeService service, IMapper mapper)
    : IRequestHandler<ChangeEquipmentProgressionStyleCommand, Response<ForgeMutationDto>>
{
    public async Task<Response<ForgeMutationDto>> Handle(ChangeEquipmentProgressionStyleCommand request, CancellationToken ct) =>
        ForgeMutationDto.From(await service.ExecuteAsync(request.CharacterId, request.OperationId,
            new(ForgeOperationKind.ChangeStyle, request.ItemInstanceId, request.StyleId), request.QuoteToken, ct), mapper);
}

