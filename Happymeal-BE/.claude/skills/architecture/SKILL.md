---
name: architecture
description: Explains the Happymeal-BE architecture - Clean Architecture layers, CQRS-style commands and queries with their handlers and ISender (nested requests and handlers grouped by use case, without MediatR), the unified Result/Error type, domain validation with IValidatable (no FluentValidation), the transactional outbox for domain events and IDomainEventHandler, the ProblemDetails error contract, and AggregateRoot and IAuditable audit fields, client-assignable ids, and runtime setup (EF Core PostgreSQL, docker-compose, OpenAPI + Scalar). Use when onboarding, designing or reviewing a feature, deciding which layer code belongs in, adding a command, query, domain event or domain event handler, adding validation, or changing persistence and startup.
---

Read the shared skill before starting the task. Resolve its reference links relative to the shared file.

| File | When to read it |
| --- | --- |
| [Shared skill](../../../skills/architecture/SKILL.md) | Whenever this skill is invoked. |
| [`project-map.md`](../../../skills/architecture/references/project-map.md) | When you need the solution structure, project responsibilities, dependencies or packages. |
