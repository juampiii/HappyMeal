import { expect, test } from '@playwright/test';

const draft = {
  id: 'example',
  name: 'Sopa de casa',
  description: null,
  ingredients: [{ name: 'Sal', quantity: 1, unit: 'g' }],
  steps: ['Mezclar'],
  tags: [],
  servings: null,
  preparationMinutes: null,
  version: 1,
  isPublished: false,
  isHidden: false,
};

test('a permanent 404 offers the back link without a retry button', async ({ page }) => {
  await page.route('**/api/recipes/example', (route) =>
    route.fulfill({ status: 404, json: { code: 'recipe.notFound' } }),
  );
  await page.goto('/recipes/example');
  await expect(page.getByRole('alert')).toContainText('No se ha encontrado');
  await expect(page.getByRole('button', { name: 'Reintentar' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Volver al recetario' })).toBeVisible();
});

test('editing one field preserves other server errors and uses native accessible labels', async ({
  page,
}) => {
  await page.route('**/api/recipes', (route) =>
    route.fulfill({
      status: 400,
      json: {
        code: 'validation.failed',
        validationErrors: {
          description: [{ code: 'validation.maxLength', parameters: { max: 5 } }],
          'tags[0]': [{ code: 'validation.maxLength', parameters: { max: 3 } }],
        },
      },
    }),
  );
  await page.goto('/recipes/new');
  await page.getByRole('textbox', { name: 'Nombre de la receta' }).fill('Soup');
  const description = page.getByRole('textbox', { name: 'Descripción', exact: true });
  const tags = page.getByRole('textbox', { name: 'Etiquetas', exact: true });
  await description.fill('Description');
  await tags.fill('quick');
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  await expect(page.getByRole('alert')).toHaveText(
    'Revisa los campos con errores antes de guardar.',
  );
  await expect(description).toHaveAccessibleName('Descripción');
  expect(await description.getAttribute('aria-label')).toBeNull();
  await expect(description).toHaveAttribute('aria-invalid', 'true');
  await expect(tags).toHaveAttribute('aria-invalid', 'true');
  await tags.fill('ok');
  await expect(page.locator('#tags-error')).toHaveText('');
  await expect(tags).toHaveAttribute('aria-invalid', 'false');
  await expect(page.locator('#description-error')).toContainText('5 caracteres');
  await expect(description).toHaveAttribute('aria-invalid', 'true');
  await description.fill('Good');
  await expect(page.getByRole('alert')).toHaveCount(0);
  await expect(description).toHaveAttribute('aria-invalid', 'false');
});

test('failed hiding keeps confirmation and its error available for retry', async ({ page }) => {
  let deletes = 0;
  await page.route('**/api/recipes', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/recipes/example', (route) => {
    if (route.request().method() !== 'DELETE') return route.fulfill({ json: draft });
    deletes++;
    return deletes === 1
      ? route.fulfill({ status: 503, json: { code: 'error.unexpected' } })
      : route.fulfill({ status: 204 });
  });
  await page.goto('/recipes/example');
  const bookUrl = new URL('/', page.url()).href;
  await page.getByRole('button', { name: 'Ocultar receta' }).click();
  await page.getByRole('button', { name: 'Sí, ocultar' }).click();
  await expect(page.locator('#hide-confirmation')).toBeVisible();
  await expect(page.locator('#hide-confirmation').getByRole('alert')).toContainText('inesperado');
  await expect(page.getByRole('button', { name: 'Sí, ocultar' })).toBeEnabled();
  await page.getByRole('button', { name: 'Sí, ocultar' }).click();
  await expect(page).toHaveURL(bookUrl);
  expect(deletes).toBe(2);
});

test('keeps the save confirmation across navigation without fetching the created recipe', async ({
  page,
}) => {
  let detailRequests = 0;
  await page.route('**/api/recipes', (route) => route.fulfill({ status: 201, json: draft }));
  await page.route('**/api/recipes/example', (route) => {
    detailRequests++;
    return route.fulfill({ json: draft });
  });
  await page.goto('/recipes/new');
  await page.getByRole('textbox', { name: 'Nombre de la receta' }).fill('Sopa de casa');
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  await expect(page).toHaveURL(/\/recipes\/example$/);
  await expect(page.getByRole('status')).toHaveText('Borrador guardado.');
  await expect(page.getByRole('heading', { name: 'Sopa de casa' })).toBeVisible();
  expect(detailRequests).toBe(0);
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
  await expect(page.getByRole('status')).toHaveText('Draft saved.');
  await expect(page.locator('.ingredients')).toContainText('1 gram');
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Sopa de casa' })).toBeVisible();
  expect(detailRequests).toBe(1);
  await expect(page.getByText('Draft saved.', { exact: true })).toHaveCount(0);
});

test('publishes without a detail reload or loading flash', async ({ page }) => {
  let detailRequests = 0;
  let finishPublication!: () => void;
  const publicationReady = new Promise<void>((resolve) => {
    finishPublication = resolve;
  });
  await page.route('**/api/recipes/example', (route) => {
    detailRequests++;
    return detailRequests === 1 ? route.fulfill({ json: draft }) : route.abort();
  });
  await page.route('**/api/recipes/example/publish', async (route) => {
    await publicationReady;
    await route.fulfill({ status: 204 });
  });
  await page.goto('/recipes/example');
  await page.getByRole('button', { name: 'Publicar receta' }).click();
  await expect(page.getByRole('heading', { name: 'Sopa de casa' })).toBeVisible();
  await expect(page.getByText('Cargando receta…', { exact: true })).toHaveCount(0);
  finishPublication();
  await expect(page.locator('.badge.published')).toHaveText('Publicada');
  await expect(page.getByRole('status')).toContainText('Receta publicada.');
  await expect(page.getByRole('alert')).toHaveCount(0);
  expect(detailRequests).toBe(1);
});

test('shows translated field errors for invalid local input without submitting', async ({
  page,
}, testInfo) => {
  let submissions = 0;
  await page.route('**/api/recipes', (route) => {
    submissions++;
    return route.fulfill({ status: 500 });
  });
  await page.goto('/recipes/new');
  await page.getByRole('textbox', { name: 'Nombre de la receta' }).fill('Sopa');
  await page.getByRole('textbox', { name: 'Etiquetas', exact: true }).fill('x'.repeat(51));
  await page.getByRole('button', { name: 'Añadir ingrediente' }).click();
  await page.getByRole('textbox', { name: 'Ingrediente', exact: true }).fill('   ');
  await page.getByRole('button', { name: 'Añadir paso' }).click();
  await page.getByRole('textbox', { name: 'Paso 1', exact: true }).fill('   ');
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  await expect(page.locator('#tags-error')).toContainText('50 caracteres');
  await expect(page.locator('#ingredient-name-error-0')).toContainText('Completa');
  await expect(page.locator('#step-error-0')).toContainText('Completa');
  expect(submissions).toBe(0);
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
  await expect(page.locator('#step-error-0')).toHaveText('Step 1 is required.');
  await expect(page.locator('#tags-error')).toContainText('50 characters');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
  await page.screenshot({ path: testInfo.outputPath('field-errors.png'), fullPage: true });
});

test('offers a retry after a transient detail failure and recovers without stale feedback', async ({
  page,
}) => {
  let requests = 0;
  await page.route('**/api/recipes/example', (route) => {
    requests++;
    return requests === 1
      ? route.fulfill({ status: 503, json: { code: 'error.unexpected' } })
      : route.fulfill({ json: draft });
  });
  await page.goto('/recipes/example');
  await expect(page.getByRole('alert')).toContainText('inesperado');
  await expect(page.getByRole('link', { name: 'Volver al recetario' })).toBeVisible();
  await page.getByRole('button', { name: 'Reintentar' }).click();
  await expect(page.getByRole('heading', { name: 'Sopa de casa' })).toBeVisible();
  await expect(page.getByRole('alert')).toHaveCount(0);
});

test('switches language immediately and remembers it after reload', async ({ page }) => {
  await page.route('**/api/recipes', (route) => route.fulfill({ json: [] }));
  await page.goto('/');
  await expect(
    page.getByRole('heading', { name: 'Tu próxima favorita empieza aquí' }),
  ).toBeVisible();
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
  await expect(page.getByRole('heading', { name: 'Your next favorite starts here' })).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page).toHaveTitle('HappyMeal · Our recipe book');
  await page.getByRole('link', { name: 'Create recipe', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'A new recipe' })).toBeVisible();
  await page.reload();
  await expect(page.getByRole('button', { name: 'Save draft' })).toBeVisible();
  await expect(page.getByRole('combobox', { name: 'Language' })).toHaveValue('en');
  await expect(page.locator('body')).not.toContainText('Guardar borrador');
});

