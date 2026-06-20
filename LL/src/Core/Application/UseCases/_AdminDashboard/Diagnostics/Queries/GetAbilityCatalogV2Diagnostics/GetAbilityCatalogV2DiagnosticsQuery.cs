using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogV2Diagnostics;

public record GetAbilityCatalogV2DiagnosticsQuery() : IRequest<AbilityCatalogV2DiagnosticReport>;

public sealed class GetAbilityCatalogV2DiagnosticsQueryHandler
    : IRequestHandler<GetAbilityCatalogV2DiagnosticsQuery, AbilityCatalogV2DiagnosticReport>
{
    private readonly IAbilityCatalogV2Diagnostics _diagnostics;

    public GetAbilityCatalogV2DiagnosticsQueryHandler(IAbilityCatalogV2Diagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public Task<AbilityCatalogV2DiagnosticReport> Handle(
        GetAbilityCatalogV2DiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_diagnostics.RunTrainingEncounter());
}
