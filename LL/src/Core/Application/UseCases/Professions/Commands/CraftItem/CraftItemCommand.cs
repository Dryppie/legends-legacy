using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Services.LL.Professions;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.CraftItem;
public record CraftItemCommand(Guid CharacterId, string RecipeId) : IRequest<Response<bool>>;
public class CraftItemCommandHandler : IRequestHandler<CraftItemCommand, Response<bool>>
{
    private readonly ICraftingService _craftingService;

    public CraftItemCommandHandler(ICraftingService craftingService)
    {
        _craftingService = craftingService;
    }

    public async Task<Response<bool>> Handle(CraftItemCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.RecipeId, out var recipeId)) return Response<bool>.Fail("Failed to craft item.");

        return await _craftingService.CraftItemFromRecipeAsync(request.CharacterId, recipeId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to craft item.");
    }
}