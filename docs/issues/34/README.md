# 34 — Manage recipes with ingredients and cooking details

**Story:** [Azure DevOps #34](https://dev.azure.com/juampidev/HappyMeal/_workitems/edit/34)

**Plan status:** Agreed; original plans preserved in [implementation history](implementation-history.md)

**Delivery status:** Implemented and merged into `develop` through [PR #28](https://github.com/juampiii/HappyMeal/pull/28) on 2026-11-27; testing deployment not yet verified

**Last review:** 2026-11-28

**Origin:** Closure summary reconstructed from the agreed amendments, code, tests and merged component PRs.

## Objective and agreed scope

Provide a recipe book where users can save a name-only draft, complete its
ingredients and ordered preparation steps, publish it, and hide recipes without
breaking existing detail references. Publication makes content immutable and emits
a recipe-published domain event through the transactional outbox.

Deliver the .NET API, PostgreSQL persistence and Angular interface in this story.
The interface supports Spanish and English, defaults to Spanish and preserves
user-authored content and unsaved input when the language changes. Household
authentication (#35), installable PWA support (#36), menus and creating subsequent
recipe versions remain outside scope.

## Confirmed behavior

- A draft requires a nonblank name. Publishing additionally requires at least one
  ingredient and one nonblank step. Only drafts can be edited.
- Ingredients require a name; quantity and unit are independently optional.
  Quantities must be positive when present. Servings and preparation minutes must
  be positive integers when present.
- Supported unit codes are `g`, `kg`, `ml`, `l`, `ud`, `cucharadita` and `cucharada`.
  Display labels are localized; transport codes remain stable.
- Recipe names and ingredient names reject duplicates ignoring exterior spaces,
  case and accents. Hiding a recipe releases its name for reuse.
- Tags are optional free text. Blank entries are ignored; duplicates are removed
  ignoring case, with accents remaining distinct.
- The recipe book lists visible drafts and published recipes. Hidden recipes are
  absent from the list but readable by ID. There is no physical deletion, hidden
  list or unhide action. The future planner must exclude drafts and hidden recipes.
- Limits: recipe/ingredient names 200 characters, descriptions/steps 2000, tags 50;
  at most 100 ingredients, 100 steps, 20 distinct tags and 100 incoming tag entries.
- Validation and persistence failures return stable error codes. The UI translates
  codes and parameters, displays relevant field feedback and retains entered data.

## Delivery and changes from the original plan

Backend [PR #26](https://github.com/juampiii/HappyMeal/pull/26) and frontend
[PR #27](https://github.com/juampiii/HappyMeal/pull/27) are merged into
`feature/34-manage-recipes`. They were reviewed separately so that domain/API
changes and UI changes could be assessed independently. The final feature PR
[#28](https://github.com/juampiii/HappyMeal/pull/28) targeting `develop` was
merged on 2026-11-27.

The original agreed plans and intermediate verification are preserved verbatim
in [implementation history](implementation-history.md). Later agreements supersede
earlier choices: hiding releases names, ICU comparison replaces `NameKey`, owned
steps/tags replace serialized arrays, one `ManageRecipes` migration follows
`InitialCreate`, and ES/EN localization is included. Earlier notes about uncommitted
changes describe intermediate states, not the current delivery.

## Verification

- Backend final local verification: enforced-style build, 82 unit tests and 62
  PostgreSQL integration tests passed, as recorded in the implementation history.
- Frontend final local verification: 45 unit tests and 28 desktop/mobile Playwright
  cases passed. These include six opt-in cases against a live backend and disposable
  PostgreSQL, covering both languages and the supported unit contract.
- Frontend production build, Prettier and diff checks passed. The mobile validation
  layout was visually reviewed.
- [PR #27 CI](https://github.com/juampiii/HappyMeal/actions/runs/46001260240)
  passed the test job. Publication and deployment were skipped for the PR, as
  configured. CI runs controlled-response browser cases; live-backend browser
  cases are opt-in locally.
- Listing uses split queries; detail lookup retains its previous query behavior.
  See the [query performance evaluation](recipe-query-performance.md).

## Remaining closure steps

1. Done: the final feature PR [#28](https://github.com/juampiii/HappyMeal/pull/28)
   was merged into `develop` on 2026-11-27.
2. Check the resulting image publication, migration and API deployment to testing.
   These jobs are triggered after merge, subject to the deployment configuration;
   this feature revision has not been deployed or verified there yet.
3. Record that evidence and close Azure DevOps #34. Until then the story remains
   active and is not reported as fully closed.

The current deployment pipeline publishes API and migration images only. The
Angular production build is verified, but frontend packaging/hosting is a separate
delivery; it must not be inferred from a successful API deployment. The broader
mobile household release also depends on #35 and #36.

## Technical references

- [Feature summary and PR description](../../../FEATURE-34-SUMMARY.md)
- [Backend summary](../../../Happymeal-BE/docs/34-manage-recipes-summary.md)
- [Frontend summary](../../../Happymeal-FE/docs/34-manage-recipes-summary.md)
- [Frontend development, localization and browser tests](../../../Happymeal-FE/README.md)
- [API error-code contract](../../../Happymeal-BE/docs/api-error-codes.md)
- [Deployment and operational verification](../../../Happymeal-BE/deploy/README.md)
- [Product roadmap](../roadmap/README.md)
- API: `GET/POST /api/recipes`, `GET/PUT/DELETE /api/recipes/{id}` and
  `POST /api/recipes/{id}/publish`. `DELETE` hides the recipe.
- Frontend: `/` (recipe book), `/recipes/new` and `/recipes/:id`.
- Persistence: `InitialCreate` then `ManageRecipes`; PostgreSQL ICU collation,
  visible-name partial unique index and recipe-owned ingredient/step/tag tables.
- Event delivery: transactional outbox and local handler; integration with future
  menu/shopping services is outside this story.