test('retranslates coded errors without losing form input or changing unit codes', async ({
  page,
}) => {
  const submitted: Record<string, unknown>[] = [];
  await page.route('**/api/recipes', async (route) => {
    if (route.request().method() === 'GET') return route.fulfill({ json: [] });
    submitted.push(route.request().postDataJSON());
    await route.fulfill({
      status: 400,
      json: {
        code: 'validation.failed',
        errors: { 'tags[0]': ['INTERNAL SERVER MESSAGE'] },
        validationErrors: {
          'tags[0]': [{ code: 'validation.maxLength', parameters: { max: 5 } }],
        },
      },
    });
  });
  await page.goto('/recipes/new');
  await page.getByRole('textbox', { name: /Nombre de la receta/ }).fill('Mi sopa');
  await page.getByRole('textbox', { name: 'Descripción', exact: true }).fill('Una receta de casa');
  await page.getByRole('textbox', { name: /Etiquetas/ }).fill('rápida');
  await page.getByRole('button', { name: 'Añadir ingrediente' }).click();
  await page.getByRole('textbox', { name: 'Ingrediente', exact: true }).fill('Sal al gusto');
  await page.getByRole('spinbutton', { name: 'Cantidad', exact: true }).fill('0.5');
  await page.getByRole('combobox', { name: 'Unidad', exact: true }).selectOption('cucharada');
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  await expect(page.locator('#tags-error')).toContainText('5 caracteres');
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
  await expect(page.getByRole('alert')).toHaveText('Check the fields with errors before saving.');
  await expect(page.locator('#tags-error')).toHaveText('Tag 1 allows up to 5 characters.');
  await expect(page.getByRole('textbox', { name: /Recipe name/ })).toHaveValue('Mi sopa');
  await expect(page.getByRole('textbox', { name: 'Description', exact: true })).toHaveValue(
    'Una receta de casa',
  );
  await expect(page.getByRole('textbox', { name: 'Ingredient', exact: true })).toHaveValue(
    'Sal al gusto',
  );
  await expect(page.getByRole('combobox', { name: 'Unit', exact: true })).toHaveValue('cucharada');
  await expect(page.getByRole('option', { name: 'tablespoons', exact: true })).toHaveAttribute(
    'value',
    'cucharada',
  );
  expect(submitted).toHaveLength(1);
  expect(submitted[0]['ingredients']).toEqual([
    { name: 'Sal al gusto', quantity: 0.5, unit: 'cucharada' },
  ]);
  await expect(page.locator('body')).not.toContainText('INTERNAL SERVER MESSAGE');
});

