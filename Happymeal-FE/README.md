# HappyMeal recipe book

Angular interface in Spanish and English, with Spanish as the default.
Authentication belongs to story #35; installable PWA support to #36.

## Local development

Use Node from `.nvmrc`. Run `npm ci`, then `npm start`. Start the backend separately with `dotnet run --project src/Happymeal.API --urls http://localhost:5094` from `Happymeal-BE`. The development proxy forwards `/api/**` to that local address. No broad CORS policy is needed.

Run `npm test -- --watch=false` and `npm run build` to verify the frontend. The
production build is ready for the separate packaging delivery. The backend PR is
merged into `feature/34-manage-recipes`; this frontend branch includes that updated
integration base. Shared API-image hosting is not included in this branch.

## Localization

The header language selector changes the interface immediately without navigation
or reload. A validated `happymeal.language` localStorage value remembers `es` or
`en`; unavailable storage falls back to a session choice. Document language and
title change with the interface. Runtime localization uses Angular signals and
typed catalogs in `src/app/i18n/messages.ts`, without a runtime translation dependency.

Add text to both catalogs, preserving the same keys and `{parameter}` names.
Use `I18n.t`, `I18n.count`, `I18n.number` and `I18n.date` rather than embedding UI
text or formatting display numbers manually. Formatters use `es-ES` and `en-GB`.
There are currently no date fields in the recipe API/UI; the date formatter is
available and tested for future use. Numeric form controls keep machine-readable
values; only display text is formatted.

`ApiErrors` translates backend `code` and per-field `validationErrors` entries,
including indexed field paths and numeric `min`/`max` parameters. Unknown codes,
malformed/non-JSON bodies and older `errors` dictionaries use localized fallbacks.
Server diagnostic prose is never matched or displayed as the translated message.
Components retain original errors and message keys so feedback also changes language.
The backend contract is documented in the [API error-code catalog](../Happymeal-BE/docs/api-error-codes.md).

User-authored recipe content stays untouched. Unit labels are translated while
transport values (`ud`, `cucharadita`, `cucharada`, etc.) remain the same in both languages.

## Browser regression tests

```sh
npx playwright install chromium
npm run test:e2e
```

Playwright starts its own Angular server on `http://localhost:4300` and runs desktop/mobile
Chromium tests. The default suite controls API responses to verify localization,
persisted preference, retained form input, unit payloads, network errors and
published-detail rendering, field errors, navigation feedback and publication without
redundant GETs. CI installs Chromium and runs these tests after the
unit tests and production build. Traces and screenshots stay in ignored
`test-results/` and `playwright-report/` folders.

Set `HAPPYMEAL_E2E_PORT` to another port (1024–65535) if 4300 is occupied. The suite
always starts its own server so it cannot accidentally reuse another worktree's
preview or an instance being changed by HMR. Do not stop an unrelated server to run it.

The live lifecycle suite is opt-in because it writes recipes to the configured
backend. Start the updated backend on port 5094 with a disposable PostgreSQL
database, then run in PowerShell:

```powershell
$env:HAPPYMEAL_LIVE_API = '1'
npm run test:e2e
Remove-Item Env:HAPPYMEAL_LIVE_API
```

It verifies creating a name-only draft, rejecting premature publication with
translated validation feedback, editing, publishing, hiding, exclusion from the
listing and continued detail access. Both Spanish and English run at desktop
and mobile sizes. It creates unique test names and hides those recipes; it does
not reset or delete a database. An additional contract test checks every unit
advertised by the UI against the backend. Without the environment flag these six live
cases are explicitly skipped.

## Form validation and lifecycle feedback

Local validators reject whitespace-only required text, enforce the shared-contract
text/count limits and check tags before submitting. `recipe-validation.ts` keeps
the numeric limits together; backend validation is still authoritative. Field
messages use the same localized rule formatter as API errors and are associated
with their native labels and `aria-describedby`. All form controls expose
`aria-invalid` for local/server feedback. Editing clears only the errors for the
changed field; structural edits clear errors for that collection because indices
can change. The validation banner summarizes rather than repeating field messages.
`FieldError` uses signal inputs, `computed` and RxJS interop for control events.

Detail loads ignore obsolete successes, failures and loading completions after
navigation or a newer retry. A 404 keeps the back link but has no retry action;
network errors, 408, 429 and 5xx can retry. Failed hiding keeps the confirmation
open and shows its error there, allowing retry or cancel without reopening it.

Creation passes the returned recipe through transient router navigation info,
captured during component construction and consumed once. The detail route can
show its saved notice without a second GET; reload/back navigation reads fresh
data rather than reusing persisted navigation state. Editing an existing draft
does not navigate. A successful publication updates its published flag from the
acknowledged POST and retains the visible detail, without a follow-up GET.

Unit labels have singular forms for quantities of one. The seven unit codes are
an explicit API contract in the UI, checked by the live suite; an API endpoint to
discover future units remains a separate enhancement. Run `npm run format:check`
to check formatting. CI runs it, with portable line-ending handling on Windows/Linux.
