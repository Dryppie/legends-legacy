using Application.UseCases.CharacterActions.Commands.StartCraftingAction;
using Application.UseCases.Crafting.Commands.CraftItems;
using Application.UseCases.Crafting.Commands.LearnBlueprint;
using Application.UseCases.Crafting.Dtos;
using Application.UseCases.Crafting.Queries.GetCraftingRecipes;
using Application.UseCases.Crafting.Queries.GetRecipeMasteries;
using Application.UseCases.Professions.Commands.RemoveCraftingQueueItem;
using Application.UseCases.Professions.Dtos;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

public class CraftingController : BaseController
{
    [HttpGet("recipes")]
    public async Task<ActionResult<Response<IReadOnlyList<CraftingRecipeDto>>>> GetRecipes([FromQuery] int targetTier = 1) =>
        await Mediator.Send(new GetCraftingRecipesQuery(CurrentCharacterGuid, targetTier));

    [HttpPost("craft")]
    public async Task<ActionResult<Response<CraftItemsResultDto>>> CraftItems([FromBody] CraftItemsRequestDto request) =>
        await Mediator.Send(new CraftItemsCommand(
            CurrentCharacterGuid,
            request.RecipeId,
            request.BlueprintId,
            request.TargetTier,
            request.Quantity));

    [HttpGet("mastery")]
    public async Task<ActionResult<Response<IReadOnlyList<RecipeMasteryDto>>>> GetMastery() =>
        await Mediator.Send(new GetRecipeMasteriesQuery(CurrentCharacterGuid));

    [HttpPost("blueprints/learn")]
    public async Task<ActionResult<Response<LearnBlueprintResultDto>>> LearnBlueprint([FromBody] LearnBlueprintRequestDto request) =>
        await Mediator.Send(new LearnBlueprintCommand(CurrentCharacterGuid, request.BlueprintItemInstanceId));

    [HttpPost("RemoveCraftingQueueItem")]
    public async Task<ActionResult<Response<RemoveCraftingQueueItemResponseDto>>> RemoveCraftingQueueItem([FromBody] string queueItemId) =>
        await Mediator.Send(new RemoveCraftingQueueItemCommand(CurrentCharacterGuid, queueItemId));
}
