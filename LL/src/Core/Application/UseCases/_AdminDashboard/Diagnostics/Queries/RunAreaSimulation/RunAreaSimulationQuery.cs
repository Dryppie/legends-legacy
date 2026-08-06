using Application.Interfaces.Services.LL.Regions;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.RunAreaSimulation;

public sealed record GetAreaSimulationOptionsQuery : IRequest<AreaSimulationOptions>;

public sealed class GetAreaSimulationOptionsQueryHandler
    : IRequestHandler<GetAreaSimulationOptionsQuery, AreaSimulationOptions>
{
    private readonly IAreaCombatSimulator _simulator;

    public GetAreaSimulationOptionsQueryHandler(IAreaCombatSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<AreaSimulationOptions> Handle(
        GetAreaSimulationOptionsQuery request,
        CancellationToken cancellationToken) =>
        _simulator.GetOptionsAsync(cancellationToken);
}

public sealed record RunAreaSimulationQuery(AreaSimulationRequest Request)
    : IRequest<AreaSimulationReport>;

public sealed class RunAreaSimulationQueryHandler
    : IRequestHandler<RunAreaSimulationQuery, AreaSimulationReport>
{
    private readonly IAreaCombatSimulator _simulator;

    public RunAreaSimulationQueryHandler(IAreaCombatSimulator simulator)
    {
        _simulator = simulator;
    }

    public Task<AreaSimulationReport> Handle(
        RunAreaSimulationQuery request,
        CancellationToken cancellationToken) =>
        _simulator.RunAsync(request.Request, cancellationToken);
}

public sealed record AnalyzeRegionAreaBalanceQuery(RegionAreaBalanceRequest Request)
    : IRequest<RegionAreaBalanceReport>;

public sealed class AnalyzeRegionAreaBalanceQueryHandler
    : IRequestHandler<AnalyzeRegionAreaBalanceQuery, RegionAreaBalanceReport>
{
    private readonly IRegionAreaBalanceAnalyzer _analyzer;

    public AnalyzeRegionAreaBalanceQueryHandler(IRegionAreaBalanceAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<RegionAreaBalanceReport> Handle(
        AnalyzeRegionAreaBalanceQuery request,
        CancellationToken cancellationToken) =>
        _analyzer.AnalyzeAsync(request.Request, cancellationToken);
}
