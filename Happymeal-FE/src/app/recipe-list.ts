import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Recipes, Recipe, errorMessage } from './recipes';
@Component({
  imports: [RouterLink],
  template: `
    <section class="heading">
      <div>
        <p class="eyebrow">COCINAR EMPIEZA AQUÍ</p>
        <h1>Una buena receta,<br /> siempre a mano.</h1>
        <p>Guarda tus ideas, completa los detalles y publica tus favoritas.</p>
      </div>
      <a class="button" routerLink="/recipes/new">Crear receta</a>
    </section>
    @if (loading()) {
      <p role="status">Cargando el recetario…</p>
    }
    @if (error()) {
      <div role="alert" class="notice error">
        {{ error() }} <button class="quiet" (click)="load()">Reintentar</button>
      </div>
    }
    @if (!loading() && !error()) {
      <div class="section-title">
        <h2>Tus recetas</h2>
        <span>{{ recipes().length }} en el recetario</span>
      </div>
      @if (!recipes().length) {
        <section class="empty">
          <span class="empty-icon">✳</span>
          <h2>Tu próxima favorita empieza aquí</h2>
          <p>Solo necesitas un nombre para guardar tu primer borrador.</p>
          <a routerLink="/recipes/new" class="button">Crear mi primera receta</a>
        </section>
      }
      <div class="recipe-grid">
        @for (recipe of recipes(); track recipe.id) {
          <a class="recipe-card" [routerLink]="['/recipes', recipe.id]"
            ><div class="card-top">
              <span class="badge" [class.published]="recipe.isPublished">{{
                recipe.isPublished ? 'Publicada' : 'Borrador'
              }}</span
              ><span class="recipe-symbol">✳</span>
            </div>
            <h2>{{ recipe.name }}</h2>
            <p>{{ recipe.description || 'Una receta por descubrir.' }}</p>
            <div class="recipe-meta">
              <span>{{ recipe.ingredients.length }} ingredientes</span>
              @if (recipe.preparationMinutes) {
                <span>{{ recipe.preparationMinutes }} min</span>
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
  recipes = signal<Recipe[]>([]);
  loading = signal(true);
  error = signal('');
  ngOnInit() {
    void this.load();
  }
  async load() {
    this.loading.set(true);
    this.error.set('');
    try {
      this.recipes.set(await this.api.list());
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.loading.set(false);
    }
  }
}
