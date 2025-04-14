using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Application.UseCases._AdminDashboard.Creatures.Queries.GetCreatures;
using Application.UseCases._AdminDashboard.Creatures.Queries.UpdateCreatures;
using Domain.Models.Entities.Creatures;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class CreatureController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<Creature>>> Get()
    {
        return await Mediator.Send(new GetCreaturesQuery());
    }

    [HttpPost("updateCreature")]
    public async Task Update([FromBody] CreatureDto creature)
    {
        await Mediator.Send(new UpdateCreatureQuery(creature));
    }
}
