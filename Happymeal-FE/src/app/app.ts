import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  template: ` <header>
      <a routerLink="/" class="brand"><span class="brand-icon">H</span> HappyMeal</a
      ><span class="household">Nuestro recetario</span>
    </header>
    <main><router-outlet /></main>
    <footer>Recetas que merece la pena compartir.</footer>`,
})
export class App {}
