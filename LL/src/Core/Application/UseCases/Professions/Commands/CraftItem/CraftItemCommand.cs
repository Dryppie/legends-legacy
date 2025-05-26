using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.CraftItem;
public record CraftItemCommand(Guid CharacterId, string RecipeId) : IRequest<Response<InventoryItemDto>>;
public class CraftItemCommandHandler : IRequestHandler<CraftItemCommand, Response<InventoryItemDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IMapper _mapper;

    public CraftItemCommandHandler(ICraftingService craftingService, IMapper mapper)
    {
        _craftingService = craftingService;
        _mapper = mapper;
    }

    public async Task<Response<InventoryItemDto>> Handle(CraftItemCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.RecipeId, out var recipeId)) return Response<InventoryItemDto>.Fail("Failed to craft item.");

        var inventoryItem = await _craftingService.CraftItemFromRecipeAsync(request.CharacterId, recipeId, cancellationToken);

        return inventoryItem != null
            ? Response<InventoryItemDto>.Success(_mapper.Map<InventoryItemDto>(inventoryItem))
            : Response<InventoryItemDto>.Fail("Failed to craft item.");
    }
}