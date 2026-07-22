using Application.Interfaces.Services.LL.Dungeons;
using Domain.Components.Attributes;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.RunDungeonSimulation;

public sealed record GetDungeonSimulationOptionsQuery : IRequest<DungeonSimulationOptions>;

public sealed class GetDungeonSimulationOptionsQueryHandler
    : IRequestHandler<GetDungeonSimulationOptionsQuery, DungeonSimulationOptions>
{
    private readonly IDungeonRunSimulator _simulator;

    public GetDungeonSimulationOptionsQueryHandler(IDungeonRunSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<DungeonSimulationOptions> Handle(
        GetDungeonSimulationOptionsQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_simulator.GetOptions());
    }
}

public sealed record RunDungeonSimulationQuery(DungeonSimulationRequest Request)
    : IRequest<DungeonSimulationReport>;

public sealed class RunDungeonSimulationQueryHandler
    : IRequestHandler<RunDungeonSimulationQuery, DungeonSimulationReport>
{
    private readonly IDungeonRunSimulator _simulator;

    public RunDungeonSimulationQueryHandler(IDungeonRunSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<DungeonSimulationReport> Handle(
        RunDungeonSimulationQuery request,
        CancellationToken cancellationToken) =>
        _simulator.RunAsync(request.Request, cancellationToken);
}

public sealed record GetDungeonSimulationCombatRatingQuery(DungeonSimulationCharacter Character)
    : IRequest<CombatRatingBreakdown>;

public sealed class GetDungeonSimulationCombatRatingQueryHandler
    : IRequestHandler<GetDungeonSimulationCombatRatingQuery, CombatRatingBreakdown>
{
    private readonly IDungeonRunSimulator _simulator;

    public GetDungeonSimulationCombatRatingQueryHandler(IDungeonRunSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<CombatRatingBreakdown> Handle(
        GetDungeonSimulationCombatRatingQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_simulator.GetCombatRating(request.Character));
    }
}
