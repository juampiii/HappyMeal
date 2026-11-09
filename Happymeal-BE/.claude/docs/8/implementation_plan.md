# Story 8: Group requests and handlers by use case

Branch: `refactor/8-create-recipe-vertical-slice`

## Goal

Apply the same vertical slice organization to every existing Application use case, one at a time: a static `<UseCase>Feature` class containing its request and handler in a single file.

Success criteria:
- All five Recipes use cases follow the convention below.
- Each use case is delivered in its own commit, including its controller references and test adaptations.
- Each commit builds and its affected unit tests pass.
- Existing HTTP contracts, business behavior, persistence and domain events remain unchanged.
- No new packages or project configuration changes.

## Current state

- Recipes is the only feature area currently implemented.
- Five request/handler pairs exist: `CreateRecipe`, `UpdateRecipe`, `DeleteRecipe`, `GetRecipeById` and `GetRecipes`.
- `CreateRecipeFeature.cs`, controller references and its unit tests have pending changes from the initial refactor.
- `CreateRecipe.cs` also exists with the previous container name. Consolidate it into `CreateRecipeFeature.cs` to avoid registering two creation handlers for different request types.
- The other four use cases still use separate request and handler files.
- `AddRequestHandlers` scans `Assembly.GetTypes()` and registers implemented handler interfaces, including public nested handlers. No DI changes are expected.
- `CLAUDE.md` and the local architecture guidance still describe the previous naming convention.

## Decisions

- Keep existing directories and namespaces: `Features/Recipes/{Commands,Queries}/<UseCase>/`.
- Use `<UseCase>Feature.cs` with a `public static class <UseCase>Feature`.
- Nest `public sealed record Command(...)` for commands or `public sealed record Query(...)` for queries.
- Nest `public sealed class Handler(...)`, preserving the current handler interface, dependencies, response type and `HandleAsync` signature.
- Use `CreateRecipeFeature.Command`, `UpdateRecipeFeature.Command`, `DeleteRecipeFeature.Command`, `GetRecipeByIdFeature.Query` and `GetRecipesFeature.Query` at call sites.
- The `Feature` suffix avoids collisions with namespaces and removes the need for aliases such as `RecipeCreation`.
- Keep existing test class/file names (`...CommandHandlerTests`, `...QueryHandlerTests`); update the types used inside them. Those names still describe what is tested.
- Keep `RecipeDto` shared at the Recipes root. Keep HTTP DTOs in API and lifecycle event handling in `DomainEventHandlers`.
- This adopts vertical slice organization within Application; it does not move endpoints, Domain or Infrastructure into the slice.

## Out of scope

- New functionality, bug fixes, pagination or changes to error handling.
- Moving or duplicating shared DTOs, repository contracts or domain events.
- Wrapping `LogRecipeLifecycleDomainEventHandler` in a Feature container: it already groups lifecycle event handling and has no command/query pair to consolidate.
- Changing `ISender`, handler discovery, database schema, migrations or the outbox.

## Steps

Status legend: [ ] pending, [x] done. Pending work already present in the working tree is not considered a completed commit.

### Phase 1: Create recipe

Commit: `refactor: group create recipe`

- [x] 1. Review and complete the pending creation refactor. Keep only `Commands/CreateRecipe/CreateRecipeFeature.cs` with `Command` and `Handler`; remove the obsolete `CreateRecipe.cs`, `CreateRecipeCommand.cs` and `CreateRecipeCommandHandler.cs`.
- [x] 2. Confirm `RecipesController.CreateAsync` sends `CreateRecipeFeature.Command` and `CreateRecipeCommandHandlerTests` uses `CreateRecipeFeature.Handler` and `.Command` without an alias.
- [x] 3. Update the naming guidance in `CLAUDE.md` and `.claude/skills/architecture/SKILL.md` and its relevant references to describe the new convention. Describe the other use cases as pending migration until their respective commits.
- [x] 4. Build the solution and run `CreateRecipeCommandHandlerTests` (client id, generated id, validation, conflict and persistence failure). Commit only this slice, its guidance updates and plan progress.

### Phase 2: Update recipe

Commit: `refactor: group update recipe`

