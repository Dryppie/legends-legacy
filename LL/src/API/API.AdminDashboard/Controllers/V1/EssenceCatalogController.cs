using Application.Interfaces.Services.LL.Essences;
using Application.UseCases._AdminDashboard.Essences.Queries.GetEssenceCatalog;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers.V1;

public class EssenceCatalogController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<EssenceCatalogReport>> GetEssenceCatalog()
    {
        return await Mediator.Send(new GetEssenceCatalogQuery());
    }
}
