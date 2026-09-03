using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ClaimStarterEquipment;

public sealed record ClaimStarterEquipmentCommand(Guid CharacterId, StarterEquipmentGrantKind Kind,
    IReadOnlyList<string> DefinitionIds) : ICommand<Response<StarterEquipmentGrantDto>>;

public sealed class ClaimStarterEquipmentCommandHandler(IStarterEquipmentService service, IMapper mapper)
    : IRequestHandler<ClaimStarterEquipmentCommand, Response<StarterEquipmentGrantDto>>
{
    public async Task<Response<StarterEquipmentGrantDto>> Handle(ClaimStarterEquipmentCommand request, CancellationToken cancellationToken)
    {
        var result = await service.ClaimAsync(request.CharacterId, request.Kind, request.DefinitionIds, cancellationToken);
        return result.Grant is null ? Response<StarterEquipmentGrantDto>.Fail(result.Error ?? "Starter equipment could not be claimed.")
            : Response<StarterEquipmentGrantDto>.Success(mapper.Map<StarterEquipmentGrantDto>(result.Grant));
    }
}
