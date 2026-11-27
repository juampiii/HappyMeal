import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { I18n } from './i18n/i18n';
@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  template: ` <header>
      <a routerLink="/" class="brand"><span class="brand-icon">H</span> HappyMeal</a>
      <div class="header-tools">
        <span class="household">{{ i18n.t('app.recipeBook') }}</span>
        <label class="language-picker"
          >{{ i18n.t('app.language') }}
          <select [value]="i18n.language()" (change)="i18n.setLanguage($any($event.target).value)">
            <option value="es" lang="es">{{ i18n.t('language.es') }}</option>
            <option value="en" lang="en">{{ i18n.t('language.en') }}</option>
          </select>
        </label>
      </div>
    </header>
    <main><router-outlet /></main>
    <footer>{{ i18n.t('app.footer') }}</footer>`,
})
export class App {
  readonly i18n = inject(I18n);
}
