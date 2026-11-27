import { TestBed } from '@angular/core/testing';
import { I18n, languageStorageKey } from './i18n';
import { en, es } from './messages';

describe('Runtime localization', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('defaults to Spanish and updates document language and title', () => {
    const i18n = TestBed.inject(I18n);
    expect(i18n.language()).toBe('es');
    expect(document.documentElement.lang).toBe('es');
    expect(document.title).toBe('HappyMeal · Nuestro recetario');
  });

  it('restores English and persists a later choice', () => {
    localStorage.setItem(languageStorageKey, 'en');
    const i18n = TestBed.inject(I18n);
    expect(i18n.t('app.recipeBook')).toBe('Our recipe book');
    expect(document.documentElement.lang).toBe('en');
    i18n.setLanguage('es');
    expect(localStorage.getItem(languageStorageKey)).toBe('es');
  });

  it('ignores unsupported stored and selected languages', () => {
    localStorage.setItem(languageStorageKey, 'unexpected');
    const i18n = TestBed.inject(I18n);
    i18n.setLanguage('fr');
    expect(i18n.language()).toBe('es');
  });

  it('allows a session language change when storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const i18n = TestBed.inject(I18n);
    expect(i18n.language()).toBe('es');
    expect(() => i18n.setLanguage('en')).not.toThrow();
    expect(i18n.t('recipe.save')).toBe('Save draft');
    expect(document.title).toBe('HappyMeal · Our recipe book');
  });

  it('formats numbers, dates and singular/plural text in the active language', () => {
    const i18n = TestBed.inject(I18n);
    const date = new Date(2026, 10, 27, 12);
    expect(i18n.number(12345.5)).toBe('12.345,5');
    expect(i18n.number(null)).toBe('');
    expect(i18n.date(date, { month: 'long' })).toBe('noviembre');
    expect(i18n.count('count.ingredients.one', 'count.ingredients.other', 1)).toBe('1 ingrediente');
    i18n.setLanguage('en');
    expect(i18n.number(12345.5)).toBe('12,345.5');
    expect(i18n.date(date, { month: 'long' })).toBe('November');
    expect(i18n.count('count.ingredients.one', 'count.ingredients.other', 2)).toBe('2 ingredients');
  });

  it('keeps catalog keys and interpolation parameters aligned', () => {
    expect(Object.keys(en).sort()).toEqual(Object.keys(es).sort());
    for (const key of Object.keys(es) as (keyof typeof es)[]) {
      const placeholders = (text: string) =>
        [...text.matchAll(/\{(\w+)\}/g)].map((match) => match[1]).sort();
      expect(placeholders(en[key]), key).toEqual(placeholders(es[key]));
      expect(en[key].trim(), key).not.toBe('');
      expect(es[key].trim(), key).not.toBe('');
    }
  });
});
