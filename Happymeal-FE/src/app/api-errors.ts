import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { I18n, TranslationParameters } from './i18n/i18n';
import { TranslationKey } from './i18n/messages';

const outcomeCodes: Record<string, TranslationKey> = {
  'recipe.notFound': 'recipe.notFound',
  'recipe.nameAlreadyExists': 'recipe.nameAlreadyExists',
  'recipe.idAlreadyExists': 'recipe.idAlreadyExists',
  'recipe.notEditable': 'recipe.notEditable',
  'recipe.hidden': 'recipe.hidden',
  'recipe.concurrentChange': 'recipe.concurrentChange',
  'recipe.saveFailed': 'recipe.saveFailed',
  'resource.notFound': 'resource.notFound',
  'resource.conflict': 'resource.conflict',
  'request.invalid': 'request.invalid',
  'http.methodNotAllowed': 'http.methodNotAllowed',
  'http.unsupportedMediaType': 'http.unsupportedMediaType',
  'http.error': 'http.error',
  'error.failure': 'error.failure',
  'error.unexpected': 'error.unexpected',
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

@Injectable({ providedIn: 'root' })
export class ApiErrors {
  private i18n = inject(I18n);

  hasFieldErrors(error: unknown): boolean {
    return (
      error instanceof HttpErrorResponse &&
      error.status === 400 &&
      isRecord(error.error) &&
      isRecord(error.error['validationErrors']) &&
      Object.values(error.error['validationErrors']).some(
        (entries) => Array.isArray(entries) && entries.length > 0,
      )
    );
  }

  clearField(error: unknown, path: string): unknown | null {
    if (!this.hasFieldErrors(error)) return null;
    const response = error as HttpErrorResponse;
    const body = response.error as Record<string, unknown>;
    const fields = body['validationErrors'] as Record<string, unknown>;
    const remaining = Object.fromEntries(
      Object.entries(fields).filter(
        ([field]) =>
          path !== '' &&
          field !== path &&
          !field.startsWith(`${path}.`) &&
          !field.startsWith(`${path}[`),
      ),
    );
    if (!Object.keys(remaining).length) return null;
    if (Object.keys(remaining).length === Object.keys(fields).length) return error;
    return new HttpErrorResponse({
      status: response.status,
      statusText: response.statusText,
      headers: response.headers,
      url: response.url ?? undefined,
      error: { ...body, validationErrors: remaining },
    });
  }

  isRetryable(error: unknown): boolean {
    return (
      error instanceof HttpErrorResponse &&
      (error.status === 0 || error.status === 408 || error.status === 429 || error.status >= 500)
    );
  }

  message(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) return this.i18n.t('error.generic');
    if (error.status === 0) return this.i18n.t('error.network');
    const body = isRecord(error.error) ? error.error : {};
    const code = body['code'];
    if (error.status === 400 && isRecord(body['validationErrors'])) {
      const messages = Object.entries(body['validationErrors']).flatMap(([path, entries]) =>
        Array.isArray(entries) ? entries.map((entry) => this.validationMessage(path, entry)) : [],
      );
      if (messages.length) return [...new Set(messages)].join(' ');
    }
    if (typeof code === 'string' && Object.hasOwn(outcomeCodes, code)) {
      return this.i18n.t(outcomeCodes[code]);
    }
    // Older servers can still supply field names, but their English prose is never displayed.
    if (error.status === 400 && isRecord(body['errors'])) {
      const fields = [...new Set(Object.keys(body['errors']).map((path) => this.fieldLabel(path)))];
      if (fields.length) return this.i18n.t('error.reviewFields', { fields: fields.join(', ') });
    }
    if (error.status === 404) return this.i18n.t('recipe.notFound');
    if (error.status === 409) return this.i18n.t('resource.conflict');
    if (error.status === 400) return this.i18n.t('error.validation');
    if (error.status === 405) return this.i18n.t('http.methodNotAllowed');
    if (error.status === 415) return this.i18n.t('http.unsupportedMediaType');
    return this.i18n.t('error.saveFailed');
  }

  fieldMessages(error: unknown, path: string): string[] {
    if (!(error instanceof HttpErrorResponse) || error.status !== 400 || !isRecord(error.error))
      return [];
    const fields = error.error['validationErrors'];
    if (!isRecord(fields)) return [];
    return Object.entries(fields)
      .filter(([field]) => field === path || (path === 'tags' && field.startsWith('tags[')))
      .flatMap(([field, entries]) =>
        Array.isArray(entries) ? entries.map((entry) => this.validationMessage(field, entry)) : [],
      );
  }

  validationMessage(path: string, entry: unknown): string {
    const field = this.fieldLabel(path);
    if (!isRecord(entry)) return this.i18n.t('validation.invalid', { field });
    const code = entry['code'];
    const allowed = [
      'validation.required',
      'validation.maxLength',
      'validation.maxItems',
      'validation.minItems',
      'validation.positive',
      'validation.integer',
      'validation.minValue',
      'validation.duplicate',
      'validation.unsupportedValue',
      'validation.emptyId',
      'validation.invalid',
      'request.invalidValue',
    ];
    if (typeof code !== 'string' || !allowed.includes(code) || !this.i18n.has(code)) {
      return this.i18n.t('validation.invalid', { field });
    }
    const parameters: TranslationParameters = { field };
    const source = isRecord(entry['parameters']) ? entry['parameters'] : {};
    for (const name of ['max', 'min']) {
      if (typeof source[name] === 'number' && Number.isFinite(source[name])) {
        parameters[name] = this.i18n.number(source[name]);
      }
    }
    const required =
      code === 'validation.minItems' || code === 'validation.minValue'
        ? 'min'
        : code === 'validation.maxLength' || code === 'validation.maxItems'
          ? 'max'
          : null;
    if (required && parameters[required] === undefined)
      return this.i18n.t('validation.invalid', { field });
    if (code === 'validation.minItems') {
      parameters['items'] = this.i18n.count(
        'count.items.one',
        'count.items.other',
        source['min'] as number,
      );
    }
    return this.i18n.t(code, parameters);
  }

  private fieldLabel(path: string): string {
    const fields: Record<string, TranslationKey> = {
      name: 'field.name',
      description: 'field.description',
      servings: 'field.servings',
      preparationMinutes: 'field.preparationMinutes',
      ingredients: 'field.ingredients',
      steps: 'field.steps',
      tags: 'field.tags',
    };
    if (Object.hasOwn(fields, path)) return this.i18n.t(fields[path]);
    const ingredient = /^ingredients\[(\d+)\](?:\.(name|quantity|unit))?$/.exec(path);
    if (ingredient) {
      const labels: Record<string, TranslationKey> = {
        name: 'field.ingredientName',
        quantity: 'field.ingredientQuantity',
        unit: 'field.ingredientUnit',
      };
      return this.i18n.t(labels[ingredient[2]] ?? 'field.ingredient', {
        index: this.i18n.number(Number(ingredient[1]) + 1),
      });
    }
    const indexed = /^(steps|tags)\[(\d+)\]$/.exec(path);
    if (indexed)
      return this.i18n.t(indexed[1] === 'steps' ? 'recipe.step' : 'field.tag', {
        index: this.i18n.number(Number(indexed[2]) + 1),
      });
    return this.i18n.t('field.value');
  }
}
