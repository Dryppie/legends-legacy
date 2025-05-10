using Application.Interfaces.Services.AdminDashboard;
using Domain.Models.Professions.Crafting;
using MediatR;

namespace Application.UseCases._AdminDashboard.Recipes.Commands.UpdateRecipe;
public record UpdateRecipeCommand(Recipe RecipeToUpdate) : IRequest<Recipe>;
public class UpdateRecipeCommandHandler : IRequestHandler<UpdateRecipeCommand, Recipe>
{
    private readonly IRecipeService _recipeService;
    public UpdateRecipeCommandHandler(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }
    public async Task<Recipe> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        await _recipeService.UpdateRecipeAsync(request.RecipeToUpdate, cancellationToken);
        return request.RecipeToUpdate;
    }
}