---
name: architecture
description: Explains the Happymeal-BE architecture - Clean Architecture layers, CQRS-style commands and queries with their handlers and ISender (nested requests and handlers grouped by use case, without MediatR), the unified Result/Error type, domain validation with IValidatable (no FluentValidation), the transactional outbox for domain events and IDomainEventHandler, the ProblemDetails error contract, and AggregateRoot and IAuditable audit fields, client-assignable ids, and runtime setup (EF Core PostgreSQL, docker-compose, OpenAPI + Scalar). Use when onboarding, designing or reviewing a feature, deciding which layer code belongs in, adding a command, query, domain event or domain event handler, adding validation, or changing persistence and startup.
---

# Happymeal-BE Architecture

Use this skill for a project-wide view before you implement, review, debug or explain Happymeal-BE. The mental model is simple: the API receives requests and sends commands and queries, Application handles them, Domain owns business rules, and Infrastructure supplies persistence and background delivery of domain events.

Application use cases group a nested `Command` or `Query` and `Handler` in a public static `<UseCase>Feature` class and matching file, for example `CreateRecipeFeature.Command` and `CreateRecipeFeature.Handler`. Keep the Commands/Queries folders and namespaces. The `Feature` suffix prevents namespace collisions. Happymeal defines its own messaging contracts and follows the domain event naming used in Microsoft's eShop, such as `RecipeCreatedDomainEvent`.

## Available Files

| File | When to read it |
|------|-----------------|
| [`project-map.md`](references/project-map.md) | Solution structure, project responsibilities, dependency direction, packages. |
| [`layers-and-flows.md`](references/layers-and-flows.md) | Layers, commands/queries/handlers and `ISender`, `Result`/`Error`, Unit of Work, outbox and domain event handlers. |
| [`validation-strategy.md`](references/validation-strategy.md) | Where validation lives and the ProblemDetails / ValidationProblemDetails contract. |
| [`runtime-and-integrations.md`](references/runtime-and-integrations.md) | API startup, database, migrations, outbox dispatcher, OpenAPI, health checks, local Docker. |
| [`feature-workflow.md`](references/feature-workflow.md) | Step-by-step guide to add or change a feature. |

## Quick Routing

- Understanding the repository from zero: read [`project-map.md`](references/project-map.md), then [`layers-and-flows.md`](references/layers-and-flows.md).
- Designing a new endpoint, command or query: read [`feature-workflow.md`](references/feature-workflow.md) and [`layers-and-flows.md`](references/layers-and-flows.md).
- Raising a domain event or reacting to one: read the Unit of Work and Domain Event Handlers sections in [`layers-and-flows.md`](references/layers-and-flows.md).
- Adding validation or changing an error response: read [`validation-strategy.md`](references/validation-strategy.md).
- Changing the database model, migrations, startup or configuration: read [`runtime-and-integrations.md`](references/runtime-and-integrations.md).
- Planning test coverage: read [`feature-workflow.md`](references/feature-workflow.md), then use the `unit-tests` or `integration-tests` skill.

## Core Rules

- Respect inward dependencies: Domain only knows `Happymeal.Common`. Application defines commands, queries, handlers and abstractions. Infrastructure implements them. API composes and exposes HTTP.
- Use vertical slices under `Happymeal.Application/Features/<Feature>/{Commands,Queries,DomainEventHandlers}`. For command/query naming and file layout, follow [`feature-workflow.md`](references/feature-workflow.md). This groups Application use cases; the existing Clean Architecture layer boundaries still apply.
- Keep controllers thin. Build the command or query, call `ISender.SendAsync`, and return `this.ToActionResult(result)`.
- Keep business invariants in Domain. Params records implement `IValidatable`; factories and mutators call `Validate()` before changing state and return `Result`/`Result<T>`, not exceptions, for expected failures. Domain has no third-party packages.
- Handlers return `Result` or `Result<T>` with an `Error` for expected failures. Do not throw for business outcomes.
- Use `IUnitOfWork` repositories for tracked persistence and call `SaveEntitiesAsync` once per command.
- Domain events go through the transactional outbox. Handlers run later, out of band, at least once. Make them idempotent.
- Async methods end in `Async` (Microsoft convention), including our own (`HandleAsync`, `GetAsync`, `SaveEntitiesAsync`). Controller actions too.
- Do not add MediatR, MassTransit, or Aspire. Ask before adding a third-party package. Do not add packages with a commercial license such as FluentAssertions 8+.
- Do not invent architecture for the current task. Extend the existing slices, contracts and registration patterns first.
