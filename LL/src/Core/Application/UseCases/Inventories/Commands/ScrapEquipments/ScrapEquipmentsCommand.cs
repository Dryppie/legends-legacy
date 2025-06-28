using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Commands.ScrapEquipments;
public record ScrapEquipmentsCommand(Guid CharacterId, List<string> ItemIds) : IRequest<Response<InventoryItemDto>>;
public class ScrapEquipmentsCommandHandler : IRequestHandler<ScrapEquipmentsCommand, Response<InventoryItemDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;

    public ScrapEquipmentsCommandHandler(IInventoryService inventoryService, IMapper mapper)
    {
        _inventoryService = inventoryService;
        _mapper = mapper;
    }

    public async Task<Response<InventoryItemDto>> Handle(ScrapEquipmentsCommand request, CancellationToken cancellationToken)
    {
        var parsedGuids = new List<Guid>();
        foreach (var id in request.ItemIds)
        {
            if (Guid.TryParse(id, out var guid)) parsedGuids.Add(guid);
            else return Response<InventoryItemDto>.Fail($"Invalid GUID: '{id}'");
        }

        var inventoryItem = await _inventoryService.ScrapEquipments(request.CharacterId, parsedGuids, cancellationToken);
        if (inventoryItem == null) return Response<InventoryItemDto>.Fail("Failed to scrap equipments.");
        
        var inventoryItemDto = _mapper.Map<InventoryItemDto>(inventoryItem);
        return Response<InventoryItemDto>.Success(inventoryItemDto);
    }
}
