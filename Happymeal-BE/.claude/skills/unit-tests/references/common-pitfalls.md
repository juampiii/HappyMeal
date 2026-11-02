---
name: unit-tests-common-pitfalls
description: Consult when a unit test is confusing or flaky, or when deciding between unit and integration coverage.
---

## Unit or integration?

- Unit: domain invariants, params validation (`IValidatable`), command and query handlers with substitutes, domain event handler logic, pure helpers (`OutboxSerializer`, `ValidationFailureExtensions`).
- Integration: EF mappings and queries, migrations, `UnitOfWork` outbox staging, `OutboxDispatcher` delivery, HTTP status codes and ProblemDetails bodies.

## Common Pitfalls

1. **Leftover creation events.** `Recipe.Create` raises `RecipeCreatedDomainEvent`. Call `PullDomainEvents()` in Arrange when the test is about a later action.
2. **Reading `Value` of a failed result.** `Result<T>.Value` throws when the result is a failure. Check `IsSuccess` or `Error` first.
3. **Asserting full validation messages.** Assert property paths (`name`, `description`), not message text.
4. **Substituting entities.** Build them with `Create`. Substitutes of concrete classes hide real behavior.
5. **Forgetting `SaveEntitiesAsync` returns `bool`.** Unconfigured substitutes return `false`, so create/delete handlers return `ErrorType.Failure`. Configure `SaveEntitiesAsync(...).Returns(true)` on success paths.
6. **Comparing against `DateTimeOffset.UtcNow` in assertions.** Substitute `TimeProvider` and use a fixed value.
7. **Testing the outbox through a unit test.** The raw SQL claim only works on PostgreSQL. Cover it with integration tests.
8. **Adding packages.** Do not add FluentAssertions, Moq, AutoFixture or other packages without asking. Never add a package with a commercial license.
9. **Writing tests that were not requested.** Only write tests when the task asks for them.
