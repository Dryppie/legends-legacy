using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogBehaviorDiagnostics;

public record GetAbilityCatalogBehaviorDiagnosticsQuery() : IRequest<AbilityCatalogBehaviorDiagnosticReport>;

public sealed class GetAbilityCatalogBehaviorDiagnosticsQueryHandler
    : IRequestHandler<GetAbilityCatalogBehaviorDiagnosticsQuery, AbilityCatalogBehaviorDiagnosticReport>
{
    private readonly IAbilityCatalogBehaviorDiagnostics _diagnostics;

    public GetAbilityCatalogBehaviorDiagnosticsQueryHandler(IAbilityCatalogBehaviorDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<AbilityCatalogBehaviorDiagnosticReport> Handle(
        GetAbilityCatalogBehaviorDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_diagnostics.Analyze());
}
