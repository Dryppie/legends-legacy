using Application.Common.Interfaces;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Application.MediatR.Behaviors;
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDbContext _db;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IDbContext db,
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
        var isCommand = request is ICommandBase;
        var isOptOut = request.GetType().IsDefined(typeof(NonTransactionalAttribute), inherit: true);
        if (!isCommand || isOptOut)
            return await next();

        if (_db.CurrentTransaction is not null)
        {
            var resp = await next();
            if (_db.HasChanges)
            {
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var e in ex.Entries)
                    {
                        _logger.LogError("Concurrency on {Entity} with key {KeyValues}",
                            e.Metadata.Name,
                            string.Join(",", e.Properties.Where(p => p.Metadata.IsPrimaryKey())
                                                         .Select(p => p.CurrentValue)));
                    }
                    throw;
                }
            }
            return resp;
        }

        var strategy = _db.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.BeginTransactionAsync(ct);
            try
            {
                var response = await next();

                if (_db.HasChanges)
                {
                    try
                    {
                        await _db.SaveChangesAsync(ct);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        foreach (var e in ex.Entries)
                        {
                            _logger.LogError("Concurrency on {Entity} with key {KeyValues}",
                                e.Metadata.Name,
                                string.Join(",", e.Properties.Where(p => p.Metadata.IsPrimaryKey())
                                                             .Select(p => p.CurrentValue)));
                        }
                        throw;
                    }
                }

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

