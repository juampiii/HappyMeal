# Happymeal-BE

Backend API for Happymeal. .NET 10, ASP.NET Core controllers, EF Core 10 on PostgreSQL.

## Run locally

Requirements: .NET SDK 10.0.400 or later, Docker.

```bash
docker compose up -d
dotnet run --project src/Happymeal.API
```

In Development the API applies migrations at startup.

- Scalar UI: `https://localhost:7180/scalar/v1`
- OpenAPI: `https://localhost:7180/openapi/v1.json`
- Health: `https://localhost:7180/health`

## Endpoints

| Method | Route | Result |
|--------|-------|--------|
| GET | `/api/recipes` | 200 list |
| GET | `/api/recipes/{id}` | 200, 404 |
| POST | `/api/recipes` | 201, 400 |
| PUT | `/api/recipes/{id}` | 200, 400, 404 |
| DELETE | `/api/recipes/{id}` | 204, 404 |

Create, update and delete raise `RecipeCreatedDomainEvent`, `RecipeUpdatedDomainEvent` and `RecipeDeletedDomainEvent`. They are stored in the `Happymeal.OutboxMessages` table in the same transaction and delivered by a background dispatcher.

## Tests

```bash
dotnet test --project tests/Happymeal.UnitTests
dotnet test --project tests/Happymeal.IntegrationTests
```

Integration tests start a PostgreSQL container, so Docker must be running.

## Structure

See `.claude/skills/architecture` for the full architecture guide.
