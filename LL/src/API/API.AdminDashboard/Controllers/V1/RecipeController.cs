using Application.UseCases._AdminDashboard.Recipes.Commands.UpdateRecipe;
using Application.UseCases._AdminDashboard.Recipes.Queries.GetRecipes;
using Domain.Models.Professions.Crafting;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;
public class RecipeController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<Recipe>>> Get()
    {
        return await Mediator.Send(new GetRecipesQuery());
    }

    [HttpPost("UpdateRecipe")]
    public async Task<ActionResult<Recipe>> UpdateRecipe([FromBody] Recipe recipe)
    {
        return await Mediator.Send(new UpdateRecipeCommand(recipe));
    }
}
