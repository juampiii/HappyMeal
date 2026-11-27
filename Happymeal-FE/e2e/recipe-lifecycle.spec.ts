import { expect, test } from '@playwright/test';

test.describe('Live backend recipe lifecycle', () => {
  test.skip(
    process.env['HAPPYMEAL_LIVE_API'] !== '1',
    'Requires the current recipe backend on localhost:5094 and a disposable database.',
  );

  test('all unit choices in the UI are accepted by the backend', async ({ page, request }) => {
    await page.goto('/recipes/new');
    await page.getByRole('button', { name: 'Añadir ingrediente' }).click();
    const codes = await page
      .getByRole('combobox', { name: 'Unidad', exact: true })
      .locator('option')
      .evaluateAll((options) =>
        options.map((option) => (option as HTMLOptionElement).value).filter(Boolean),
      );
    expect(codes.length).toBeGreaterThan(0);
    const response = await request.post('/api/recipes', {
      data: {
        name: `Unit contract ${crypto.randomUUID()}`,
        ingredients: codes.map((code) => ({ name: `Ingredient ${code}`, quantity: 1, unit: code })),
      },
    });
    expect(response.status()).toBe(201);
    const recipe = await response.json();
    try {
      expect(recipe.ingredients.map((ingredient: { unit: string }) => ingredient.unit)).toEqual(
        codes,
      );
    } finally {
      await request.delete(`/api/recipes/${recipe.id}`);
    }
  });

  for (const language of ['es', 'en'] as const) {
    test(`creates, publishes and hides a recipe in ${language}`, async ({ page }, testInfo) => {
      const english = language === 'en';
      const name = `E2E ${language} ${testInfo.project.name} ${crypto.randomUUID()}`;
      await page.goto('/recipes/new');
      if (english) await page.getByRole('combobox', { name: 'Idioma' }).selectOption('en');
      await page
        .getByRole('textbox', { name: english ? /Recipe name/ : /Nombre de la receta/ })
        .fill(name);
      await page.getByRole('button', { name: english ? 'Save draft' : 'Guardar borrador' }).click();
      await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
      const detailUrl = page.url();

      // An incomplete draft stays a draft and presents translated publication errors.
      await page
        .getByRole('button', { name: english ? 'Publish recipe' : 'Publicar receta' })
        .click();
      await expect(page.getByRole('alert')).toContainText(english ? 'at least 1' : 'al menos 1');
      await page.getByRole('button', { name: english ? 'Edit draft' : 'Editar borrador' }).click();
      await page
        .getByRole('button', { name: english ? 'Add ingredient' : 'Añadir ingrediente' })
        .click();
      await page
        .getByRole('textbox', { name: english ? 'Ingredient' : 'Ingrediente', exact: true })
        .fill('Sal al gusto');
      await page
        .getByRole('combobox', { name: english ? 'Unit' : 'Unidad', exact: true })
        .selectOption('cucharada');
      await page.getByRole('button', { name: english ? 'Add step' : 'Añadir paso' }).click();
      await page
        .getByRole('textbox', { name: english ? 'Step 1' : 'Paso 1', exact: true })
        .fill('Mezclar');
      await page.getByRole('button', { name: english ? 'Save draft' : 'Guardar borrador' }).click();
      await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
      await page
        .getByRole('button', { name: english ? 'Publish recipe' : 'Publicar receta' })
        .click();
      await expect(page.locator('.badge.published')).toHaveText(
        english ? 'Published' : 'Publicada',
      );
      await expect(
        page.getByRole('button', { name: english ? 'Edit draft' : 'Editar borrador' }),
      ).toHaveCount(0);
      await page.getByRole('button', { name: english ? 'Hide recipe' : 'Ocultar receta' }).click();
      await page.getByRole('button', { name: english ? 'Yes, hide' : 'Sí, ocultar' }).click();
      await expect(page).toHaveURL(new URL('/', detailUrl).href);
      await expect(page.getByRole('heading', { name, exact: true })).toHaveCount(0);
      await page.goto(detailUrl);
      await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
      await expect(
        page
          .locator('.notice')
          .filter({ hasText: english ? 'This recipe is hidden' : 'Esta receta está oculta' }),
      ).toBeVisible();
    });
  }
});
