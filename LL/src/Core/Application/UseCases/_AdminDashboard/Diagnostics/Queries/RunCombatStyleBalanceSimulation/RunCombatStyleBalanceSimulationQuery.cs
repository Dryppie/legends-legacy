using Application.Interfaces.Services.LL.CombatStyles;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.RunCombatStyleBalanceSimulation;

public sealed record RunCombatStyleBalanceSimulationQuery(CombatStyleBalanceSimulationRequest Request)
    : IRequest<CombatStyleBalanceSimulationReport>;

public sealed class RunCombatStyleBalanceSimulationQueryHandler
    : IRequestHandler<RunCombatStyleBalanceSimulationQuery, CombatStyleBalanceSimulationReport>
{
    private readonly ICombatStyleBalanceSimulator _simulator;

    public RunCombatStyleBalanceSimulationQueryHandler(ICombatStyleBalanceSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<CombatStyleBalanceSimulationReport> Handle(
        RunCombatStyleBalanceSimulationQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_simulator.Run(request.Request));
    }
}
