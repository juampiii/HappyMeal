import { Component, computed, DestroyRef, inject, signal, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  ValueChangeEvent,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Recipes, Recipe, Ingredient, units } from './recipes';
import { I18n } from './i18n/i18n';
import { TranslationKey } from './i18n/messages';
import { ApiErrors } from './api-errors';
import { FieldError } from './field-error';
import {
  recipeLimits,
  requiredText,
  textLimit,
  collectionLimit,
  validTags,
  positiveNumber,
} from './recipe-validation';
@Component({
  imports: [ReactiveFormsModule, RouterLink, FieldError],
  templateUrl: './recipe-page.html',
})
export class RecipePage implements OnInit {
  private api = inject(Recipes);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private createdRecipe = (
    this.router.currentNavigation()?.extras.info as { createdRecipe?: Recipe } | undefined
  )?.createdRecipe;
  private fb = inject(FormBuilder);
  private destroyRef = inject(DestroyRef);
  private currentId = signal<string | null>(null);
  private loadGeneration = 0;
  readonly i18n = inject(I18n);
  private apiErrors = inject(ApiErrors);
  private failure = signal<unknown>(null);
  readonly serverError = this.failure.asReadonly();
  private localError = signal<TranslationKey | null>(null);
  private messageKey = signal<TranslationKey | null>(null);
  recipe = signal<Recipe | null>(null);
  loading = signal(false);
  loadFailed = signal(false);
  canRetryLoad = computed(() => this.loadFailed() && this.apiErrors.isRetryable(this.failure()));
  busy = signal(false);
  error = computed(() => {
    const key = this.localError();
    return key
      ? this.i18n.t(key)
      : this.failure() === null
        ? ''
        : this.editing() && this.apiErrors.hasFieldErrors(this.failure())
          ? this.i18n.t('error.formInvalid')
          : this.apiErrors.message(this.failure());
  });
  message = computed(() => {
    const key = this.messageKey();
    return key ? this.i18n.t(key) : '';
  });
  editing = signal(false);
  confirmHide = signal(false);
  units = units;
  readonly limits = recipeLimits;
  form = this.fb.group({
    name: ['', [requiredText, textLimit(recipeLimits.name)]],
    description: ['', textLimit(recipeLimits.description)],
    servings: [null as number | null, [positiveNumber, Validators.pattern(/^\d+$/)]],
    preparationMinutes: [null as number | null, [positiveNumber, Validators.pattern(/^\d+$/)]],
    tags: ['', validTags],
    ingredients: this.fb.array<ReturnType<RecipePage['ingredientGroup']>>(
      [],
      collectionLimit(recipeLimits.ingredients),
    ),
    steps: this.fb.array<ReturnType<FormBuilder['control']>>(
      [],
      collectionLimit(recipeLimits.steps),
    ),
  });
  get ingredients() {
    return this.form.controls.ingredients;
  }
  get steps() {
    return this.form.controls.steps;
  }
  ingredientGroup(i?: Ingredient) {
    return this.fb.group({
      name: [i?.name ?? '', [requiredText, textLimit(recipeLimits.ingredientName)]],
      quantity: [i?.quantity ?? (null as number | null), positiveNumber],
      unit: [i?.unit ?? ''],
    });
  }
  addIngredient() {
    if (this.ingredients.length >= recipeLimits.ingredients) return;
    this.ingredients.push(this.ingredientGroup());
  }
  addStep() {
    if (this.steps.length >= recipeLimits.steps) return;
    this.steps.push(
      this.fb.control('', { validators: [requiredText, textLimit(recipeLimits.step)] }),
    );
  }
  moveStep(i: number, offset: number) {
    const step = this.steps.at(i);
    this.steps.removeAt(i);
    this.steps.insert(i + offset, step);
  }
  ngOnInit() {
    this.destroyRef.onDestroy(() => this.loadGeneration++);
    this.form.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (!(event instanceof ValueChangeEvent)) return;
      const path = this.controlPath(event.source);
      if (path !== null) this.failure.set(this.apiErrors.clearField(this.failure(), path));
      if (this.form.valid) this.localError.set(null);
      this.messageKey.set(null);
    });
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');
      this.loadGeneration++;
      this.loading.set(false);
      this.currentId.set(id);
      this.recipe.set(null);
      this.editing.set(false);
      this.clearFeedback();
      if (id) {
        const createdRecipe = this.createdRecipe;
        this.createdRecipe = undefined;
        if (createdRecipe?.id === id) {
          this.recipe.set(createdRecipe);
          this.messageKey.set('recipe.saved');
        } else {
          void this.load(id);
        }
      } else {
        this.recipe.set(null);
        this.editing.set(true);
      }
    });
  }
  retryLoad() {
    const id = this.currentId();
    if (id) void this.load(id);
  }
  async load(id: string) {
    const generation = ++this.loadGeneration;
    this.loading.set(true);
    this.clearFeedback();
    try {
      const recipe = await this.api.get(id);
      if (generation !== this.loadGeneration || this.currentId() !== id) return;
      this.recipe.set(recipe);
      this.editing.set(false);
    } catch (e) {
      if (generation !== this.loadGeneration || this.currentId() !== id) return;
      this.loadFailed.set(true);
      this.failure.set(e);
    } finally {
      if (generation === this.loadGeneration) this.loading.set(false);
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
    r.steps.forEach((s) =>
      this.steps.push(this.fb.control(s, [requiredText, textLimit(recipeLimits.step)])),
    );
    this.clearFeedback();
    this.editing.set(true);
  }
  async save() {
    this.clearFeedback();
    this.form.markAllAsTouched();
    if (this.form.invalid || !this.form.controls.name.value?.trim()) {
      this.failure.set(null);
      this.localError.set('error.formInvalid');
      return;
    }
    this.busy.set(true);
    this.clearFeedback();
    const v = this.form.getRawValue();
    const creating = this.recipe() === null;
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
      this.messageKey.set('recipe.saved');
      if (creating) await this.router.navigate(['/recipes', r.id], { info: { createdRecipe: r } });
    } catch (e) {
      this.failure.set(e);
    } finally {
      this.busy.set(false);
    }
  }
  async publish() {
    const r = this.recipe();
    if (!r) return;
    this.busy.set(true);
    this.clearFeedback();
    try {
      await this.api.publish(r.id);
      this.recipe.set({ ...r, isPublished: true });
      this.messageKey.set('recipe.publishedFeedback');
    } catch (e) {
      this.failure.set(e);
    } finally {
      this.busy.set(false);
    }
  }
  async hide() {
    const r = this.recipe();
    if (!r) return;
    this.busy.set(true);
    this.clearFeedback(false);
    try {
      await this.api.hide(r.id);
      this.recipe.set({ ...r, isHidden: true });
      this.confirmHide.set(false);
      await this.router.navigate(['/']);
    } catch (e) {
      this.failure.set(e);
    } finally {
      this.busy.set(false);
    }
  }
  cancelEdit() {
    this.clearFeedback();
    this.editing.set(false);
  }

  askToHide() {
    this.clearFeedback();
    this.confirmHide.set(true);
  }

  cancelHide() {
    this.clearFeedback();
  }

  unitLabel(code: string | null, quantity: number | null = null) {
    const unit = units.find((u) => u.code === code);
    if (!unit) return '';
    const singularKey = `${unit.labelKey}.one`;
    return this.i18n.t(quantity === 1 && this.i18n.has(singularKey) ? singularKey : unit.labelKey);
  }

  ingredientAmount(ingredient: Ingredient): string {
    return [
      this.i18n.number(ingredient.quantity),
      this.unitLabel(ingredient.unit, ingredient.quantity),
    ]
      .filter(Boolean)
      .join(' ');
  }

  fieldInvalid(control: AbstractControl, path: string): boolean {
    return (
      (control.invalid && (control.touched || control.dirty)) ||
      this.apiErrors.fieldMessages(this.failure(), path).length > 0
    );
  }

  private controlPath(control: AbstractControl): string | null {
    const segments: (string | number)[] = [];
    while (control !== this.form) {
      const parent = control.parent;
      if (!parent) return null;
      const key =
        parent instanceof FormArray
          ? parent.controls.indexOf(control)
          : Object.keys(parent.controls).find((key) => parent.controls[key] === control);
      if (key === undefined || key === -1) return null;
      segments.unshift(key);
      control = parent;
    }
    return segments.reduce<string>(
      (path, segment) =>
        typeof segment === 'number' ? `${path}[${segment}]` : path ? `${path}.${segment}` : segment,
      '',
    );
  }

  private clearFeedback(closeConfirmation = true) {
    this.loadFailed.set(false);
    this.failure.set(null);
    this.localError.set(null);
    this.messageKey.set(null);
    if (closeConfirmation) this.confirmHide.set(false);
  }
}
