import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface Ingredient {
  name: string;
  quantity: number | null;
  unit: string | null;
}
export interface RecipeInput {
  name: string;
  description: string | null;
  ingredients: Ingredient[];
  steps: string[];
  servings: number | null;
  preparationMinutes: number | null;
  tags: string[];
}
export interface Recipe extends RecipeInput {
  id: string;
  isPublished: boolean;
  isHidden: boolean;
  version: number;
}
export const units = [
  { code: 'g', labelKey: 'unit.g' },
  { code: 'kg', labelKey: 'unit.kg' },
  { code: 'ml', labelKey: 'unit.ml' },
  { code: 'l', labelKey: 'unit.l' },
  { code: 'ud', labelKey: 'unit.ud' },
  { code: 'cucharadita', labelKey: 'unit.cucharadita' },
  { code: 'cucharada', labelKey: 'unit.cucharada' },
] as const;
@Injectable({ providedIn: 'root' })
export class Recipes {
  private http = inject(HttpClient);
  list() {
    return firstValueFrom(this.http.get<Recipe[]>('/api/recipes'));
  }
  get(id: string) {
    return firstValueFrom(this.http.get<Recipe>(`/api/recipes/${id}`));
  }
  save(id: string | null, body: RecipeInput) {
    return firstValueFrom(
      id
        ? this.http.put<Recipe>(`/api/recipes/${id}`, body)
        : this.http.post<Recipe>('/api/recipes', body),
    );
  }
  publish(id: string) {
    return firstValueFrom(this.http.post<void>(`/api/recipes/${id}/publish`, {}));
  }
  hide(id: string) {
    return firstValueFrom(this.http.delete<void>(`/api/recipes/${id}`));
  }
}
