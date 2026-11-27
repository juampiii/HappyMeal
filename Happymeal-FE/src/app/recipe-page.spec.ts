import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { RecipePage } from './recipe-page';
import { Recipes, Recipe } from './recipes';
const published: Recipe = {
  id: 'one',
  name: 'Tortilla',
  description: null,
  ingredients: [{ name: 'Sal', quantity: null, unit: null }],
  steps: ['Mezclar'],
  tags: [],
  servings: null,
  preparationMinutes: null,
  isPublished: true,
  isHidden: false,
  version: 1,
};
async function setup(recipe: Recipe | null) {
  const params = new BehaviorSubject(convertToParamMap(recipe ? { id: recipe.id } : {}));
  const api = {
    get: vi.fn().mockResolvedValue(recipe),
    save: vi.fn(),
    publish: vi.fn(),
    hide: vi.fn(),
  };
  await TestBed.configureTestingModule({
    imports: [RecipePage],
    providers: [
      provideRouter([]),
      { provide: Recipes, useValue: api },
      {
        provide: ActivatedRoute,
        useValue: { paramMap: params },
      },
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(RecipePage);
  vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, page: fixture.componentInstance, api, params };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (error: unknown) => void;
  const promise = new Promise<T>((success, failure) => {
    resolve = success;
    reject = failure;
  });
  return { promise, resolve, reject };
}
describe('Recipe lifecycle controls', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('ignores an old recipe response after navigating to another recipe', async () => {
    const { page, api, params } = await setup(published);
    const old = deferred<Recipe>();
    const current = deferred<Recipe>();
    api.get.mockImplementation((id) => (id === 'two' ? old.promise : current.promise));
    params.next(convertToParamMap({ id: 'two' }));
    params.next(convertToParamMap({ id: 'three' }));
    current.resolve({ ...published, id: 'three', name: 'Current' });
    await current.promise;
    old.resolve({ ...published, id: 'two', name: 'Old' });
    await old.promise;
    expect(page.recipe()?.name).toBe('Current');
    expect(page.loading()).toBe(false);
    expect(page.error()).toBe('');
  });

  it('does not clear loading or show an old error while the current request is pending', async () => {
    const { page, api, params } = await setup(published);
    const old = deferred<Recipe>();
    const current = deferred<Recipe>();
    api.get.mockImplementation((id) => (id === 'two' ? old.promise : current.promise));
    params.next(convertToParamMap({ id: 'two' }));
    params.next(convertToParamMap({ id: 'three' }));
    old.reject(new HttpErrorResponse({ status: 503 }));
    await old.promise.catch(() => {});
    expect(page.loading()).toBe(true);
    expect(page.error()).toBe('');
    current.resolve({ ...published, id: 'three' });
    await current.promise;
    expect(page.recipe()?.id).toBe('three');
  });

  it('keeps the newest retry result when requests target the same recipe', async () => {
    const { page, api } = await setup(published);
    const old = deferred<Recipe>();
    const current = deferred<Recipe>();
    api.get.mockReturnValueOnce(old.promise).mockReturnValueOnce(current.promise);
    const firstLoad = page.load(published.id);
    const secondLoad = page.load(published.id);
    current.resolve({ ...published, name: 'Newest' });
    await secondLoad;
    old.resolve({ ...published, name: 'Stale' });
    await firstLoad;
    expect(page.recipe()?.name).toBe('Newest');
  });

  it('clears only the changed field error and does not repeat field messages in the banner', async () => {
    const { fixture, page, api } = await setup(null);
    page.form.patchValue({ name: 'Soup', description: 'Description', tags: 'quick' });
    api.save.mockRejectedValue(
      new HttpErrorResponse({
        status: 400,
        error: {
          validationErrors: {
            description: [{ code: 'validation.maxLength', parameters: { max: 5 } }],
            'tags[0]': [{ code: 'validation.maxLength', parameters: { max: 3 } }],
          },
        },
      }),
    );
    await page.save();
    fixture.detectChanges();
    expect(page.error()).not.toContain('caracteres');
    expect(fixture.nativeElement.querySelector('#description-error').textContent).toContain(
      '5 caracteres',
    );
    page.form.controls.tags.setValue('ok');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#tags-error').textContent.trim()).toBe('');
    expect(fixture.nativeElement.querySelector('#description-error').textContent).toContain(
      '5 caracteres',
    );
    expect(
      fixture.nativeElement.querySelector('#recipe-description').getAttribute('aria-invalid'),
    ).toBe('true');
    expect(page.error()).not.toBe('');
    page.form.controls.description.setValue('Good');
    fixture.detectChanges();
    expect(page.error()).toBe('');
  });

  it('preserves an ingredient name error when its quantity is edited', async () => {
    const { page, api } = await setup(null);
    page.form.controls.name.setValue('Soup');
    page.addIngredient();
    page.ingredients.at(0).patchValue({ name: 'Salt', quantity: 1 });
    api.save.mockRejectedValue(
      new HttpErrorResponse({
        status: 400,
        error: {
          validationErrors: {
            'ingredients[0].name': [{ code: 'validation.duplicate', parameters: {} }],
          },
        },
      }),
    );
    await page.save();
    page.ingredients.at(0).controls.quantity.setValue(2);
    expect(page.fieldInvalid(page.ingredients.at(0).controls.name, 'ingredients[0].name')).toBe(
      true,
    );
    page.ingredients.at(0).controls.name.setValue('Pepper');
    expect(page.error()).toBe('');
  });

  it('keeps hide confirmation open on failure and permits retry without reopening it', async () => {
    const { fixture, page, api } = await setup(published);
    page.askToHide();
    api.hide.mockRejectedValueOnce(new HttpErrorResponse({ status: 503 }));
    await page.hide();
    fixture.detectChanges();
    expect(page.confirmHide()).toBe(true);
    expect(page.recipe()?.isHidden).toBe(false);
    expect(fixture.nativeElement.querySelector('#hide-confirmation [role=alert]')).not.toBeNull();
    api.hide.mockResolvedValue(undefined);
    await page.hide();
    expect(page.confirmHide()).toBe(false);
    expect(page.recipe()?.isHidden).toBe(true);
  });

  it('does not add separator whitespace when an ingredient has no quantity', async () => {
    const { page } = await setup(published);
    expect(page.ingredientAmount({ name: 'Salt', quantity: null, unit: 'g' })).toBe('gramos');
    expect(page.ingredientAmount({ name: 'Salt', quantity: 1, unit: null })).toBe('1');
    expect(page.ingredientAmount({ name: 'Salt', quantity: null, unit: null })).toBe('');
  });

  it('publishes without clearing content or fetching the detail again', async () => {
    const { page, api } = await setup({ ...published, isPublished: false });
    api.publish.mockResolvedValue(undefined);
    api.get.mockRejectedValue(new Error('A redundant refresh would fail'));
    await page.publish();
    expect(api.get).toHaveBeenCalledTimes(1);
    expect(page.loading()).toBe(false);
    expect(page.recipe()?.isPublished).toBe(true);
    expect(page.recipe()?.steps).toEqual(['Mezclar']);
    expect(page.message()).toContain('Receta publicada');
    expect(page.error()).toBe('');
  });

  it('keeps the detail visible while publication is pending and retains the draft on failure', async () => {
    const { page, api } = await setup({ ...published, isPublished: false });
    let rejectPublication!: (error: unknown) => void;
    api.publish.mockReturnValue(
      new Promise((_, reject) => {
        rejectPublication = reject;
      }),
    );
    const publication = page.publish();
    expect(page.busy()).toBe(true);
    expect(page.loading()).toBe(false);
    expect(page.recipe()?.name).toBe('Tortilla');
    rejectPublication(
      new HttpErrorResponse({
        status: 400,
        error: {
          validationErrors: {
            ingredients: [{ code: 'validation.minItems', parameters: { min: 1 } }],
          },
        },
      }),
    );
    await publication;
    expect(page.recipe()?.isPublished).toBe(false);
    expect(page.message()).toBe('');
    expect(page.error()).toContain('al menos 1 elemento');
  });

  it('rejects whitespace-only ingredient names and steps with localized field feedback', async () => {
    const { fixture, page, api } = await setup(null);
    page.form.controls.name.setValue('Sopa');
    page.addIngredient();
    page.addStep();
    page.ingredients.at(0).controls.name.setValue('   ');
    page.steps.at(0).setValue('   ');
    await page.save();
    fixture.detectChanges();
    expect(api.save).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('#ingredient-name-error-0').textContent).toContain(
      'Completa',
    );
    expect(fixture.nativeElement.querySelector('#step-error-0').textContent).toContain('Completa');
    page.i18n.setLanguage('en');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#step-error-0').textContent).toContain(
      'Step 1 is required',
    );
  });

  it('shows server validation beside tags and clears it when the user edits', async () => {
    const { fixture, page, api } = await setup(null);
    page.form.controls.name.setValue('Sopa');
    api.save.mockRejectedValue(
      new HttpErrorResponse({
        status: 400,
        error: {
          validationErrors: {
            'tags[0]': [{ code: 'validation.maxLength', parameters: { max: 5 } }],
          },
        },
      }),
    );
    await page.save();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#tags-error').textContent).toContain(
      '5 caracteres',
    );
    page.form.controls.tags.setValue('quick');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#tags-error').textContent.trim()).toBe('');
    expect(page.error()).toBe('');
  });

  it('saves edits without navigation and clears the notice before a new action', async () => {
    const draft = { ...published, isPublished: false };
    const { page, api } = await setup(draft);
    page.edit();
    page.form.controls.name.setValue('Tortilla nueva');
    api.save.mockResolvedValue({ ...draft, name: 'Tortilla nueva' });
    await page.save();
    expect(TestBed.inject(Router).navigate).not.toHaveBeenCalled();
    expect(page.message()).toBe('Borrador guardado.');
    page.askToHide();
    expect(page.message()).toBe('');
    expect(page.confirmHide()).toBe(true);
    page.cancelHide();
    expect(page.confirmHide()).toBe(false);
    page.edit();
    page.cancelEdit();
    expect(page.error()).toBe('');
  });

  it('retries a failed detail load and resets the old error', async () => {
    const { page, api } = await setup(published);
    api.get.mockRejectedValueOnce(
      new HttpErrorResponse({ status: 503, error: { code: 'error.unexpected' } }),
    );
    await page.load(published.id);
    expect(page.error()).toContain('inesperado');
    expect(page.canRetryLoad()).toBe(true);
    api.get.mockResolvedValue(published);
    page.retryLoad();
    await Promise.resolve();
    expect(page.error()).toBe('');
    expect(api.get).toHaveBeenCalledTimes(3);
  });

  it('uses singular unit labels for an amount of one in both languages', async () => {
    const { page } = await setup(published);
    expect(page.unitLabel('g', 1)).toBe('gramo');
    expect(page.unitLabel('ud', 1)).toBe('unidad');
    expect(page.unitLabel('g', 2)).toBe('gramos');
    page.i18n.setLanguage('en');
    expect(page.unitLabel('cucharada', 1)).toBe('tablespoon');
    expect(page.unitLabel('ud', 1)).toBe('unit');
  });
  it('renders published content without an edit or publish action', async () => {
    const { fixture } = await setup(published);
    const buttons = [...fixture.nativeElement.querySelectorAll('button')].map(
      (b: any) => b.textContent,
    );
    expect(buttons.some((text: string) => text.includes('Editar borrador'))).toBe(false);
    expect(buttons.some((text: string) => text.includes('Publicar receta'))).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Tortilla');
  });
  it('retains draft input and displays Spanish feedback after a failed save', async () => {
    const { page, api } = await setup(null);
    page.form.controls.name.setValue('Sopa pendiente');
    api.save.mockRejectedValue(new HttpErrorResponse({ status: 500 }));
    await page.save();
    expect(page.form.controls.name.value).toBe('Sopa pendiente');
    expect(page.editing()).toBe(true);
    expect(page.error()).toContain('Tus datos siguen');
  });
  it('keeps a recipe in place when hiding fails', async () => {
    const { page, api } = await setup(published);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate');
    api.hide.mockRejectedValue(new HttpErrorResponse({ status: 500 }));
    await page.hide();
    expect(navigate).not.toHaveBeenCalled();
    expect(page.recipe()?.isHidden).toBe(false);
    expect(page.error()).not.toBe('');
  });
  it('saves a name-only draft without requiring ingredients or steps', async () => {
    const { page, api } = await setup(null);
    page.form.controls.name.setValue('Sopa');
    api.save.mockResolvedValue({
      ...published,
      name: 'Sopa',
      isPublished: false,
      ingredients: [],
      steps: [],
    });
    await page.save();
    expect(api.save).toHaveBeenCalledWith(
      null,
      expect.objectContaining({ name: 'Sopa', ingredients: [], steps: [] }),
    );
  });
  it('switches labels and existing errors without losing dirty input or unit codes', async () => {
    const { fixture, page, api } = await setup(null);
    page.addIngredient();
    page.addStep();
    page.form.patchValue({
      name: 'Sopa pendiente',
      ingredients: [{ name: 'Sal', quantity: 0.5, unit: 'cucharada' }],
      steps: ['Mezclar'],
    });
    page.form.markAsDirty();
    api.save.mockRejectedValue(
      new HttpErrorResponse({ status: 409, error: { code: 'recipe.nameAlreadyExists' } }),
    );
    await page.save();
    const input = page.form.getRawValue();
    expect(page.error()).toContain('Ya existe');
    page.i18n.setLanguage('en');
    fixture.detectChanges();
    expect(page.form.getRawValue()).toEqual(input);
    expect(page.form.dirty).toBe(true);
    expect(page.error()).toContain('A visible recipe');
    expect(fixture.nativeElement.textContent).toContain('Save draft');
    expect(
      fixture.nativeElement.querySelector('[aria-label="Remove ingredient 1"]'),
    ).not.toBeNull();
    expect(fixture.nativeElement.querySelector('option[value="cucharada"]').textContent).toContain(
      'tablespoons',
    );
    expect(api.save.mock.calls[0][1].ingredients[0].unit).toBe('cucharada');
  });

  it('translates units, numbers and notices while keeping published recipe content intact', async () => {
    const { fixture, page } = await setup({
      ...published,
      ingredients: [{ name: 'Sal al gusto', quantity: 0.5, unit: 'cucharada' }],
    });
    expect(fixture.nativeElement.textContent).toContain('0,5');
    page.i18n.setLanguage('en');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('0.5');
    expect(fixture.nativeElement.textContent).toContain('tablespoons');
    expect(fixture.nativeElement.textContent).toContain('This recipe is published');
    expect(fixture.nativeElement.textContent).toContain('Sal al gusto');
    expect(fixture.nativeElement.textContent).toContain('Mezclar');
    expect(fixture.nativeElement.textContent).toContain('Tortilla');
  });

  it('retranslates a successful-save notice without another API request', async () => {
    const { page, api } = await setup(null);
    page.form.controls.name.setValue('Sopa');
    api.save.mockResolvedValue({ ...published, name: 'Sopa', isPublished: false });
    await page.save();
    expect(page.message()).toBe('Borrador guardado.');
    page.i18n.setLanguage('en');
    expect(page.message()).toBe('Draft saved.');
    expect(api.save).toHaveBeenCalledTimes(1);
  });
});
