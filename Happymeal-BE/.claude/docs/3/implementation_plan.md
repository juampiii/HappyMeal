# Implementation plan: NoTracking by default (story 3)

## Goal
The DbContext is `QueryTrackingBehavior.NoTracking` by default. Tracked reads are explicit:
- the repository method ends with `Tracked` (`GetByIdTrackedAsync`)
- the variable that holds the result ends with `Tracked` (`recipeTracked`)

## Steps
- [x] 1. `IRecipeRepository`: rename `GetAsync` to `GetByIdTrackedAsync`, add untracked `GetByIdAsync`.
- [x] 2. `ServiceExtensions`: add `.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)`.
- [x] 3. `RecipeRepository`: tracked method uses `.AsTracking()`, remove redundant `.AsNoTracking()` in `GetAllAsync`.
- [x] 4. Handlers: Update and Delete use `GetByIdTrackedAsync` and `recipeTracked`; GetById query uses `GetByIdAsync`.
- [x] 5. `DesignTimeHappymealDbContext`: add `.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` to match the runtime context.

## Notes
- `OutboxDispatcher` already uses `.AsTracking()` on its claim query, no change needed.
- `UnitOfWork.StageDomainEvents` reads `ChangeTracker`, so aggregates that raise events must be loaded tracked.
- `DesignTimeHappymealDbContext` uses NoTracking like the runtime one. It does not use retry on failure (the connection is never opened) or the `AuditableInterceptor` (needs DI).
