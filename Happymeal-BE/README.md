# Happymeal-BE

Backend API for Happymeal. .NET 10, ASP.NET Core controllers, EF Core 10 on PostgreSQL.

## Configure

Run commands from `Happymeal-BE/`. Copy `.env.example` to `.env` (`Copy-Item .env.example .env` in PowerShell, `cp .env.example .env` in Bash), then set `POSTGRES_PASSWORD` and `HAPPYMEAL_TAG`. `.env` is ignored by Git. Use a separate password on the server.

## Run with Docker Compose

Requires Docker with Compose; the SDK runs inside the build container.

```bash
docker compose up -d --build
```

Compose builds `happymeal-api` and `happymeal-migrate`. PostgreSQL must become healthy, then the migration container applies the EF Core migrations and exits successfully before the API starts. This creates the schema on a new database without running the API in Development.

- Health: `http://localhost:8080/health` (200, `Healthy`)
- Recipes: `http://localhost:8080/api/recipes` (200, `[]` on a new database)
- `/` returns 404; Scalar and OpenAPI are only available in Development.

## Run the API with the local SDK

Requires .NET SDK 10.0.400 or later and Docker. Start only PostgreSQL and pass the password from `.env` in a local connection string; `dotnet run` does not load `.env`. In PowerShell:

```powershell
docker compose up -d postgres
$dbPassword = Read-Host 'POSTGRES_PASSWORD from .env'
$env:ConnectionStrings__Happymeal = "Host=localhost;Port=5432;Database=happymeal;Username=happymeal;Password=$dbPassword"
dotnet run --project src/Happymeal.API
```

In Development the API applies migrations at startup.

- Scalar UI: `https://localhost:7180/scalar/v1`
- OpenAPI: `https://localhost:7180/openapi/v1.json`
- Health: `https://localhost:7180/health`

## Deploy to the homelab

Build both images with `./build.ps1` from PowerShell. It appends the Git short SHA to the version in `.env` and sets `$env:HAPPYMEAL_TAG` for that shell. Transfer both `happymeal-api:<tag>` and `happymeal-migrate:<tag>` to the server. Put `docker-compose.prod.yml` and a server-specific `.env` in the deployment folder, with `HAPPYMEAL_TAG` set to that exact tag, then run:

```bash
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps -a
docker compose -f docker-compose.prod.yml logs --tail 50 migrate api
curl --fail-with-body http://192.168.1.142:8091/health
curl --fail-with-body http://192.168.1.142:8091/api/recipes
```

The production file uses prebuilt images, publishes the API on port 8091 and keeps PostgreSQL on the internal network. A successful `migrate` container exits with code 0. Keep the deployment folder (Compose project name) unchanged to reuse the database volume.

See the [build and deployment guide](docs/learning/02-docker-compose-build-and-deploy.md#deploy-by-hand-to-the-debian-server) for image transfer, backups, migration checks and rollback.

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

See the [architecture skill](skills/architecture/SKILL.md) for the full architecture guide.

## Agent skills

Start Claude Code or Codex in `Happymeal-BE/` to discover the [backend skills](skills/INDEX.md). Each tool loads a short entry that links to the shared skill.

See [SkillKit maintenance and verification](skills/MAINTENANCE.md) to add or update a skill.
