using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.RunAbilityBalanceSimulation;

public sealed record RunAbilityBalanceSimulationQuery(AbilityBalanceSimulationRequest Request)
    : IRequest<AbilityBalanceSimulationReport>;

public sealed class RunAbilityBalanceSimulationQueryHandler
    : IRequestHandler<RunAbilityBalanceSimulationQuery, AbilityBalanceSimulationReport>
{
    private readonly IAbilityBalanceSimulator _simulator;

    public RunAbilityBalanceSimulationQueryHandler(IAbilityBalanceSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<AbilityBalanceSimulationReport> Handle(
        RunAbilityBalanceSimulationQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_simulator.Run(request.Request));
    }
}
