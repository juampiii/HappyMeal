import { Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { map, startWith, switchMap } from 'rxjs';
import { ApiErrors } from './api-errors';

@Component({
  selector: 'app-field-error',
  template: `@if (message(); as text) {
    <small class="field-error">{{ text }}</small>
  }`,
})
export class FieldError {
  readonly control = input.required<AbstractControl>();
  readonly path = input.required<string>();
  readonly serverError = input<unknown>(null);
  private errors = inject(ApiErrors);
  private state = toSignal(
    toObservable(this.control).pipe(
      switchMap((control) =>
        control.events.pipe(
          startWith(null),
          map(() => ({
            errors: control.errors,
            visible: control.touched || control.dirty,
          })),
        ),
      ),
    ),
    { initialValue: { errors: null, visible: false } },
  );

  readonly message = computed(() => {
    const messages = this.errors.fieldMessages(this.serverError(), this.path());
    const state = this.state();
    if (state.visible) {
      const local = state.errors ?? {};
      if (local['required']) messages.push(this.rule('validation.required'));
      if (local['maxlength'])
        messages.push(
          this.rule('validation.maxLength', { max: local['maxlength'].requiredLength }),
        );
      if (local['positive']) messages.push(this.rule('validation.positive'));
      else if (local['pattern']) messages.push(this.rule('validation.integer'));
      else if (local['min'])
        messages.push(this.rule('validation.minValue', { min: local['min'].min }));
      if (local['maxItems'])
        messages.push(this.rule('validation.maxItems', { max: local['maxItems'].max }));
      for (const issue of local['localized'] ?? []) {
        messages.push(this.errors.validationMessage(issue.path, issue));
      }
      if (Object.keys(local).length && !messages.length)
        messages.push(this.rule('validation.invalid'));
    }
    return [...new Set(messages)].join(' ');
  });

  private rule(code: string, parameters: Record<string, number> = {}): string {
    return this.errors.validationMessage(this.path(), { code, parameters });
  }
}
