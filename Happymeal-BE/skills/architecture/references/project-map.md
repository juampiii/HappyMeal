# Project Map

Happymeal-BE is a .NET 10 (C# 14) solution with a pragmatic Clean Architecture. It has no Aspire host and no platform packages.

## Solution Layout

```text
Happymeal.slnx
├── src/
│   ├── Happymeal.Common          local artifact that replaces DYCommon
│   ├── Happymeal.Domain
│   ├── Happymeal.Application
│   ├── Happymeal.Infrastructure
│   └── Happymeal.API
├── tests/
│   ├── Happymeal.UnitTests
│   └── Happymeal.IntegrationTests
├── docker-compose.yml            local PostgreSQL
├── global.json                   SDK 10.0.400+, Microsoft.Testing.Platform runner
└── .config/dotnet-tools.json     dotnet-ef
```

Folders, project files, namespaces and assembly names all use `Happymeal.*` (no hyphen, unlike Catalog-BE).

## Project Responsibilities

- `Happymeal.Common`: small shared building blocks with no business meaning. `Results/Result`, `Result<T>`, `Error`, `ErrorType` (the single result type for every layer), `Validation/IValidatable`, `ValidationFailure` and `ToValidationError()`, `DomainEvents/IDomainEvent`, `IDomainEventHandler<TDomainEvent>`, `IDomainEventDispatcher` + `DomainEventDispatcher`, `IDomainEventHandlerCatalog`, and the `AddDomainEventHandlers(assembly)` DI scanner. It is a project in the solution, not a NuGet package. Extract it to a package only when another service needs it.
- `Happymeal.Domain`: entities (`Recipe`), base types (`Entity`, `AggregateRoot`, `IAuditable`, `DomainEvent`), params records that validate themselves (`Params/`, `IValidatable`), and domain events (`Events/`, `<Entity><Verb>DomainEvent`).
- `Happymeal.Application`: CQRS contracts (`Common/Messaging`: `ICommand`, `IQuery`, their handlers and `ISender`), repository abstractions (`Common/Interfaces`), feature slices (`Features/<Feature>`), and `Configuration/ServiceExtensions.AddApplicationServices`.
- `Happymeal.Infrastructure`: `HappymealDbContext` (schema `Happymeal`), EF configurations and migrations (`Database/`), repositories and `UnitOfWork` (`Repositories/`), the outbox (`Outbox/`), and `Configuration/ServiceExtensions.AddInfrastructure`.
- `Happymeal.API`: controllers, request DTOs (`Dtos/`), `ControllerExtensions.ToActionResult`, `ApiServiceExtensions` (controllers, ProblemDetails, OpenAPI, Scalar, health checks), and `Program.cs` as composition root.
- `Happymeal.UnitTests`: isolated tests for Domain, Application and pure Infrastructure helpers.
- `Happymeal.IntegrationTests`: `WebApplicationFactory<Program>` tests against a PostgreSQL Testcontainer, reset with Respawn.

## Dependency Direction

```text
Happymeal.Common
  ↑
Happymeal.Domain
  ↑
Happymeal.Application
  ↑
Happymeal.Infrastructure
  ↑
Happymeal.API
```

Actual project references:

- `Happymeal.Common` has no project references.
- `Happymeal.Domain` references `Happymeal.Common`.
- `Happymeal.Application` references `Happymeal.Domain` and `Happymeal.Common`.
- `Happymeal.Infrastructure` references `Happymeal.Application`, `Happymeal.Domain` and `Happymeal.Common`.
- `Happymeal.API` references `Happymeal.Application`, `Happymeal.Domain` and `Happymeal.Infrastructure`.
- `Happymeal.UnitTests` references `Happymeal.Application`, `Happymeal.Domain` and `Happymeal.Infrastructure`.
- `Happymeal.IntegrationTests` references `Happymeal.API`.

## Main Packages

| Project | Packages |
|---------|----------|
| Common | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| Domain | none (only `Happymeal.Common`) |
| Application | `Microsoft.Extensions.Logging.Abstractions` |
| Infrastructure | EF Core 10 + Design, `Npgsql.EntityFrameworkCore.PostgreSQL`, EF health check, Options (configuration + data annotations) |
| API | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` |
| UnitTests | xUnit v3, NSubstitute, AwesomeAssertions |
| IntegrationTests | xUnit v3, AwesomeAssertions, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, Respawn |

All packages are free for commercial use. Do not add MediatR, MassTransit, FluentAssertions 8+ or other packages with a commercial license.

## Architectural Character

This is a CQRS-style application without MediatR: commands, queries, handlers and `ISender` are in-house contracts. Each Application use case groups its nested `Command` or `Query` and `Handler` in a public static `<UseCase>Feature` class in a matching file. Existing Commands/Queries folders and namespaces remain; the Feature suffix prevents namespace collisions. Domain events are delivered by an in-house transactional outbox and the `Happymeal.Common` dispatcher. For the mechanics, read [`layers-and-flows.md`](layers-and-flows.md).
