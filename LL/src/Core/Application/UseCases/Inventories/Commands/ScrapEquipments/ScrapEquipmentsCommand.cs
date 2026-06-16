using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Commands.ScrapEquipments;
public record ScrapEquipmentsCommand(Guid CharacterId, List<string> ItemIds) : ICommand<Response<ScrapEquipmentsResponseDto>>;
public class ScrapEquipmentsCommandHandler : IRequestHandler<ScrapEquipmentsCommand, Response<ScrapEquipmentsResponseDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;

    public ScrapEquipmentsCommandHandler(IInventoryService inventoryService, IMapper mapper)
    {
        _inventoryService = inventoryService;
        _mapper = mapper;
    }

    public async Task<Response<ScrapEquipmentsResponseDto>> Handle(ScrapEquipmentsCommand request, CancellationToken cancellationToken)
    {
        var parsedGuids = new List<Guid>();
        foreach (var id in request.ItemIds)
        {
            if (Guid.TryParse(id, out var guid)) parsedGuids.Add(guid);
            else return Response<ScrapEquipmentsResponseDto>.Fail($"Invalid GUID: '{id}'");
        }

        var inventoryItem = await _inventoryService.ScrapEquipments(request.CharacterId, parsedGuids, cancellationToken);
        if (inventoryItem == null) return Response<ScrapEquipmentsResponseDto>.Fail("Failed to scrap equipments.");

        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        if (inventory == null) return Response<ScrapEquipmentsResponseDto>.Fail("Failed to load updated inventory.");

        return Response<ScrapEquipmentsResponseDto>.Success(new ScrapEquipmentsResponseDto
        {
            GainedItem = _mapper.Map<InventoryItemDto>(inventoryItem),
            InventoryItems = _mapper.Map<IReadOnlyList<InventoryItemDto>>(inventory.InventoryItems)
        });
    }
}
