using Application.Interfaces.Services.LL.Balance;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.RunEquipmentCombatPacing;

public sealed record RunEquipmentCombatPacingQuery(EquipmentCombatPacingRequest Request)
    : IRequest<EquipmentCombatPacingReport>;

public sealed class RunEquipmentCombatPacingQueryHandler
    : IRequestHandler<RunEquipmentCombatPacingQuery, EquipmentCombatPacingReport>
{
    private readonly IEquipmentCombatPacingAnalyzer _analyzer;

    public RunEquipmentCombatPacingQueryHandler(IEquipmentCombatPacingAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public Task<EquipmentCombatPacingReport> Handle(
        RunEquipmentCombatPacingQuery request,
        CancellationToken cancellationToken) =>
        _analyzer.AnalyzeAsync(request.Request, cancellationToken);
}
