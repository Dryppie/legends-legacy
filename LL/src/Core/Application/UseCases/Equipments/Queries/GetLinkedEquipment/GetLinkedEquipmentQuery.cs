using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetLinkedEquipment;

// Equipment details can be inspected by other players, like marketplace gear.
// Only the item DTO is returned; inventory and owner data are never exposed.
public sealed record GetLinkedEquipmentQuery(Guid EquipmentId) : IQuery<EquipmentInstanceDto?>;

public sealed class GetLinkedEquipmentQueryHandler(IEquipmentSlotService equipment, IMapper mapper)
    : IRequestHandler<GetLinkedEquipmentQuery, EquipmentInstanceDto?>
{
    public async Task<EquipmentInstanceDto?> Handle(GetLinkedEquipmentQuery request, CancellationToken cancellationToken)
    {
        if (request.EquipmentId == Guid.Empty) return null;
        var item = await equipment.GetLinkedEquipmentAsync(request.EquipmentId, cancellationToken);
        if (item is null) return null;
        var dto = mapper.Map<EquipmentInstanceDto>(item);
        dto.IsFavorite = false;
        return dto;
    }
}
