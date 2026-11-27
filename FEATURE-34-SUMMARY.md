# Manage recipes with ingredients and cooking details (#34)

Delivers the recipe lifecycle across the .NET API, PostgreSQL database and Angular
recipe book. Users can save a draft with only a name, complete its ingredients and
ordered preparation steps, publish it, and hide it without breaking existing links.
The interface is available in Spanish and English, with Spanish as the default.

## Backend

- Enforce draft editing and immutable published content. Publishing requires at
  least one ingredient and step and records a publication event in the
  transactional outbox. Hiding replaces physical deletion.
- Validate optional quantities, units, servings and preparation time; reject
  duplicate recipe/ingredient names and enforce text and collection limits.
- Model units as a value object and ingredients, steps and tags within the recipe
  aggregate. Persist owned collections in relational tables.
- Compare recipe names using PostgreSQL ICU collation and a partial unique index;
  hidden recipes release their names. Consolidate feature schema changes into
  `ManageRecipes`, following the existing `InitialCreate` migration.
- Expose stable ProblemDetails and per-field validation codes for client
  localization. Distinguish persistence conflicts and keep controller/application
  mapping in explicit mappers. Use split queries only for the recipe list.

See the [backend summary](Happymeal-BE/docs/34-manage-recipes-summary.md).

## Frontend

- Add recipe listing, detail, draft creation/editing, publication and confirmed
  hiding, with responsive layouts and reactive forms.
- Localize visible text, accessible labels, units, feedback and validation in
  Spanish/English; remember the preference and retain unsaved input when switching.
- Translate structured API codes and parameters into field messages. Preserve
  input after failures, clear only edited fields' server errors and avoid repeating
  all field messages in the alert banner.
- Preserve creation feedback across navigation, avoid redundant detail reloads
  after publishing, ignore stale detail responses and retain hide confirmation
  after errors. Offer retry for transient load failures, not permanent 404s.
- Add unit and desktop/mobile Playwright regression tests plus formatting and
  browser checks in CI. Live-backend browser cases remain opt-in.

See the [frontend summary](Happymeal-FE/docs/34-manage-recipes-summary.md).

## Validation

- Backend: 82 unit tests, 62 PostgreSQL integration tests and enforced-style build
  passed in the final recorded local backend verification.
- Frontend: 45 unit tests, 28 Playwright cases, production build and Prettier passed
  in the final recorded local frontend verification. Browser coverage includes
  the live lifecycle and unit contract against disposable PostgreSQL in both
  languages and desktop/mobile sizes. Mobile validation layout was visually checked.
- [Component PR CI](https://github.com/juampiii/HappyMeal/actions/runs/46001260240)
  passed. The final integration PR has its own checks; these prior results do not
  stand in for that run or for post-merge deployment verification.

## Scope and delivery

Backend [PR #26](https://github.com/juampiii/HappyMeal/pull/26) and frontend
[PR #27](https://github.com/juampiii/HappyMeal/pull/27) were independently reviewed
and merged into `feature/34-manage-recipes`. This final PR integrates that feature
into `develop` and remains open for review.

Authentication/household access (#35), installable PWA support (#36), recipe version
creation, hidden-recipe recovery, menus, shopping lists and automatic unit
conversion are outside scope. The existing deployment publishes API and migration
images; Angular production packaging/hosting is a separate delivery. Publication,
migration and API health in testing must be checked after the final merge before
the story is closed.

The [story delivery record](docs/issues/34/README.md) links the preserved original
plans, decisions, performance evaluation and operational instructions.
