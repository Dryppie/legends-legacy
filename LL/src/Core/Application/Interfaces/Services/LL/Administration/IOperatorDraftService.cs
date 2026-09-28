using Common.Primitives;
using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface IOperatorDraftService
{
    Task<Response<OperatorDraft>> GetAsync(string actor, string key, CancellationToken ct);
    Task<Response<OperatorDraft>> SaveAsync(string actor, string key, Guid expectedVersion, string content, CancellationToken ct);
}