test('localizes a published detail while preserving user content', async ({ page }, testInfo) => {
  await page.route('**/api/recipes/example', (route) =>
    route.fulfill({
      json: {
        id: 'example',
        name: 'Tortilla de casa',
        description: 'Como la de mi abuela',
        ingredients: [{ name: 'Sal al gusto', quantity: 0.5, unit: 'cucharada' }],
        steps: ['Mezclar despacio'],
        tags: ['rápida'],
        servings: 2,
        preparationMinutes: 30,
        version: 1,
        isPublished: true,
        isHidden: false,
      },
    }),
  );
  await page.goto('/recipes/example');
  await expect(page.locator('.ingredients')).toContainText('0,5 cucharadas');
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
  await expect(page.locator('.ingredients')).toContainText('0.5 tablespoons');
  await expect(page.locator('.recipe-meta')).toContainText('2 servings');
  await expect(page.getByRole('heading', { name: 'Tortilla de casa' })).toBeVisible();
  await expect(page.locator('.steps')).toContainText('Mezclar despacio');
  await expect(page.getByRole('button', { name: 'Edit draft' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Publish recipe' })).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
  await page.screenshot({ path: testInfo.outputPath('localized-detail.png'), fullPage: true });
});

test('translates a network error and retry action in place', async ({ page }) => {
  await page.route('**/api/recipes', (route) => route.abort('failed'));
  await page.goto('/');
  await expect(page.getByRole('alert')).toContainText('Comprueba la conexión');
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
  await expect(page.getByRole('alert')).toContainText('Check your connection');
  await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
});
