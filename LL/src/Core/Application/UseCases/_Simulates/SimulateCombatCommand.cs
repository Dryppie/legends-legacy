using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases._Simulates;
public record SimulateCombatCommand(int PlayerTeamSize, int EnemyTeamSize, int Fights, int Tier, int LocationId) : IRequest;

public class SimulateCombatCommandHandler : IRequestHandler<SimulateCombatCommand>
{
    private readonly ISimulatorService _simulatorService;
    public SimulateCombatCommandHandler(ISimulatorService simulatorService)
    {
        _simulatorService = simulatorService;
    }
    public async Task Handle(SimulateCombatCommand request, CancellationToken cancellationToken)
    {
        await _simulatorService.SimulateCombat(request.PlayerTeamSize, request.EnemyTeamSize, request.Fights, request.Tier, request.LocationId);
    }
}