import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Recipes, Recipe, Ingredient, units, errorMessage } from './recipes';
@Component({ imports: [ReactiveFormsModule, RouterLink], templateUrl: './recipe-page.html' })
export class RecipePage implements OnInit {
  private api = inject(Recipes);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  recipe = signal<Recipe | null>(null);
  loading = signal(false);
  busy = signal(false);
  error = signal('');
  message = signal('');
  editing = signal(false);
  confirmHide = signal(false);
  units = units;
  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(2000)],
    servings: [null as number | null, [Validators.min(1), Validators.pattern(/^\d+$/)]],
    preparationMinutes: [null as number | null, [Validators.min(1), Validators.pattern(/^\d+$/)]],
    tags: [''],
    ingredients: this.fb.array<ReturnType<RecipePage['ingredientGroup']>>([]),
    steps: this.fb.array<ReturnType<FormBuilder['control']>>([]),
  });
  get ingredients() {
    return this.form.controls.ingredients;
  }
  get steps() {
    return this.form.controls.steps;
  }
  ingredientGroup(i?: Ingredient) {
    return this.fb.group({
      name: [i?.name ?? '', Validators.required],
      quantity: [i?.quantity ?? (null as number | null), Validators.min(Number.MIN_VALUE)],
      unit: [i?.unit ?? ''],
    });
  }
  addIngredient() {
    this.ingredients.push(this.ingredientGroup());
  }
  addStep() {
    this.steps.push(this.fb.control('', { validators: Validators.required }));
  }
  moveStep(i: number, offset: number) {
    const step = this.steps.at(i);
    this.steps.removeAt(i);
    this.steps.insert(i + offset, step);
  }
  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        void this.load(id);
      } else {
        this.recipe.set(null);
        this.editing.set(true);
      }
    });
  }
  async load(id: string) {
    this.loading.set(true);
    this.error.set('');
    try {
      const recipe = await this.api.get(id);
      this.recipe.set(recipe);
      this.editing.set(false);
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.loading.set(false);
    }
  }
  edit() {
    const r = this.recipe();
    if (!r || r.isPublished || r.isHidden) return;
    this.form.reset({
      name: r.name,
      description: r.description ?? '',
      servings: r.servings,
      preparationMinutes: r.preparationMinutes,
      tags: r.tags.join(', '),
    });
    this.ingredients.clear();
    r.ingredients.forEach((i) => this.ingredients.push(this.ingredientGroup(i)));
    this.steps.clear();
    r.steps.forEach((s) => this.steps.push(this.fb.control(s, Validators.required)));
    this.error.set('');
    this.editing.set(true);
  }
  async save() {
    this.form.markAllAsTouched();
    if (this.form.invalid || !this.form.controls.name.value?.trim()) {
      this.error.set('Revisa el nombre, los ingredientes y los valores numéricos.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const v = this.form.getRawValue();
    try {
      const r = await this.api.save(this.recipe()?.id ?? null, {
        name: v.name!.trim(),
        description: v.description || null,
        servings: v.servings,
        preparationMinutes: v.preparationMinutes,
        tags: (v.tags ?? '')
          .split(',')
          .map((x) => x.trim())
          .filter(Boolean),
        ingredients: v.ingredients.map((i) => ({
          name: i.name!,
          quantity: i.quantity,
          unit: i.unit || null,
        })),
        steps: v.steps.map((s) => String(s)),
      });
      this.recipe.set(r);
      this.editing.set(false);
      this.message.set('Borrador guardado.');
      await this.router.navigate(['/recipes', r.id]);
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  async publish() {
    const r = this.recipe();
    if (!r) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.api.publish(r.id);
      await this.load(r.id);
      this.message.set('Receta publicada. Su contenido ya no puede modificarse.');
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  async hide() {
    const r = this.recipe();
    if (!r) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.api.hide(r.id);
      await this.router.navigate(['/']);
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  unitLabel(code: string | null) {
    return units.find((u) => u.code === code)?.label ?? '';
  }
}
