using Application.UseCases._AdminDashboard.Items.Commands.UpdateItems;
using Application.UseCases._AdminDashboard.Items.Dtos;
using Application.UseCases._AdminDashboard.Items.Queries.GetItemBases;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;
public class ItemController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<ItemBaseDto>>> Get()
    {
        return await Mediator.Send(new GetItemBasesQuery());
    }

    [HttpPost("UpdateItemBase")]
    public async Task<ActionResult<ItemBaseDto>> UpdateItemBase([FromBody] ItemBaseDto itemBase)
    {
        return await Mediator.Send(new UpdateItemBaseCommand(itemBase));
    }
}
