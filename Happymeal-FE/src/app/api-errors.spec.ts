import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrors } from './api-errors';
import { I18n } from './i18n/i18n';

describe('Localized API errors', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('offers retry only for network, timeout, rate-limit and server failures', () => {
    const errors = TestBed.inject(ApiErrors);
    for (const status of [0, 408, 429, 500, 503]) {
      expect(errors.isRetryable(new HttpErrorResponse({ status }))).toBe(true);
    }
    for (const status of [400, 401, 403, 404, 405, 409, 415]) {
      expect(errors.isRetryable(new HttpErrorResponse({ status }))).toBe(false);
    }
  });

  it('uses singular and plural validation text for publication requirements', () => {
    const errors = TestBed.inject(ApiErrors);
    const requirement = (min: number) =>
      new HttpErrorResponse({
        status: 400,
        error: {
          validationErrors: { ingredients: [{ code: 'validation.minItems', parameters: { min } }] },
        },
      });
    expect(errors.message(requirement(1))).toContain('1 elemento');
    TestBed.inject(I18n).setLanguage('en');
    expect(errors.message(requirement(1))).toContain('1 item');
    expect(errors.message(requirement(2))).toContain('2 items');
  });

  it('translates a stable code independently of server prose and language', () => {
    const errors = TestBed.inject(ApiErrors);
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        code: 'recipe.nameAlreadyExists',
        detail: 'ARBITRARY SERVER TEXT',
      },
    });
    expect(errors.message(error)).toBe('Ya existe una receta visible con ese nombre.');
    TestBed.inject(I18n).setLanguage('en');
    expect(errors.message(error)).toBe('A visible recipe with that name already exists.');
  });

  it('translates indexed fields and interpolates validation limits', () => {
    const errors = TestBed.inject(ApiErrors);
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        code: 'validation.failed',
        validationErrors: {
          'tags[1]': [{ code: 'validation.maxLength', parameters: { max: 50 } }],
          'ingredients[0].unit': [{ code: 'validation.unsupportedValue', parameters: {} }],
        },
      },
    });
    expect(errors.message(error)).toContain('«Etiqueta 2» admite hasta 50 caracteres.');
    TestBed.inject(I18n).setLanguage('en');
    expect(errors.message(error)).toContain('Tag 2 allows up to 50 characters.');
    expect(errors.message(error)).toContain('Ingredient 1 unit');
  });

  it('falls back for unknown rule codes and missing limit parameters', () => {
    const errors = TestBed.inject(ApiErrors);
    TestBed.inject(I18n).setLanguage('en');
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        validationErrors: {
          name: [
            { code: 'future.rule' },
            { code: 'validation.maxLength', parameters: { max: 'invalid' } },
          ],
        },
      },
    });
    expect(errors.message(error)).toBe('Check Recipe name.');
    expect(errors.message(error)).not.toContain('{max}');
  });

  it('uses localized fallbacks for unknown outcome codes and non-JSON bodies', () => {
    const errors = TestBed.inject(ApiErrors);
    TestBed.inject(I18n).setLanguage('en');
    expect(
      errors.message(new HttpErrorResponse({ status: 500, error: '<html>SERVER ERROR</html>' })),
    ).toContain('Your changes could not be saved.');
    expect(
      errors.message(
        new HttpErrorResponse({
          status: 409,
          error: { code: 'future.conflict', detail: 'SECRET' },
        }),
      ),
    ).toContain('conflicts with the current state');
  });

  it('supports legacy field keys without displaying legacy messages', () => {
    const errors = TestBed.inject(ApiErrors);
    TestBed.inject(I18n).setLanguage('en');
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { name: ['SECRET ENGLISH MESSAGE'] } },
    });
    expect(errors.message(error)).toBe('Check: Recipe name.');
  });

  it('localizes connection and malformed-request errors', () => {
    const errors = TestBed.inject(ApiErrors);
    expect(errors.message(new HttpErrorResponse({ status: 0 }))).toContain('Comprueba la conexión');
    TestBed.inject(I18n).setLanguage('en');
    expect(errors.message(new HttpErrorResponse({ status: 0 }))).toContain('Check your connection');
    expect(
      errors.message(
        new HttpErrorResponse({
          status: 400,
          error: {
            validationErrors: {
              '$.servings': [{ code: 'request.invalidValue', parameters: {} }],
            },
          },
        }),
      ),
    ).toBe('Check the value of “Entered data”.');
  });
});
