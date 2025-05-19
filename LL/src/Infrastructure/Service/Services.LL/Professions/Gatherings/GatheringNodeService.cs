using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Services.LL.Professions.Gatherings;
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