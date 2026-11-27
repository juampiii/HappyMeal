# Backend summary — Manage recipes (#34)

Adds the draft, publication and hiding lifecycle to the recipe API, with domain
validation, PostgreSQL enforcement and stable error codes for localized clients.
Reviewed in [PR #26](https://github.com/juampiii/HappyMeal/pull/26).

## Behavior and API

- Create a name-only draft and edit its optional description, structured ingredients,
  ordered steps, servings, preparation minutes and free-text tags.
- Publish only when at least one ingredient and step exist. Published content
  becomes immutable; subsequent version creation is outside this story.
- Emit `RecipePublishedDomainEvent` through the transactional outbox. Replace the
  obsolete deletion event with hiding behavior; external service integration is
  deferred to future stories.
- Hide without physically deleting. Lists exclude hidden recipes, detail lookup
  preserves existing references and a hidden recipe's name can be reused.

| Endpoint | Result |
| --- | --- |
| `GET /api/recipes` | Visible drafts and published recipes, ordered by name |
| `GET /api/recipes/{id}` | Recipe details, including hidden recipes |
| `POST /api/recipes` | Create draft |
| `PUT /api/recipes/{id}` | Update draft |
| `POST /api/recipes/{id}/publish` | Publish valid draft |
| `DELETE /api/recipes/{id}` | Hide recipe |

## Domain and validation

`Recipe` owns its ingredients, positioned `RecipeStep` elements and `RecipeTag`
elements. Collections are exposed read-only and modified through the aggregate.
`UnitOfMeasure` is a separate value object; its existing string transport codes
remain `g`, `kg`, `ml`, `l`, `ud`, `cucharadita` and `cucharada`.

Ingredient quantity and unit remain independently optional. Quantities are positive
decimals; servings and preparation minutes are positive integers when present.
Recipe and ingredient duplicates ignore exterior whitespace, case and accents.
Tags ignore blank entries and deduplicate ignoring case, with accents distinct.

Limits are 200 characters for recipe/ingredient names, 2000 for descriptions/steps,
50 per tag, 100 ingredients, 100 steps, 20 distinct tags and 100 input tag entries.
Invalid requests are rejected without applying the requested change.

## Persistence, errors and maintainability

- Relational owned tables replace array conversions and collection comparers.
- PostgreSQL ICU collation replaces `NameKey`. A partial unique index enforces
  visible recipe-name uniqueness, including concurrent requests.
- One `ManageRecipes` migration follows `InitialCreate`; it replaces the four
  intermediate feature migrations. Migration application and model consistency
  have focused integration coverage.
- Recipe listing uses no tracking and split queries; detail lookup keeps its
  existing query behavior. The choice follows the recorded performance evaluation.
- API and application mappers construct commands/domain parameters explicitly.
  Style/modeling guidance captures the agreed readability conventions.
- ProblemDetails exposes `code`; validation also exposes indexed
  `validationErrors` with codes/parameters while preserving legacy `errors`.
  Constraint-specific conflicts avoid reporting every uniqueness failure as a
  name collision. See the [error contract](api-error-codes.md).

## Verification and remaining scope

The final recorded local backend run passed 82 unit tests, 62 PostgreSQL
integration tests and the enforced-style solution build. Coverage includes
lifecycle rules, hidden-name reuse, limits, error contracts, collation, persistence,
owned-content updates, outbox behavior and concurrent operations. These results
are historical verification of the implementation, not a new run during summary
preparation. The merged frontend PR's CI also passed the shared test job.

Deployment of this revision to testing remains pending the final merge into
`develop`. The deployment pipeline publishes API and migration images; it does
not package the Angular application. Household authentication, future recipe
versions, planner implementation and external event consumers are outside scope.

- [Global feature summary](../../FEATURE-34-SUMMARY.md)
- [Story record and original decisions](../../docs/issues/34/README.md)
- [Query performance evidence](../../docs/issues/34/recipe-query-performance.md)
- [Deployment verification guide](../deploy/README.md)
