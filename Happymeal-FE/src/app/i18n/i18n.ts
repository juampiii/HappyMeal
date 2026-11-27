import { DOCUMENT, Injectable, inject, signal } from '@angular/core';
import { en, es, TranslationKey } from './messages';

export type Language = 'es' | 'en';
export type TranslationParameters = Record<string, string | number>;
export const languageStorageKey = 'happymeal.language';

@Injectable({ providedIn: 'root' })
export class I18n {
  private document = inject(DOCUMENT);
  private selectedLanguage = signal<Language>(this.readPreference());
  readonly language = this.selectedLanguage.asReadonly();

  constructor() {
    this.updateDocument();
  }

  setLanguage(language: string) {
    if (language !== 'es' && language !== 'en') return;
    this.selectedLanguage.set(language);
    this.updateDocument();
    try {
      this.document.defaultView?.localStorage.setItem(languageStorageKey, language);
    } catch {
      // Private browsing or blocked storage must not prevent a session language change.
    }
  }

  has(key: string): key is TranslationKey {
    return Object.hasOwn(es, key);
  }

  t(key: TranslationKey, parameters: TranslationParameters = {}): string {
    const catalog = this.language() === 'es' ? es : en;
    return catalog[key].replace(/\{(\w+)\}/g, (placeholder, name: string) =>
      String(parameters[name] ?? placeholder),
    );
  }

  count(one: TranslationKey, other: TranslationKey, count: number): string {
    return this.t(count === 1 ? one : other, { count: this.number(count) });
  }

  number(value: number | null | undefined): string {
    return value == null
      ? ''
      : new Intl.NumberFormat(this.locale(), { maximumFractionDigits: 20 }).format(value);
  }

  date(value: Date | string | number, options?: Intl.DateTimeFormatOptions): string {
    return new Intl.DateTimeFormat(this.locale(), options).format(new Date(value));
  }

  private locale(): string {
    return this.language() === 'es' ? 'es-ES' : 'en-GB';
  }

  private readPreference(): Language {
    try {
      return this.document.defaultView?.localStorage.getItem(languageStorageKey) === 'en'
        ? 'en'
        : 'es';
    } catch {
      return 'es';
    }
  }

  private updateDocument() {
    this.document.documentElement.lang = this.language();
    this.document.title = this.t('app.title');
  }
}
