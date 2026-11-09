---
name: integration-tests-async-side-effects
description: Consult when a test must wait for the outbox to deliver, retry or dead-letter a domain event.
---

## Why polling

`UnitOfWork.SaveEntitiesAsync` only writes `OutboxMessage` rows. `OutboxDispatcher` delivers them later on its own timer. The HTTP response comes back before any handler runs.

## Assert staging (no waiting)

Right after the call, the row exists in the same transaction as the entity:

```csharp
var messages = await QueryAsync(db => db.OutboxMessages.AsNoTracking().ToListAsync());
messages.Should().ContainSingle(m => m.Type == typeof(RecipeCreatedDomainEvent).FullName);
```

## Assert delivery (poll with a timeout)

Poll until a condition holds or a timeout expires. Never use a fixed `Task.Delay`.

```csharp
private async Task<T> WaitForAsync<T>(Func<Task<T>> probe, Func<T, bool> isDone, TimeSpan? timeout = null)
{
    var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
    while (true)
    {
        var value = await probe();
        if (isDone(value) || DateTime.UtcNow >= deadline)
        {
            return value;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(250));
    }
}

var processed = await WaitForAsync(
    () => QueryAsync(db => db.OutboxMessages.AsNoTracking().SingleAsync()),
    m => m.ProcessedOn is not null);

processed.ProcessedOn.Should().NotBeNull();
processed.DeadLetteredOn.Should().BeNull();
```

Move `WaitForAsync` to `TestBase` the first time a second test class needs it.

## Assert the handler effect

When a handler writes state, poll that state instead of the outbox row. When the handler only logs, `ProcessedOn` set and `DeadLetteredOn` null is the proof that it succeeded.

## Failures and dead letters

A handler that returns a failed `Result` or throws increments `Attempts`. After `MaxAttempts` the message gets `DeadLetteredOn`. To test this without waiting for five polls, lower `Outbox:MaxAttempts` and `Outbox:PollInterval` in the factory, and register a failing test handler there.
