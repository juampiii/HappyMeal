import { TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { FieldError } from './field-error';
import { I18n } from './i18n/i18n';

describe('Signal field feedback', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('formats a normal minimum validator without assuming that it means positive', async () => {
    const fixture = TestBed.createComponent(FieldError);
    const control = new FormControl(3, Validators.min(5));
    control.markAsTouched();
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('path', 'servings');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.message()).toContain('5');
    expect(fixture.componentInstance.message()).not.toContain('cero');
    TestBed.inject(I18n).setLanguage('en');
    expect(fixture.componentInstance.message()).toBe('Servings must be at least 5.');
    control.setValue(6);
    expect(fixture.componentInstance.message()).toBe('');
  });

  it('switches event sources when a control input is replaced', async () => {
    const fixture = TestBed.createComponent(FieldError);
    const first = new FormControl('', Validators.required);
    first.markAsTouched();
    fixture.componentRef.setInput('control', first);
    fixture.componentRef.setInput('path', 'name');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.message()).toContain('Completa');
    const replacement = new FormControl('Valid');
    fixture.componentRef.setInput('control', replacement);
    fixture.detectChanges();
    await fixture.whenStable();
    first.setValue('');
    expect(fixture.componentInstance.message()).toBe('');
  });
});
