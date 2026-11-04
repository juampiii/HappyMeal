---
name: integration-tests-test-infrastructure
description: Consult for the API factory, PostgreSQL container, Respawn reset, collection fixture and TestBase.
---

## Pieces

All in `tests/Happymeal.IntegrationTests/Infrastructure/`:

| Type | Role |
|------|------|
| `HappymealApiFactory` | `WebApplicationFactory<Program>` + `IAsyncLifetime`. Starts a `postgres:18-alpine` container, points `ConnectionStrings:Happymeal` to it, uses environment `Testing`, applies migrations, creates the Respawner (`DbAdapter.Postgres`). |
| `HappymealCollection` | `[CollectionDefinition]` with `ICollectionFixture<HappymealApiFactory>`. One container and one API host for the whole run. |
| `TestBase` | `[Collection(HappymealCollection.Name)]`. Gives `Factory` and `Client`, resets the database in `InitializeAsync`, and offers `SeedAsync(...)` and `QueryAsync(...)` helpers that use a fresh DI scope. |

## Lifecycle

```text
first test in the collection
  -> HappymealApiFactory.InitializeAsync
       start SQL container -> build host (Testing) -> MigrateAsync -> Respawner.CreateAsync
each test
  -> TestBase.InitializeAsync -> ResetDatabaseAsync (Respawn, schema Happymeal, keeps __EFMigrationsHistory)
end of run
  -> DisposeAsync (connection, container, host)
```

Because all tests share one collection, they run sequentially. That is required: Respawn resets a database that all tests share.

## Environment

- `Testing` is not `Development`, so `Program` does not migrate at startup. The factory migrates explicitly.
- OpenAPI and Scalar are not mapped in `Testing`. HTTPS redirection is registered, but it does nothing in tests because the test server has no HTTPS port.
- The `OutboxDispatcher` hosted service runs. Its default poll interval is 2 seconds. Override `Outbox:PollInterval` with `builder.UseSetting(...)` in the factory if delivery tests become slow.

## Overriding services

Override services in `HappymealApiFactory.ConfigureWebHost` with `builder.ConfigureTestServices(...)` (add `Microsoft.AspNetCore.TestHost` usings). Keep overrides in the factory, not in individual tests, so all tests share one host. Typical candidates: `TimeProvider` (use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` only after asking to add the package), or a recording `IDomainEventHandler<T>` for delivery tests.

## Requirements

Docker must be running. The first run pulls the PostgreSQL image.
