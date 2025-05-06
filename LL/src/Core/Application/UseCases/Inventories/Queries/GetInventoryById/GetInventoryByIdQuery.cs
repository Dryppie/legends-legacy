using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Queries.GetInventoryById;
public record GetInventoryByIdQuery(Guid characterId) : IRequest<Response<InventoryDto>>;

public class GetInventoryByIdQueryHandler : IRequestHandler<GetInventoryByIdQuery, Response<InventoryDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;
    public GetInventoryByIdQueryHandler(IInventoryService inventoryService, IMapper mapper)
    {
        _inventoryService = inventoryService;
        _mapper = mapper;
    }
    public async Task<Response<InventoryDto>> Handle(GetInventoryByIdQuery request, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryByIdAsync(request.characterId, cancellationToken);
        if (inventory == null) return Response<InventoryDto>.Fail("Failed to get inventory.");

        var inventoryDto =  _mapper.Map<InventoryDto>(inventory);
        return Response<InventoryDto>.Success(inventoryDto);
    }
}