import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of } from 'rxjs';
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
        useValue: { paramMap: of(convertToParamMap(recipe ? { id: recipe.id } : {})) },
      },
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(RecipePage);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, page: fixture.componentInstance, api };
}
describe('Recipe lifecycle controls', () => {
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
});
