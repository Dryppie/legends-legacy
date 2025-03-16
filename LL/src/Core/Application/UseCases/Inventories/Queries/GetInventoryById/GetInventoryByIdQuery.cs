using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Authorization.Security;
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
        try
        {
            var inventory = await _inventoryService.GetInventoryByIdAsync(request.characterId, cancellationToken);
            var inventoryDto = _mapper.Map<InventoryDto>(inventory);

            return Response<InventoryDto>.Success(inventoryDto);
        }
        catch (Exception)
        {
            return Response<InventoryDto>.Fail("Error fetching inventory for ID: " + request.characterId);
        }
    }
}