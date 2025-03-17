using Application.Interfaces.Services.AdminDashboard;
using Domain.Models.Entities.Creatures;
using MediatR;

namespace Application.UseCases._AdminDashboard.Creatures.Queries.GetCreatures;
public record GetCreaturesQuery() : IRequest<List<Creature>>;

public class GetCreaturesQueryHandler : IRequestHandler<GetCreaturesQuery, List<Creature>>
{
    private readonly ICreatureService _creatureService;
    public GetCreaturesQueryHandler(ICreatureService creatureService)
    {
        _creatureService = creatureService;
    }

    public async Task<List<Creature>> Handle(GetCreaturesQuery request, CancellationToken cancellationToken)
    {
        return await _creatureService.GetCreaturesAsync(cancellationToken);
    }
}
