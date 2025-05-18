using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions.Gatherings;
public class GatheringNodeRepository : IGatheringNodeRepository
{
    private readonly IDbContext _context;
    public GatheringNodeRepository(IDbContext context)
    {
        _context = context;
    }
    public async Task<GatheringNode> GetGatheringNodeByIdAsync(string gatheringNodeId, CancellationToken cancellationToken)
    {
        var gatheringNode = await _context.GatheringNodes
                .FirstOrDefaultAsync(g => g.Id.Equals(gatheringNodeId), cancellationToken);

        NotFoundException.ThrowIfNull(gatheringNode, nameof(gatheringNode), gatheringNodeId);

        return gatheringNode;
    }
}