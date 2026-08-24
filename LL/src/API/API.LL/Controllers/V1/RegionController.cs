using Application.UseCases.Regions.Queries.GetRegion;
using Domain.Models.Regions;
using Application.UseCases.Regions.Dtos;
using Application.UseCases.Regions.Queries.GetRegionGatheringPreview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;
[Authorize]

public class RegionController : BaseController
{
    [HttpGet("{regionId}")]
    public async Task<ActionResult<Region>> GetRegion(int regionId)
    {
        return await Mediator.Send(new GetRegionQuery(regionId));
    }

    [HttpGet("{regionId}/gathering")]
    public async Task<ActionResult<RegionGatheringPreviewDto>> GetGatheringPreview(
        int regionId)
    {
        return await Mediator.Send(new GetRegionGatheringPreviewQuery(regionId));
    }
}
