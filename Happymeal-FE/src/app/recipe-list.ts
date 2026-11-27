import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Recipes, Recipe } from './recipes';
import { I18n } from './i18n/i18n';
import { ApiErrors } from './api-errors';
@Component({
  imports: [RouterLink],
  template: `
    <section class="heading">
      <div>
        <p class="eyebrow">{{ i18n.t('list.eyebrow') }}</p>
        <h1>{{ i18n.t('list.title') }}</h1>
        <p>{{ i18n.t('list.intro') }}</p>
      </div>
      <a class="button" routerLink="/recipes/new">{{ i18n.t('list.create') }}</a>
    </section>
    @if (loading()) {
      <p role="status">{{ i18n.t('list.loading') }}</p>
    }
    @if (error()) {
      <div role="alert" class="notice error">
        {{ error() }} <button class="quiet" (click)="load()">{{ i18n.t('action.retry') }}</button>
      </div>
    }
    @if (!loading() && !error()) {
      <div class="section-title">
        <h2>{{ i18n.t('list.recipes') }}</h2>
        <span>{{ i18n.count('list.count.one', 'list.count.other', recipes().length) }}</span>
      </div>
      @if (!recipes().length) {
        <section class="empty">
          <span class="empty-icon">✳</span>
          <h2>{{ i18n.t('list.emptyTitle') }}</h2>
          <p>{{ i18n.t('list.emptyText') }}</p>
          <a routerLink="/recipes/new" class="button">{{ i18n.t('list.firstRecipe') }}</a>
        </section>
      }
      <div class="recipe-grid">
        @for (recipe of recipes(); track recipe.id) {
          <a class="recipe-card" [routerLink]="['/recipes', recipe.id]"
            ><div class="card-top">
              <span class="badge" [class.published]="recipe.isPublished">{{
                i18n.t(recipe.isPublished ? 'recipe.published' : 'recipe.draft')
              }}</span><span class="recipe-symbol">✳</span>
            </div>
            <h2>{{ recipe.name }}</h2>
            <p>{{ recipe.description || i18n.t('list.noDescription') }}</p>
            <div class="recipe-meta">
              <span>{{
                i18n.count(
                  'count.ingredients.one',
                  'count.ingredients.other',
                  recipe.ingredients.length
                )
              }}</span>
              @if (recipe.preparationMinutes) {
                <span>{{
                  i18n.t('count.minutes', { count: i18n.number(recipe.preparationMinutes) })
                }}</span>
              }
            </div></a
          >
        }
      </div>
    }
  `,
})
export class RecipeList implements OnInit {
  private api = inject(Recipes);
  readonly i18n = inject(I18n);
  private apiErrors = inject(ApiErrors);
  private failure = signal<unknown>(null);
  recipes = signal<Recipe[]>([]);
  loading = signal(true);
  error = computed(() => (this.failure() === null ? '' : this.apiErrors.message(this.failure())));
  ngOnInit() {
    void this.load();
  }
  async load() {
    this.loading.set(true);
    this.failure.set(null);
    try {
      this.recipes.set(await this.api.list());
    } catch (e) {
      this.failure.set(e);
    } finally {
      this.loading.set(false);
    }
  }
}
