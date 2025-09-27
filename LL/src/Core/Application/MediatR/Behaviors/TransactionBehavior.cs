using Application.MediatR.Markers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.MediatR.Behaviors;
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly DbContext _db;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(DbContext db,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Only wrap commands
        if (request is not ICommand<TResponse>)
            return await next();

        // If someone already opened a tx, just run inside it and avoid creating a new one
        if (_db.Database.CurrentTransaction is not null)
        {
            var resp = await next();
            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(ct);
            return resp;
        }

        // IMPORTANT: everything that touches the DB must be inside the strategy delegate
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var response = await next();

                if (_db.ChangeTracker.HasChanges())
                    await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                // Optionally: dispatch domain/integration events AFTER commit (Outbox publisher)
                // await _outboxPublisher.FlushAsync(ct);

                return response;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(ct); }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback failed.");
                }
                _logger.LogError(ex, "Command {Command} failed; transaction rolled back.", typeof(TRequest).Name);
                throw;
            }
        });
    }
}
