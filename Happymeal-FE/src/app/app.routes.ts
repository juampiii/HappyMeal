import { Routes } from '@angular/router';
import { RecipeList } from './recipe-list';
import { RecipePage } from './recipe-page';
export const routes: Routes = [
  { path: '', component: RecipeList },
  { path: 'recipes/new', component: RecipePage },
  { path: 'recipes/:id', component: RecipePage },
  { path: '**', redirectTo: '' },
];
