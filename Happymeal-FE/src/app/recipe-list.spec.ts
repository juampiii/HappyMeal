import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RecipeList } from './recipe-list';
import { Recipes, Recipe } from './recipes';

describe('Localized recipe list', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('translates counts and draft state without refetching or translating recipe content', async () => {
    const recipe: Recipe = {
      id: 'one',
      name: 'Sopa de casa',
      description: null,
      ingredients: [{ name: 'Sal', quantity: null, unit: null }],
      steps: [],
      tags: [],
      servings: null,
      preparationMinutes: null,
      isPublished: false,
      isHidden: false,
      version: 1,
    };
    const api = { list: vi.fn().mockResolvedValue([recipe]) };
    await TestBed.configureTestingModule({
      imports: [RecipeList],
      providers: [provideRouter([]), { provide: Recipes, useValue: api }],
    }).compileComponents();
    const fixture = TestBed.createComponent(RecipeList);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('1 ingrediente');
    fixture.componentInstance.i18n.setLanguage('en');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('1 recipe in the book');
    expect(fixture.nativeElement.textContent).toContain('1 ingredient');
    expect(fixture.nativeElement.textContent).toContain('Draft');
    expect(fixture.nativeElement.textContent).toContain('Sopa de casa');
    expect(api.list).toHaveBeenCalledTimes(1);
  });
});
