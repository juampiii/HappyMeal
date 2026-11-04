using Happymeal.Domain.Base;

namespace Happymeal.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public const int MaxErrorLength = 2048;

    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public DateTimeOffset? DeadLetteredOn { get; private set; }
    public DateTimeOffset? ClaimedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
    public Guid CorrelationId { get; private set; }

    public static OutboxMessage Create(DomainEvent domainEvent, DateTimeOffset occurredOn)
    {
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = domainEvent.GetType().FullName!,
            Payload = OutboxSerializer.Serialize(domainEvent),
            OccurredOn = occurredOn,
            CorrelationId = Guid.NewGuid()
        };
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedOn = now;
    }

    public void RecordFailure(string error)
    {
        Attempts++;
        Error = Truncate(error);
    }

    public void MarkDeadLettered(DateTimeOffset now)
    {
        ProcessedOn = now;
        DeadLetteredOn = now;
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
    }

    // Required by EF Core
    private OutboxMessage()
    {
    }
}
