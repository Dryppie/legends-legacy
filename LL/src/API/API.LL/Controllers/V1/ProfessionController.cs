using Application.UseCases.Professions.Dtos;
using Application.UseCases.Professions.Queries.GetProfessions;
using Common.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
public class ProfessionController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<List<ProfessionDto>>>> Get() =>
        await Mediator.Send(new GetMyProfessionsQuery(CurrentCharacterGuid));
}
