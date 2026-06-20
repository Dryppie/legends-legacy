using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogDiagnostics;

public record GetAbilityCatalogDiagnosticsQuery() : IRequest<AbilityCatalogDiagnosticReport>;

public sealed class GetAbilityCatalogDiagnosticsQueryHandler
    : IRequestHandler<GetAbilityCatalogDiagnosticsQuery, AbilityCatalogDiagnosticReport>
{
    private readonly IAbilityCatalogDiagnostics _diagnostics;

    public GetAbilityCatalogDiagnosticsQueryHandler(IAbilityCatalogDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<AbilityCatalogDiagnosticReport> Handle(
        GetAbilityCatalogDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_diagnostics.RunTrainingEncounter());
}
