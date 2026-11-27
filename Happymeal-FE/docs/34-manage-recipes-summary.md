# Frontend summary — Manage recipes (#34)

Introduces the Angular recipe book and localizes it in Spanish and English.
Users can list recipes, read details, create/edit drafts, publish completed drafts
and hide recipes with confirmation. Reviewed in
[PR #27](https://github.com/juampiii/HappyMeal/pull/27).

## Interface and localization

- Routes: `/` for the recipe book, `/recipes/new` for creation and `/recipes/:id`
  for details and draft editing. Hidden recipes disappear from the list but remain
  readable through existing detail URLs. Published recipes have no edit action.
- Reactive forms support descriptions, ingredients with independent optional
  quantities/units, ordered steps, servings, preparation minutes and comma-separated
  tags. Client validators cover required trimmed text, positive numbers, integers
  and API text/count limits; the server remains authoritative.
- Typed ES/EN catalogs and a signal-based service translate application text,
  accessible labels, units, validation and action feedback. Spanish is the default;
  the language preference is remembered when browser storage is available.
- Switching language updates the document language/title and feedback without
  losing form input. User-authored recipe content and API unit codes stay unchanged.
  Unit labels have singular forms and display quantities use locale formatting.
- Native labels, field descriptions and `aria-invalid` expose validation state.
  A concise alert avoids repeating every inline error to screen readers.

## Errors and lifecycle feedback

Structured API `code` and `validationErrors` entries are translated using their
codes, field paths and parameters. Unknown/legacy errors use localized fallbacks;
server diagnostic prose is not shown as a translated message. Conflict codes have
distinct messages. Correcting one control clears only its server errors, while
structural collection edits invalidate the affected indexed errors.

Creation carries the returned recipe through transient router info to retain the
saved notice without a duplicate GET. Publishing uses the acknowledged response
to update the displayed state without a reload/loading flash. Stale detail requests
cannot overwrite the current route's recipe, errors or loading state.

Failures preserve entered data. Hide failures retain the confirmation for retry
or cancel. Network/server failures and HTTP 408/429 allow detail-load retry; a 404
offers the link back to the recipe book. Ingredient amounts omit empty parts.

`FieldError` uses signal inputs, computed messages and Angular RxJS interop for
control events. Localization adds no runtime translation dependency; Playwright
is a development-only dependency agreed for browser regression coverage.

## Verification and CI

- Final recorded local verification: 45 unit tests, 28 desktop/mobile Playwright
  cases, production build, Prettier and diff checks passed.
- The browser suite covers localization, preference storage, input retention,
  structured errors, creation feedback, publication, hiding and recovery paths.
  Six opt-in live-backend cases check all UI units and the ES/EN lifecycle against
  disposable PostgreSQL. Mobile validation layout was visually reviewed.
- CI enforces formatting, unit tests, production build and controlled-response
  browser cases. Live-backend cases require `HAPPYMEAL_LIVE_API=1`; they are not
  represented as remotely executed by the default workflow.
- Tests start an isolated Angular server on port 4300; `HAPPYMEAL_E2E_PORT` can
  override it. Existing previews are deliberately not reused. Port 4317 was also
  verified. Browser traces and screenshots remain ignored test artifacts.
- [PR #27 CI passed](https://github.com/juampiii/HappyMeal/actions/runs/46001260240).
  The final feature PR requires its own checks before merge.

## Remaining scope and references

Google/household access belongs to #35 and installable PWA support to #36.
Production frontend packaging/hosting is a separate delivery: a successful API
deployment does not make this interface available on the homelab. Creating new
recipe versions and recovering hidden recipes are outside this story.

- [Global feature summary](../../FEATURE-34-SUMMARY.md)
- [Development, localization and test commands](../README.md)
- [Story record and original decisions](../../docs/issues/34/README.md)
- [API error contract](../../Happymeal-BE/docs/api-error-codes.md)
