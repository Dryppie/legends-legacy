using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;

namespace Application.Interfaces.Services.LL.Entities;

public interface ICreatureBuildProfileDiagnostics
{
    CreatureBuildProfileDiagnostic Create(Creature creature, Area area);
    Task<CreatureBuildProfileDiagnosticReport> CreateReportAsync(CancellationToken cancellationToken);
}

public sealed record CreatureBuildProfileDiagnosticReport(
    int CreaturesChecked,
    int ScenariosChecked,
    IReadOnlyList<CreatureBuildProfileDiagnostic> Diagnostics,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors)
{
    public bool HasWarnings => Warnings.Count > 0;
    public bool HasErrors => Errors.Count > 0;
}
