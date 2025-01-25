using Application.Interfaces.Services.LL;
using Domain.Models.GatheringNodes;

namespace Services.LL.Gatherings;
public class GatheringNodeService : IGatheringNodeService
{
    private readonly IGatheringNodeRepository _gatheringNodeRepository;

    public GatheringNodeService(IGatheringNodeRepository gatheringNodeRepository)
    {
        _gatheringNodeRepository = gatheringNodeRepository;
    }

    public async Task<GatheringNode> GetGatheringNodeById(string gatheringNodeId, CancellationToken cancellationToken)
    {
        return await _gatheringNodeRepository.GetGatheringNodeByIdAsync(gatheringNodeId, cancellationToken);
    }
}