import { FormControl } from '@angular/forms';
import {
  collectionLimit,
  recipeLimits,
  requiredText,
  textLimit,
  validTags,
} from './recipe-validation';

describe('Recipe form validation', () => {
  it('keeps accented tag names distinct while unifying case variants', () => {
    const base = Array.from({ length: 19 }, (_, index) => `tag${index}`);
    const control = new FormControl([...base, 'rápida', 'RÁPIDA'].join(','), validTags);
    expect(control.valid).toBe(true);
    control.setValue([...base, 'rápida', 'rapida'].join(','));
    expect(control.getError('maxItems').max).toBe(20);
  });
  it('rejects whitespace-only text but accepts trimmed text', () => {
    expect(new FormControl(' \t ', requiredText).hasError('required')).toBe(true);
    expect(new FormControl(' Salt ', requiredText).valid).toBe(true);
  });

  it('accepts the text limits and rejects the next character', () => {
    for (const max of [
      recipeLimits.name,
      recipeLimits.description,
      recipeLimits.ingredientName,
      recipeLimits.step,
    ]) {
      const control = new FormControl('x'.repeat(max), textLimit(max));
      expect(control.valid).toBe(true);
      control.setValue('x'.repeat(max + 1));
      expect(control.getError('maxlength').requiredLength).toBe(max);
    }
  });

  it('bounds ingredients and steps to 100 elements', () => {
    const control = new FormControl(Array(100).fill('x'), collectionLimit(100));
    expect(control.valid).toBe(true);
    control.setValue(Array(101).fill('x'));
    expect(control.getError('maxItems').max).toBe(100);
  });

  it('validates tag text, distinct count and raw count while ignoring blanks and case variants', () => {
    const control = new FormControl('x'.repeat(50), validTags);
    expect(control.valid).toBe(true);
    control.setValue('x'.repeat(51));
    expect(control.getError('localized')[0].parameters.max).toBe(50);
    control.setValue(Array.from({ length: 20 }, (_, index) => `tag${index}`).join(','));
    expect(control.valid).toBe(true);
    control.setValue(control.value + ',another');
    expect(control.getError('maxItems').max).toBe(20);
    control.setValue(Array(100).fill('quick').join(','));
    expect(control.valid).toBe(true);
    control.setValue(Array(101).fill('').join(','));
    expect(control.getError('maxItems').max).toBe(100);
    control.setValue('quick, QUICK, ,');
    expect(control.valid).toBe(true);
  });
});
