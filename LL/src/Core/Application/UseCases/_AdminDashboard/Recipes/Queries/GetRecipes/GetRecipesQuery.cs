using Application.Interfaces.Services.AdminDashboard;
using Domain.Models.Professions.Crafting;
using MediatR;

namespace Application.UseCases._AdminDashboard.Recipes.Queries.GetRecipes;
public record GetRecipesQuery() : IRequest<List<Recipe>>;
public class GetRecipeQueryHandler : IRequestHandler<GetRecipesQuery, List<Recipe>>
{
    private readonly IRecipeService _recipeService;
    public GetRecipeQueryHandler(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }
    public async Task<List<Recipe>> Handle(GetRecipesQuery request, CancellationToken cancellationToken)
    {
        return await _recipeService.GetRecipesAsync(cancellationToken);
    }
}