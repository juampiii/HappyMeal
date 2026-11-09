---
name: integration-tests-common-pitfalls
description: Consult to avoid common integration test mistakes around Docker, Respawn, seeding, tracking and async delivery.
---

## Common Pitfalls

1. **Docker not running.** The factory fails in `InitializeAsync`. Start Docker Desktop first.
2. **Assuming data from another test.** Respawn empties the `Happymeal` schema before each test. Seed what you need.
3. **Creating a second collection or factory.** All tests must use `[Collection(HappymealCollection.Name)]` through `TestBase`. A second factory starts a second container and parallel tests break the shared Respawn reset.
4. **Seeding invalid entities.** Build them with `Recipe.Create(...)`, not object initializers or reflection.
5. **Expecting outbox rows from seeded data.** `SeedAsync` uses `SaveChangesAsync` directly. Only `UnitOfWork.SaveEntitiesAsync` stages events.
6. **Fixed delays for async effects.** Poll with a timeout (`async-side-effects.md`).
7. **Reading through a tracked context.** Use `QueryAsync(...)` (new scope) or `AsNoTracking()` to read what the API wrote.
8. **Asserting full error messages.** Assert status codes and error keys.
9. **Forgetting migrations.** A model change without a migration fails here first. Add the migration (see the architecture skill, `runtime-and-integrations.md`).
10. **Adding packages or changing the csproj without need.** Read `Happymeal.IntegrationTests.csproj` first.
11. **Running the suite without asking.** It starts real containers and takes time. Ask the user first.
