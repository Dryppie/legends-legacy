namespace Domain.Models.Administration;

public interface IOperatorDraftRepository
{
    Task LockAsync(string actor, CancellationToken ct);
    Task<OperatorDraft?> GetAsync(string actor, string key, CancellationToken ct);
    void Add(OperatorDraft draft);
}
