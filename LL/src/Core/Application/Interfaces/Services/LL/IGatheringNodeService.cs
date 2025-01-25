using Domain.Models.GatheringNodes;

namespace Application.Interfaces.Services.LL;
public interface IGatheringNodeService
{
    Task<GatheringNode> GetGatheringNodeById(string gatheringNodeId, CancellationToken cancellationToken);
}