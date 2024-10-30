using Application.UseCases.Regions.Queries.GetRegion;
using Domain.Models.Regions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Route("api/[controller]")]
[ApiController]
public class RegionController : BaseController
{
    [HttpGet("regions/{regionId}")]
    public async Task<ActionResult<Region>> GetRegion(int regionId)
    {
        return await Mediator.Send(new GetRegionQuery(regionId));
    }
}
