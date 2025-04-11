using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Entities.Creatures;

namespace Application.Interfaces.Services.AdminDashboard;
public interface ICreatureService
{
    Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken);
    Task UpdateCreatureAsync(CreatureDto creatureToUpdate, CancellationToken cancellationToken);
}
