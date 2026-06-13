using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases._AdminDashboard.Diagnostics.Queries.GetAbilityCatalogDiagnostics;

public record GetAbilityCatalogDiagnosticsQuery() : IRequest<AbilityCatalogSmokeTestReport>;

public sealed class GetAbilityCatalogDiagnosticsQueryHandler
    : IRequestHandler<GetAbilityCatalogDiagnosticsQuery, AbilityCatalogSmokeTestReport>
{
    private readonly IAbilityCatalogSmokeTester _smokeTester;

    public GetAbilityCatalogDiagnosticsQueryHandler(IAbilityCatalogSmokeTester smokeTester)
    {
        _smokeTester = smokeTester;
    }

    public Task<AbilityCatalogSmokeTestReport> Handle(
        GetAbilityCatalogDiagnosticsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_smokeTester.Run());
}
