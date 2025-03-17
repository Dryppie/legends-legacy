using Application.UseCases._AdminDashboard.Creatures.Queries.GetCreatures;
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
}
