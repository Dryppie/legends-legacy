using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Inventories.Queries.GetInventoryById;
public record GetInventoryByIdQuery(Guid characterId) : IRequest<InventoryDto>;

public class GetInventoryByIdQueryHandler : IRequestHandler<GetInventoryByIdQuery, InventoryDto>
{
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;
    public GetInventoryByIdQueryHandler(IInventoryService inventoryService, IMapper mapper)
    {
        _inventoryService = inventoryService;
        _mapper = mapper;
    }
    public async Task<InventoryDto> Handle(GetInventoryByIdQuery request, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryByIdAsync(request.characterId, cancellationToken);
        var inventoryDto =  _mapper.Map<InventoryDto>(inventory);
        return inventoryDto;
    }
}