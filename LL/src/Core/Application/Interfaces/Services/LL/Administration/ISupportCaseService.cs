using Common.Primitives;
using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface ISupportCaseService
{
    Task<Response<SupportCaseDetails>> ApplyAsync(SupportCaseChange change, AdministrationActor actor, CancellationToken ct);
    Task<Response<SupportCaseDetails>> GetAsync(Guid id, int? beforeSequence, CancellationToken ct);
    Task<SupportCasePage> SearchAsync(Guid? characterId, SupportCaseStatus? status, string? search, int page, CancellationToken ct);
}
