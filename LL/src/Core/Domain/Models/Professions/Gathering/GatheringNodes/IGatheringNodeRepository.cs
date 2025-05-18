namespace Domain.Models.Professions.Gathering.GatheringNodes;
public interface IGatheringNodeRepository
{
    Task<GatheringNode> GetGatheringNodeByIdAsync(string gatheringNodeId, CancellationToken cancellationToken);
}