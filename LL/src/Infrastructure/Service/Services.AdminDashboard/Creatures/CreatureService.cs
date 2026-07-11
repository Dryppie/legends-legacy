using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Entities.Creatures;
using Services.AdminDashboard.JsonReaders;

namespace Services.AdminDashboard.Creatures;
public class CreatureService : ICreatureService
{
    private readonly ICreatureRepository _creatureRepository;
    private readonly CreatureJsonReader _creatureReader;
    public CreatureService(ICreatureRepository creatureRepository)
    {
        _creatureRepository = creatureRepository;
        _creatureReader = new();
    }

    public async Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken)
    {
        return _creatureReader.GetCreaturesFromJson();
        //return await _creatureRepository.GetCreaturesAsync(cancellationToken);
    }

    public Task<List<Guid>> GetCreaturesByKey(IReadOnlyList<string> enemyCreatureKeys, CancellationToken cancellationToken)
    {
        return _creatureRepository.GetCreaturesByKey(enemyCreatureKeys, cancellationToken);
        //return await _creatureRepository.GetCreaturesByNameAsync(enemyCreatureKeys, cancellationToken);
    }


    public async Task UpdateCreatureAsync(CreatureDto creatureToUpdate, CancellationToken cancellationToken)
    {
        _creatureReader.UpdateCreatureFromCreature(creatureToUpdate);
        //return await _creatureRepository.UpdateCreatureAsync(cancellationToken);
    }
}
