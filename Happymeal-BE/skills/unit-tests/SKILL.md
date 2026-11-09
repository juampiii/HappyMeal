---
name: unit-tests
description: Guide for unit testing in Happymeal-BE with xUnit v3, NSubstitute, AwesomeAssertions. Use when writing, reviewing or updating unit tests for domain entities and events, params validation (IValidatable), command and query handlers, domain event handlers or pure infrastructure helpers, without a real database or HTTP.
---

# Unit Testing in Happymeal-BE

Unit tests isolate Domain, Application and pure Infrastructure logic. They never start PostgreSQL, Testcontainers, `WebApplicationFactory` or the outbox dispatcher.

## Available files

| File | When to read it |
|------|-----------------|
| [`project-structure.md`](references/project-structure.md) | Folder layout, packages, global usings, `InternalsVisibleTo`. |
| [`test-style.md`](references/test-style.md) | Naming, AAA, `_sut`, theories, expression-bodied tests. |
| [`mocking-and-assertions.md`](references/mocking-and-assertions.md) | NSubstitute setup and verification, AwesomeAssertions, asserting `Result`/`Error`. |
| [`domain-and-validation.md`](references/domain-and-validation.md) | Entity factories and mutators, domain events, `IValidatable` params tests. |
| [`application-services.md`](references/application-services.md) | Command and query handler tests (result + state + events + interactions) and domain event handler tests. |
| [`common-pitfalls.md`](references/common-pitfalls.md) | Frequent mistakes and the unit vs integration decision. |

## Quick Routing

- New command or query handler test: [`application-services.md`](references/application-services.md), [`mocking-and-assertions.md`](references/mocking-and-assertions.md), [`test-style.md`](references/test-style.md).
- New entity or domain event test: [`domain-and-validation.md`](references/domain-and-validation.md).
- New params validation test: [`domain-and-validation.md`](references/domain-and-validation.md).
- New domain event handler test: [`application-services.md`](references/application-services.md).
- Confusing or flaky test: [`common-pitfalls.md`](references/common-pitfalls.md).

## Scope

Do not add packages or change `Happymeal.UnitTests.csproj` unless the task asks for it. Never add FluentAssertions (8+ has a commercial license); AwesomeAssertions is the free fork with the same API. Do not turn a unit test into an integration test.
