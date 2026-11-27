# HappyMeal frontend

- Use reactive forms, Router and HttpClient. The API owns business rules.
- Localize all application text in Spanish and English; default to Spanish.
  Use the typed catalogs and I18n service, including accessible text and feedback.
- Translate API codes and parameters, never server prose. Preserve user-written
  content, transport unit codes and unsaved input across failures/language changes.
- Keep field errors relevant, avoid duplicate announcements, and respect the
  recipe lifecycle. See [README](README.md) for implementation and API details.
- Do not add UI/state dependencies without agreement. Playwright is the agreed
  development-only browser test tool.
- Run `npm run format:check`, `npm test -- --watch=false`, `npm run build` and
  relevant Playwright checks after meaningful changes.
- Write code/docs in English. Authentication belongs to #35; PWA installation to #36.
