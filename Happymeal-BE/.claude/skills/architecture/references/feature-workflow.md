# Feature Workflow

Use this workflow to add or change a feature. Keep the change inside its vertical slice. Read [`layers-and-flows.md`](layers-and-flows.md) first for the layer boundaries. The Recipes feature is the reference slice.

## 1. Define Contracts First

- Request DTO in `Happymeal.API/Dtos` if the HTTP body changes.
- Nested `Command` (`ICommand` or `ICommand<TResponse>`) or `Query` (`IQuery<TResponse>`) record in a public static `<Name>Feature` class in `Features/<Feature>/{Commands,Queries}/<Name>/<Name>Feature.cs`.
- Response DTO at the feature root, with a static `From(entity)` mapper.
- Repository interface methods in `Common/Interfaces` for any new persistence need.

Example layout:

```text
Features/Recipes/
  Commands/CreateRecipe/CreateRecipeFeature.cs   # Command + Handler
  Queries/GetRecipeById/GetRecipeByIdFeature.cs  # Query + Handler
  RecipeDto.cs                                 # shared response DTO
  DomainEventHandlers/LogRecipeLifecycleDomainEventHandler.cs
```

The folder and namespace name is `CreateRecipe`; the container and file name is `CreateRecipeFeature`. Use the same convention for new use cases. Domain event handlers remain separate because they react to events rather than handling a command/query request.

Do not write Infrastructure before the Application contract is clear.

## 2. Place Business Rules

Domain owns invariants that are true whatever the entry point. Application owns orchestration, lookups and cross-aggregate checks. Infrastructure owns technical details. API owns HTTP. Move stable business meaning inward and volatile technical detail outward.

## 3. Update Domain When Needed

- Add or change the params record (`Params/`). It implements `IValidatable` and returns every broken rule from `Validate()` with `nameof(...)` paths and plain English messages.
- Keep state changes behind `Create` (returns `Result<T>`) and named mutators (return `Result`). They normalize the input, call `Validate()` and return the `Error` before changing state.
- Put length limits as `public const` on the entity and reuse them in `Validate()` and the EF configuration.
- Raise a domain event (`Events/<Entity><Verb>DomainEvent.cs`, a sealed record with primitive fields) only for a real domain change that something should react to.

Never put EF Core, HTTP or configuration concerns in Domain.

## 4. Build the Application Slice

1. Nest a `public sealed class Handler(...)` alongside `Command` or `Query` inside `<Name>Feature`. Implement `ICommandHandler<Command>`, `ICommandHandler<Command, TResponse>` or `IQueryHandler<Query, TResponse>` as appropriate. The assembly scanner includes nested handlers. Call sites use `<Name>Feature.Command` or `<Name>Feature.Query`, avoiding namespace aliases.
2. Use guard clauses: not found -> `return Error.NotFound(...)`, conflict -> `return Error.Conflict(...)`.
3. Call the Domain method. On failure pass the error through: `return result.Error!;`.
4. Persist through `IUnitOfWork` repositories and call `SaveEntitiesAsync` once (commands only).
5. Return `Result.Success(dto)` or `Result.Success()`.
6. To react to a domain event, add an `IDomainEventHandler<TDomainEvent>` in `Features/<Feature>/DomainEventHandlers/`. It must be idempotent.

## 5. Implement Infrastructure

- Implement the repository methods in `Repositories/` (`internal sealed`, `Async` suffix on async methods). Tracked `GetAsync` for writes, `AsNoTracking` for lists.
- Add a new repository to `IUnitOfWork`, `UnitOfWork` and `AddInfrastructure`.
- Add or update `Database/Configurations/<Entity>Configuration.cs` and the `DbSet` in `HappymealDbContext`.
- Add a migration (see [`runtime-and-integrations.md`](runtime-and-integrations.md)).

## 6. Expose Through the API

- Add the action to the feature controller in `Controllers/`. Name it `<Verb>Async`.
- Build `<Name>Feature.Command` or `<Name>Feature.Query` from route and body and call `sender.SendAsync(...)`, for example `sender.SendAsync(new GetRecipeByIdFeature.Query(id), cancellationToken)`.
- Return `this.ToActionResult(result)`. Use the `onSuccess` overload for `201 Created` and give the target action `[ActionName(nameof(...))]`.
- Add `[ProducesResponseType<T>]` for the success type and `ProblemDetails`/`ValidationProblemDetails` for the errors.

## 7. Test at the Right Boundary

- Domain rules, params validation, handlers with substituted collaborators, domain event handlers: `unit-tests` skill.
- Endpoint wiring, EF mapping and queries, migrations, outbox delivery: `integration-tests` skill. These start a PostgreSQL container, so prefer a unit test when it proves the logic.

Do not add packages or change test project configuration unless the task asks for it.

## Definition of Done

- Contracts were defined before the implementation.
- Layer boundaries hold and controllers stay thin.
- Handlers return `Result`/`Result<T>` for expected outcomes; commands call `SaveEntitiesAsync` once; queries never save.
- Domain validation errors reach HTTP as `400 ValidationProblemDetails` with paths that match the request body.
- New domain events have idempotent handlers, or none on purpose.
- EF configuration, migration and DI registration were updated together.
- The solution builds and the unit tests pass.
