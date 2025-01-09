using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases._Simulates;
public record SimulateCombatWithOneEssenceCommand(string EssenceName) : IRequest;

public class SimulateCombatWithOneEssenceCommandHandler : IRequestHandler<SimulateCombatWithOneEssenceCommand>
{
    private readonly ISimulatorService _simulatorService;
    public SimulateCombatWithOneEssenceCommandHandler(ISimulatorService simulatorService)
    {
        _simulatorService = simulatorService;
    }
    public async Task Handle(SimulateCombatWithOneEssenceCommand request, CancellationToken cancellationToken)
    {
        await _simulatorService.SimulateCombatWithOneEssence(request.EssenceName);
    }
}