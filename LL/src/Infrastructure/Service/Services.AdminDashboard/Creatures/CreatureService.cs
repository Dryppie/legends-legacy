using Application.Common.Interfaces;
using Application.Interfaces.Services.AdminDashboard;
using Domain.Models.Entities.Creatures;

namespace Services.AdminDashboard.Creatures;
public class CreatureService : ICreatureService
{
    private readonly ICreatureRepository _creatureRepository;
    public CreatureService(ICreatureRepository creatureRepository, IDbContext context)
    {
        _creatureRepository = creatureRepository;
    }

    public async Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken)
    {
        return await _creatureRepository.GetCreaturesAsync(cancellationToken);
    }
}
