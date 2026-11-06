# Story 5: Add unit and integration tests

Branch: `feature/5-add-unit-integration-tests`

## Goal

Add unit and integration tests for the Recipes feature, following the `unit-tests` and `integration-tests` skills.

Success criteria:
- `dotnet build Happymeal.slnx` passes.
- `dotnet test --project tests/Happymeal.UnitTests` is green.
- Integration tests run only after the user's OK (Docker, slow).
- No new packages. No changes to the test `.csproj` files.

## Current state

- `Happymeal.UnitTests` and `Happymeal.IntegrationTests` exist, with no test files.
- Integration infrastructure exists: `HappymealApiFactory`, `HappymealCollection`, `TestBase`.

## Decisions

- `AuditableInterceptor` is covered by integration tests. A unit test would need an InMemory provider (new package). The skill `unit-tests/references/common-pitfalls.md` was updated.
- Dead-letter test (failing handler, low `MaxAttempts`) is out of scope for now.

## Out of scope

- Isolated tests for `HttpCurrentUser`, `ControllerExtensions`, `ServiceExtensions`. Integration tests already exercise them.
- Production code changes. If a test finds a bug, note it here as a follow-up.

## Steps

Status legend: [ ] pending, [x] done.

### Phase 1: Unit tests, Domain

- [x] 1. `Domain/Params/RecipeParamsTests.cs`: empty or whitespace name, name over 200 chars, description over 2000 chars, valid case, several failures at once.
- [x] 2. `Domain/RecipeTests.cs`:
  - `Create`: empty id, invalid params, normalization (trim, blank description becomes null), `RecipeCreatedDomainEvent`.
  - `Update`: failure leaves state unchanged and raises no event, success raises `RecipeUpdatedDomainEvent`.
  - `MarkAsDeleted`: raises `RecipeDeletedDomainEvent`.

### Phase 2: Unit tests, Application

NSubstitute on `IUnitOfWork` and `IRecipeRepository`.

- [x] 3. `CreateRecipeCommandHandlerTests`: success with client id, success with generated id, validation failure, `Conflict` when it exists, `Failure` when `SaveEntitiesAsync` returns false.
- [x] 4. `UpdateRecipeCommandHandlerTests`: success, `NotFound`, validation failure.
- [x] 5. `DeleteRecipeCommandHandlerTests`: success, `NotFound`.
- [x] 6. `GetRecipeByIdQueryHandlerTests` and `GetRecipesQueryHandlerTests`.
- [x] 7. `LogRecipeLifecycleDomainEventHandlerTests`: returns success for the 3 events.
- [x] 8. (skipped) `Sender` tests only if integration tests do not already cover it.

### Phase 3: Unit tests, Infrastructure (pure)

- [x] 9. `Infrastructure/Outbox/OutboxSerializerTests`: serialize/deserialize round trip, invalid payload returns `Failure`.

### Phase 4: Integration tests

- [ ] 10. `Recipes/RecipesEndpointsTests.cs`:
  - `POST`: 201 with `Location`, 400 `ValidationProblemDetails`, 409 with duplicated id, persisted audit fields (`CreatedOn`, `CreatedBy`).
  - `GET` by id: 200, 404 `ProblemDetails`.
  - `GET` list.
  - `PUT`: 200 (with `ModifiedOn`, `ModifiedBy`), 404, 400.
  - `DELETE`: 204, 404.
- [ ] 11. `Recipes/RecipeOutboxTests.cs`: after create, the `OutboxMessage` exists in the same transaction. Then poll until `ProcessedOn` is not null and `DeadLetteredOn` is null. Keep `WaitForAsync` in the class. Move it to `TestBase` only if a second class needs it.

### Phase 5: Verification

- [x] 12. Build the solution and run the unit tests.
- [ ] 13. Ask the user before running the integration tests.

## Notes and deviations

- Step 8 skipped: `Sender` is internal and uses reflection over the service provider. Integration tests cover it through the real pipeline.
- Unit tests: 40 passing. Build has 0 warnings and 0 errors.
