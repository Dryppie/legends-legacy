using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Commands.ShatterEssence;
public record ShatterEssenceCommand(Guid CharacterId, string EssenceId, int Amount) : ICommand<Response<InventoryItemDto>>;
public class ShatterEssenceCommandHandler : IRequestHandler<ShatterEssenceCommand, Response<InventoryItemDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;

    public ShatterEssenceCommandHandler(IInventoryService inventoryService, IMapper mapper)
    {
        _inventoryService = inventoryService;
        _mapper = mapper;
    }

    public async Task<Response<InventoryItemDto>> Handle(ShatterEssenceCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.EssenceId, out var essenceId)) return Response<InventoryItemDto>.Fail("Failed to shatter essence.");

        var inventoryItem = await _inventoryService.ShatterEssenceAsync(request.CharacterId, essenceId, request.Amount, cancellationToken);
        if (inventoryItem == null) return Response<InventoryItemDto>.Fail("Failed to shatter essence.");
        
        return Response<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(inventoryItem));
    }
}