- [x] 5. Combine `UpdateRecipeCommand.cs` and `UpdateRecipeCommandHandler.cs` into `Commands/UpdateRecipe/UpdateRecipeFeature.cs`, with `Command` and `Handler`. Remove the old files.
- [x] 6. Update `RecipesController.UpdateAsync` and `UpdateRecipeCommandHandlerTests` to use `UpdateRecipeFeature.Command` and `.Handler`.
- [x] 7. Build the solution and run `UpdateRecipeCommandHandlerTests` (success, not found and validation). Preserve current save behavior. Commit this slice and plan progress.

### Phase 3: Delete recipe

Commit: `refactor: group delete recipe`

- [x] 8. Combine `DeleteRecipeCommand.cs` and `DeleteRecipeCommandHandler.cs` into `Commands/DeleteRecipe/DeleteRecipeFeature.cs`, with `Command` and `Handler`. Remove the old files.
- [x] 9. Preserve the response-free `ICommand` and `ICommandHandler<Command>` contracts. Update `RecipesController.DeleteAsync` and `DeleteRecipeCommandHandlerTests` to use the new types.
- [x] 10. Build the solution and run `DeleteRecipeCommandHandlerTests`. Preserve deletion, event emission and persistence failure behavior. Commit this slice and plan progress.

### Phase 4: Get recipe by id

Commit: `refactor: group get recipe`

- [ ] 11. Combine `GetRecipeByIdQuery.cs` and `GetRecipeByIdQueryHandler.cs` into `Queries/GetRecipeById/GetRecipeByIdFeature.cs`, with `Query` and `Handler`. Remove the old files.
- [ ] 12. Update `RecipesController.GetByIdAsync` and `GetRecipeByIdQueryHandlerTests` to use `GetRecipeByIdFeature.Query` and `.Handler`.
- [ ] 13. Build the solution and run `GetRecipeByIdQueryHandlerTests` (success and not found). Commit this slice and plan progress.

### Phase 5: List recipes

Commit: `refactor: group list recipes`

- [ ] 14. Combine `GetRecipesQuery.cs` and `GetRecipesQueryHandler.cs` into `Queries/GetRecipes/GetRecipesFeature.cs`, with `Query` and `Handler`. Remove the old files.
- [ ] 15. Preserve `IQuery<IReadOnlyList<RecipeDto>>`. Update `RecipesController.GetAllAsync` and `GetRecipesQueryHandlerTests` to use `GetRecipesFeature.Query` and `.Handler`.
- [ ] 16. Build the solution and run `GetRecipesQueryHandlerTests` (populated and empty list). Complete the final verification below before committing this slice and plan progress.

### Phase 6: Final verification

- [ ] 17. Run `dotnet build Happymeal.slnx` and `dotnet test --project tests/Happymeal.UnitTests`.
- [ ] 18. Search source and tests for old request/handler type references and aliases. Test class names are intentional exceptions. Confirm one request and one handler per use case, with no obsolete files.
- [ ] 19. Check that architectural guidance consistently documents the final naming convention, with no remaining pending migrations.
- [ ] 20. Run the existing integration tests with Docker once authorized, following the repository instructions. These exercise nested handler resolution through the real sender, all five endpoints and outbox delivery. Record results or pending authorization here.
- [ ] 21. Run `git diff --check`, review the staged files for the final slice and create its commit. Do not include unrelated working-tree changes.

## Commit workflow

- Complete and verify one use case before starting the next.
- Stage explicit files for that use case; avoid staging the whole working tree.
- Include its request, handler, controller call sites and test adaptations in the same commit so every commit compiles independently.
- Use the short English commit messages specified above, one implementation commit per use case.
- Update this plan's checkboxes and validation notes in the corresponding commit.

## Notes and deviations

- This document is the implementation plan only. No additional use cases have been migrated or committed while writing it.
- The creation refactor is pending in the working tree and must be reviewed as Phase 1 rather than assumed complete.
- Record validation results and deviations here as implementation progresses.

### Execution results

- Migration status: CreateRecipe, UpdateRecipe and DeleteRecipe complete; GetRecipeById and GetRecipes pending.
- CreateRecipe: solution build passed with 0 warnings/errors; all 5 handler tests passed. Removed the obsolete duplicate container.
- UpdateRecipe: solution build passed with 0 warnings/errors; all 3 handler tests passed.
- DeleteRecipe: solution build passed with 0 warnings/errors; all 3 handler tests passed.
