---
name: integration-tests
description: Guide for integration testing in Happymeal-BE with WebApplicationFactory, a PostgreSQL Testcontainer, Respawn, xUnit v3 and AwesomeAssertions. Use when writing, reviewing or debugging tests that call the real HTTP pipeline and database, check EF mappings or migrations, or check that the outbox delivers domain events, or when deciding whether a scenario needs real infrastructure.
---

# Integration Testing in Happymeal-BE

Tests boot the real API in memory with `WebApplicationFactory<Program>` against a **real PostgreSQL** started by Testcontainers. Respawn resets the database before each test. The outbox dispatcher runs as in production.

Use xUnit v3 with AwesomeAssertions. Send requests with the `HttpClient` created by the test factory.

## When to write an integration test

They need Docker and are slow. Decide on purpose:

- **Write one when the behavior under test is the infrastructure interaction**: routing and status codes, ProblemDetails bodies, EF Core mappings and queries, migrations, outbox staging and delivery.
- **Prefer a unit test when the logic is self-contained**: domain rules, params validation, handler logic.
- **Rule of thumb**: do not re-verify logic that a unit test already proves. Add an integration test when removing it would let a wiring or infrastructure regression through.

## Available files

| File | When to read it |
|------|-----------------|
| [`test-infrastructure.md`](references/test-infrastructure.md) | `HappymealApiFactory`, the collection fixture, `TestBase`, Respawn, the Testing environment. |
| [`writing-tests.md`](references/writing-tests.md) | Class shape, naming, AAA, HTTP calls, seeding and querying, assertions. |
| [`async-side-effects.md`](references/async-side-effects.md) | Waiting for the outbox to process or dead-letter a message. |
| [`common-pitfalls.md`](references/common-pitfalls.md) | Frequent mistakes. |

## Quick Routing

- New endpoint test: [`writing-tests.md`](references/writing-tests.md).
- Domain event delivery: [`async-side-effects.md`](references/async-side-effects.md) and [`test-infrastructure.md`](references/test-infrastructure.md).
- Failing setup, Docker or Respawn problems: [`test-infrastructure.md`](references/test-infrastructure.md), then [`common-pitfalls.md`](references/common-pitfalls.md).

## Scope

Do not add packages or change `Happymeal.IntegrationTests.csproj` unless the task asks for it. Never run the integration suite without the user's go-ahead. It starts Docker containers and takes time.
