# HappyMeal recipe book

Angular interface in Spanish. Authentication belongs to story #35; installable PWA support to #36.

## Local development

Use Node from `.nvmrc`. Run `npm ci`, then `npm start`. Start the backend separately with `dotnet run --project src/Happymeal.API --urls http://localhost:5094` from `Happymeal-BE`. The development proxy forwards `/api/**` to that local address. No broad CORS policy is needed.

Run `npm test -- --watch=false` and `npm run build` to verify the frontend. The production build is ready for the separate packaging delivery. This frontend branch uses the recipe API contract from `feature/34-manage-recipes-be`; merge that backend delivery before validating the complete flow. Shared API-image hosting is not included in this branch.
