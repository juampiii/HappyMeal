using Happymeal.Common.DomainEvents;
using Happymeal.Common.Results;
using Happymeal.Domain.Base;
using Happymeal.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Happymeal.Infrastructure.Outbox;

internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IDomainEventHandlerCatalog catalog,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Unhandled error while dispatching outbox messages.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // WaitForNextTickAsync throws on shutdown instead of returning false.
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var batchScope = scopeFactory.CreateAsyncScope();
        var dbContext = batchScope.ServiceProvider.GetRequiredService<HappymealDbContext>();

        var claimed = await ClaimBatchAsync(dbContext, cancellationToken);
        foreach (var message in claimed)
        {
            await ProcessMessageAsync(dbContext, message, cancellationToken);
        }
    }

    private async Task<List<OutboxMessage>> ClaimBatchAsync(
        HappymealDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var claimCutoff = now - _options.ClaimTimeout;

        // SKIP LOCKED skips rows another instance already holds, so two workers never get the same message.
        var claimed = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                                  UPDATE "Happymeal"."OutboxMessages" AS m
                                  SET "ClaimedAt" = {now}
                                  WHERE m."Id" IN (
                                      SELECT "Id"
                                      FROM "Happymeal"."OutboxMessages"
                                      WHERE "ProcessedOn" IS NULL
                                        AND "Attempts" < {_options.MaxAttempts}
                                        AND ("ClaimedAt" IS NULL OR "ClaimedAt" < {claimCutoff})
                                      ORDER BY "OccurredOn", "Id"
                                      LIMIT {_options.BatchSize}
                                      FOR UPDATE SKIP LOCKED
                                  )
                                  RETURNING m.*
                                  """)
            .AsTracking()
            .ToListAsync(cancellationToken);

        // RETURNING does not keep the order of the subquery
        return claimed.OrderBy(m => m.OccurredOn).ThenBy(m => m.Id).ToList();
    }

    private async Task ProcessMessageAsync(
        HappymealDbContext dbContext, OutboxMessage message, CancellationToken cancellationToken)
    {
        var eventType = typeof(DomainEvent).Assembly.GetType(message.Type);
        if (eventType is null)
        {
            await RecordFailureAsync(dbContext, message, $"Unknown event type '{message.Type}'.", cancellationToken);
            return;
        }

        if (catalog.GetHandlerTypes(eventType).Count == 0)
        {
            logger.LogDebug("Outbox message {MessageId} of type {Type} has no subscribers.", message.Id, message.Type);
            message.MarkProcessed(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var deserializeResult = OutboxSerializer.Deserialize(message.Payload, eventType);
        if (deserializeResult.IsFailure)
        {
            await RecordFailureAsync(dbContext, message, deserializeResult.Error!.Message, cancellationToken);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var result = await DispatchAsync(dispatcher, eventType, deserializeResult.Value, message.CorrelationId,
            cancellationToken);
        if (result.IsFailure)
        {
            await RecordFailureAsync(dbContext, message, result.Error!.Message, cancellationToken);
            return;
        }

        message.MarkProcessed(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordFailureAsync(
        HappymealDbContext dbContext, OutboxMessage message, string error, CancellationToken cancellationToken)
    {
        message.RecordFailure(error);

        if (message.Attempts >= _options.MaxAttempts)
        {
            message.MarkDeadLettered(timeProvider.GetUtcNow());
            logger.LogCritical(
                "Outbox message {MessageId} of type {Type} (correlation {CorrelationId}) exhausted {MaxAttempts} attempts: {Error}",
                message.Id, message.Type, message.CorrelationId, _options.MaxAttempts, error);
        }
        else
        {
            logger.LogWarning(
                "Outbox message {MessageId} of type {Type} (correlation {CorrelationId}) failed (attempt {Attempt}/{MaxAttempts}): {Error}",
                message.Id, message.Type, message.CorrelationId, message.Attempts, _options.MaxAttempts, error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Result> DispatchAsync(
        IDomainEventDispatcher dispatcher, Type eventType, DomainEvent domainEvent, Guid correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var dispatchMethod = typeof(IDomainEventDispatcher)
                .GetMethod(nameof(IDomainEventDispatcher.DispatchAsync))!
                .MakeGenericMethod(eventType);

            var dispatchTask =
                (Task<Result>)dispatchMethod.Invoke(dispatcher, [domainEvent, correlationId, cancellationToken])!;
            return await dispatchTask;
        }
        catch (Exception exception)
        {
            return Error.Failure(exception.Message);
        }
    }
}
