
namespace Domain.Models.GatheringNodes;
public interface IGatheringNodeRepository
{
    Task<GatheringNode> GetGatheringNodeByIdAsync(string gatheringNodeId, CancellationToken cancellationToken);
}