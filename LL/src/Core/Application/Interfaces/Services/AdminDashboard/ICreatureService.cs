using Domain.Models.Entities.Creatures;

namespace Application.Interfaces.Services.AdminDashboard;
public interface ICreatureService
{
    Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken);
    Task<Creature> UpdateCreatureAsync(CancellationToken cancellationToken);
}
