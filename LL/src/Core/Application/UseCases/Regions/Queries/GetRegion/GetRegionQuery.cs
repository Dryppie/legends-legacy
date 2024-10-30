using Application.Interfaces.Services.LL;
using Domain.Models.Regions;
using MediatR;

namespace Application.UseCases.Regions.Queries.GetRegion;
public record GetRegionQuery(int Id) : IRequest<Region>;

public class GetRegionQueryHandler : IRequestHandler<GetRegionQuery, Region>
{
    private readonly IRegionService _regionService;
    public GetRegionQueryHandler(IRegionService regionService)
    {
        _regionService = regionService;
    }

    public async Task<Region> Handle(GetRegionQuery request, CancellationToken cancellationToken)
    {
        return await _regionService.GetRegionByIdAsync(request.Id, cancellationToken);
    }
}
