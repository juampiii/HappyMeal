# Happymeal-BE

.NET 10 backend with a pragmatic Clean Architecture. Solution: `Happymeal.slnx`.

## Skills

Use the [skill index](skills/INDEX.md) to choose a skill for the task. Read the architecture skill before designing or reviewing a feature. Open the selected skill's `SKILL.md` and follow its relative links to the references that apply.

## Commands

```bash
docker compose up -d
dotnet build Happymeal.slnx
dotnet test --project tests/Happymeal.UnitTests
dotnet run --project src/Happymeal.API
dotnet tool restore
dotnet ef migrations add <Name> --project src/Happymeal.Infrastructure --startup-project src/Happymeal.Infrastructure --output-dir Database/Migrations
```

Integration tests need Docker and are slow. Run them only after asking.

## Hard rules

- No Aspire, no DYPlatform or DYCommon packages, no MediatR, no MassTransit, no packages with a commercial license (this includes FluentAssertions 8+). Shared building blocks go in `src/Happymeal.Common`.
- Ask before adding any production package. Domain has no third-party packages: params implement `IValidatable` and rules are written by hand. No FluentValidation.
- Handlers return `Result`/`Result<T>` with an `Error`. Errors are ProblemDetails. Validation errors are `400 ValidationProblemDetails`.
- Domain events go through the outbox. Handlers must be idempotent.
- Application use cases use `<UseCase>Feature.cs`: a public static class with a nested `Command` or `Query` record and a public sealed `Handler`. Keep existing Commands/Queries folders and namespaces; the Feature suffix prevents namespace collisions. Domain events retain `<Entity><Verb>DomainEvent`; every async method retains the `Async` suffix.
