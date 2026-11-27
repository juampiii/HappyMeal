import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { languageStorageKey } from './i18n/i18n';
describe('App', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());
  it('shows the Spanish recipe-book identity', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nuestro recetario');
  });
  it('switches language using the accessible selector without navigating', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const selector: HTMLSelectElement =
      fixture.nativeElement.querySelector('.language-picker select');
    selector.value = 'en';
    selector.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Our recipe book');
    expect(fixture.nativeElement.textContent).toContain('Language');
    expect(document.documentElement.lang).toBe('en');
    expect(localStorage.getItem(languageStorageKey)).toBe('en');
  });
});
