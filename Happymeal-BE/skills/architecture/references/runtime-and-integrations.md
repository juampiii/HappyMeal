# Runtime and Integrations

## API Startup

`src/Happymeal.API/Program.cs`:

```text
WebApplication.CreateBuilder
  -> AddApiServices             controllers, ProblemDetails, OpenAPI, ICurrentUser (HttpCurrentUser)
  -> AddApplicationServices     command/query handlers, ISender, domain event handlers
  -> AddInfrastructure          DbContext + AuditableInterceptor, repositories, UnitOfWork, outbox, health checks
  -> Build
  -> MigrateAsync (Development only)
  -> UseApiServices             exception handler, status code pages, OpenAPI + Scalar (Development), HTTPS redirect (non-Development), /health, controllers
  -> Run
```

There is no Aspire, no ServiceDefaults and no MigrationService. Logging is the default ASP.NET Core logging. There is no authentication yet (`// TODO` in `ApiServiceExtensions`).

## Local Development

1. `docker compose up -d` starts PostgreSQL 18 on `localhost:5432` (container `happymeal-postgres`, database `happymeal`, named volume `happymeal-postgres-data`).
2. `dotnet run --project src/Happymeal.API` starts the API. In Development it applies pending migrations at startup.
3. Scalar UI: `/scalar/v1`. OpenAPI document: `/openapi/v1.json`. Health: `/health` (includes a DbContext check).

The PostgreSQL user and password in `docker-compose.yml` and `appsettings.Development.json` is a local development value only. `appsettings.json` keeps the connection string empty. Other environments must provide `ConnectionStrings__Happymeal`. Startup fails fast when it is missing.

## Database and Migrations

- EF Core 10 with PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`). `HappymealDbContext` has default schema `Happymeal` and applies `IEntityTypeConfiguration<>` classes from the Infrastructure assembly. Table and column names keep the EF PascalCase names, so raw SQL must quote them (`"Happymeal"."Recipes"`).
- `AddDbContext<HappymealDbContext>` is scoped, with retries on (`EnableRetryOnFailure`) and the `AuditableInterceptor`. The outbox dispatcher creates its own scope per batch.
- `OnModelCreating` configures the audit columns of every `IAuditable` entity (`CreatedBy` required, both user columns max `IAuditable.UserMaxLength`).
- PostgreSQL compares text case-sensitively. Use `EF.Functions.ILike` or a case-insensitive collation for name searches.
- `timestamptz` columns only accept `DateTimeOffset` values with a UTC offset. Always take time from `TimeProvider.GetUtcNow()`.

## Audit Fields

`Database/Interceptors/AuditableInterceptor` is a `SaveChangesInterceptor`. On every save it sets `CreatedOn`/`CreatedBy` for added `IAuditable` entities and `ModifiedOn`/`ModifiedBy` for modified ones. Time comes from `TimeProvider`; the user comes from `ICurrentUser`.

`ICurrentUser` (Application) is implemented by `HttpCurrentUser` (API). It returns the `NameIdentifier` or `sub` claim, `"anonymous"` for a request without a user, and `"system"` outside a request (for example the outbox dispatcher).

Because it is an interceptor, every save is stamped, including `SeedAsync` in integration tests. Deleted entities are not stamped.
- Migrations live in `Database/Migrations`. Use the local tool from the `Happymeal-BE` folder:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Happymeal.Infrastructure --startup-project src/Happymeal.Infrastructure --output-dir Database/Migrations
```

`DesignTimeHappymealDbContext` supplies the design-time context, so the API is not needed as startup project.

- Outside Development nothing migrates automatically. Apply migrations from the pipeline (for example an idempotent script with `dotnet ef migrations script --idempotent`). Change this deliberately if the strategy changes.

## Outbox Dispatcher

`Outbox/OutboxDispatcher` is a `BackgroundService` that polls `Happymeal.OutboxMessages` on a `PeriodicTimer`:

- It claims a batch with one SQL statement: `UPDATE ... WHERE "Id" IN (SELECT ... FOR UPDATE SKIP LOCKED) RETURNING`. It stamps `ClaimedAt` and returns the rows. Several API instances can run without handing the same message to two workers. A partial index on `OccurredOn` (`WHERE "ProcessedOn" IS NULL`) keeps the poll cheap.
- For each message it resolves the event type from `Happymeal.Domain`, checks `IDomainEventHandlerCatalog`, deserializes the payload and calls `IDomainEventDispatcher.DispatchAsync` in a new DI scope.
- Success sets `ProcessedOn`. Failure increments `Attempts` and stores `Error`. When `Attempts` reaches `MaxAttempts` the message is dead-lettered (`DeadLetteredOn` is set) and leaves the poll.
- A `ClaimedAt` older than `ClaimTimeout` makes an unfinished message eligible again. Delivery is at least once.

`OutboxOptions` (section `Outbox`, validated on start): `PollInterval` (default 2 s), `BatchSize` (20), `MaxAttempts` (5), `ClaimTimeout` (1 min).

The raw SQL uses the quoted PostgreSQL name `"Happymeal"."OutboxMessages"`. Update it if the schema or provider changes.

There is no message broker. If an event must leave the service, add an outbox handler in Infrastructure that publishes it with the plain `Azure.Messaging.ServiceBus` SDK (no MassTransit). Ask before adding the package and the emulator to `docker-compose.yml`.

## OpenAPI

`Microsoft.AspNetCore.OpenApi` generates the document. `Scalar.AspNetCore` serves the UI. Both are mapped only in Development. Describe responses with `[ProducesResponseType<T>(status)]`, and use `ProblemDetails`/`ValidationProblemDetails` as the error types. Routes are not versioned.

## Health Checks

`/health` runs the registered checks. Infrastructure adds `AddDbContextCheck<HappymealDbContext>()`.
