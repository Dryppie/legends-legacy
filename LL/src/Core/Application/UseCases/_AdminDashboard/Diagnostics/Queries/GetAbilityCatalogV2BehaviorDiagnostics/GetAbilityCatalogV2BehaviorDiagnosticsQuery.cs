using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogV2BehaviorDiagnostics;

public record GetAbilityCatalogV2BehaviorDiagnosticsQuery() : IRequest<AbilityCatalogV2BehaviorDiagnosticReport>;

public sealed class GetAbilityCatalogV2BehaviorDiagnosticsQueryHandler
    : IRequestHandler<GetAbilityCatalogV2BehaviorDiagnosticsQuery, AbilityCatalogV2BehaviorDiagnosticReport>
{
    private readonly IAbilityCatalogV2BehaviorDiagnostics _diagnostics;

    public GetAbilityCatalogV2BehaviorDiagnosticsQueryHandler(IAbilityCatalogV2BehaviorDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<AbilityCatalogV2BehaviorDiagnosticReport> Handle(
        GetAbilityCatalogV2BehaviorDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_diagnostics.Analyze());
}
