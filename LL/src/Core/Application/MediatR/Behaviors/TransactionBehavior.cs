using Application.MediatR.Attributes;
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
        // Only wrap ICommand<>, unless explicitly opted out
        var isCommand = request is ICommandBase;
        var isOptOut = request.GetType().IsDefined(typeof(NonTransactionalAttribute), inherit: true);
        if (!isCommand || isOptOut)
            return await next();

        if (_db.Database.CurrentTransaction is not null)
        {
            var resp = await next();
            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(ct);
            return resp;
        }

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
                return response;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(ct); }
                catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed."); }
                _logger.LogError(ex, "Command {Command} failed; tx rolled back.", typeof(TRequest).Name);
                throw;
            }
        });
    }
}

