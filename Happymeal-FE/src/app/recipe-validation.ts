import { ValidatorFn } from '@angular/forms';

export const recipeLimits = {
  name: 200,
  description: 2000,
  ingredientName: 200,
  step: 2000,
  tag: 50,
  ingredients: 100,
  steps: 100,
  tags: 20,
  tagInputs: 100,
} as const;

export const requiredText: ValidatorFn = (control) =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };

export const positiveNumber: ValidatorFn = (control) =>
  control.value == null ||
  control.value === '' ||
  (typeof control.value === 'number' && Number.isFinite(control.value) && control.value > 0)
    ? null
    : { positive: true };

export function textLimit(max: number): ValidatorFn {
  return (control) =>
    typeof control.value === 'string' && control.value.trim().length > max
      ? { maxlength: { requiredLength: max } }
      : null;
}

export function collectionLimit(max: number): ValidatorFn {
  return (control) =>
    Array.isArray(control.value) && control.value.length > max ? { maxItems: { max } } : null;
}

export const validTags: ValidatorFn = (control) => {
  const raw: string[] =
    typeof control.value === 'string' && control.value ? control.value.split(',') : [];
  if (raw.length > recipeLimits.tagInputs) return { maxItems: { max: recipeLimits.tagInputs } };
  const issues = raw.flatMap((name, index) =>
    name.trim().length > recipeLimits.tag
      ? [
          {
            path: `tags[${index}]`,
            code: 'validation.maxLength',
            parameters: { max: recipeLimits.tag },
          },
        ]
      : [],
  );
  const distinct = new Set(raw.map((name) => name.trim().toLowerCase()).filter(Boolean));
  if (distinct.size > recipeLimits.tags) {
    return { localized: issues, maxItems: { max: recipeLimits.tags } };
  }
  return issues.length ? { localized: issues } : null;
};
