using Application.UseCases._AdminDashboard.Creatures.Queries.GetCreatures;
using Application.UseCases._AdminDashboard.Creatures.Queries.UpdateCreatures;
using Domain.Models.Entities.Creatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class CreatureController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<Creature>>> Get()
    {
        return await Mediator.Send(new GetCreaturesQuery());
    }

    [HttpPut]
    public async Task<ActionResult<Creature>> Update()
    {
        return await Mediator.Send(new UpdateCreatureQuery());
    }
}
