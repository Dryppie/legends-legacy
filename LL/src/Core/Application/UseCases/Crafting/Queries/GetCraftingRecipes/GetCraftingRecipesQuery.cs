using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Crafting.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Crafting.Queries.GetCraftingRecipes;

public record GetCraftingRecipesQuery(Guid CharacterId, int TargetTier = 1) : IQuery<Response<IReadOnlyList<CraftingRecipeDto>>>;

public class GetCraftingRecipesQueryHandler : IRequestHandler<GetCraftingRecipesQuery, Response<IReadOnlyList<CraftingRecipeDto>>>
{
    private readonly ICraftingService _craftingService;

    public GetCraftingRecipesQueryHandler(ICraftingService craftingService)
    {
        _craftingService = craftingService;
    }

    public async Task<Response<IReadOnlyList<CraftingRecipeDto>>> Handle(GetCraftingRecipesQuery request, CancellationToken cancellationToken)
    {
        return await _craftingService.GetCraftingRecipesAsync(request.CharacterId, request.TargetTier, cancellationToken);
    }
}
