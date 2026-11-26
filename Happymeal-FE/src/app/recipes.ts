import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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
  { code: 'g', label: 'gramos' },
  { code: 'kg', label: 'kilogramos' },
  { code: 'ml', label: 'mililitros' },
  { code: 'l', label: 'litros' },
  { code: 'ud', label: 'unidades' },
  { code: 'cucharadita', label: 'cucharaditas' },
  { code: 'cucharada', label: 'cucharadas' },
];
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
export function errorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse))
    return 'No se ha podido completar la acción. Inténtalo de nuevo.';
  if (error.status === 0)
    return 'No se puede conectar con el recetario. Comprueba la conexión e inténtalo de nuevo.';
  if (error.status === 404) return 'No se ha encontrado la receta.';
  if (error.status === 409)
    return 'El nombre ya existe, la receta ha cambiado o su estado no permite esta acción. Revisa los datos y vuelve a cargarla si es necesario.';
  if (error.status === 400) {
    const fields = Object.keys(error.error?.errors ?? {});
    const labels = fields.map((key) =>
      key.startsWith('ingredients')
        ? 'ingredientes (nombre, cantidad positiva, unidad y duplicados)'
        : key.startsWith('steps')
          ? 'pasos de preparación'
          : key === 'name'
            ? 'nombre'
            : key === 'servings'
              ? 'raciones enteras positivas'
              : key === 'preparationMinutes'
                ? 'tiempo en minutos enteros positivos'
                : 'datos introducidos',
    );
    return 'Revisa: ' + ([...new Set(labels)].join(', ') || 'los datos introducidos') + '.';
  }
  return 'No se han podido guardar los cambios. Tus datos siguen en el formulario; inténtalo de nuevo.';
}
