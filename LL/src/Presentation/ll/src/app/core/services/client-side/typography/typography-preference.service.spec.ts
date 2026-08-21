import { LocalStorageService } from '../local-storage/local-storage.service';
import { TypographyPreferenceService } from './typography-preference.service';

describe('TypographyPreferenceService', () => {
  it('restores and applies a valid stored font', () => {
    const { storage, document } = createDependencies({
      readingFont: 'readable',
    });

    const service = new TypographyPreferenceService(storage, document);

    expect(service.readingFont()).toBe('readable');
    expect(document.documentElement.dataset['readingFont']).toBe('readable');
  });

  it('falls back to the default for an invalid stored font', () => {
    const { storage, document } = createDependencies({
      readingFont: 'unknown',
      readingFontSize: 'huge',
    });
    document.documentElement.dataset['readingFont'] = 'system';
    document.documentElement.dataset['readingFontSize'] = 'large';

    const service = new TypographyPreferenceService(storage, document);

    expect(service.readingFont()).toBe('default');
    expect(
      document.documentElement.hasAttribute('data-reading-font'),
    ).toBeFalse();
    expect(service.readingFontSize()).toBe('default');
    expect(
      document.documentElement.hasAttribute('data-reading-font-size'),
    ).toBeFalse();
  });

  it('persists and applies font changes', () => {
    const { storage, set, document } = createDependencies();
    const service = new TypographyPreferenceService(storage, document);

    service.setReadingFont('system');

    expect(service.readingFont()).toBe('system');
    expect(set).toHaveBeenCalledOnceWith('readingFont', 'system');
    expect(document.documentElement.dataset['readingFont']).toBe('system');
  });

  it('resets to the default and removes the stored preference', () => {
    const { storage, remove, document } = createDependencies({
      readingFont: 'readable',
    });
    const service = new TypographyPreferenceService(storage, document);

    service.resetReadingFont();

    expect(service.readingFont()).toBe('default');
    expect(remove).toHaveBeenCalledOnceWith('readingFont');
    expect(
      document.documentElement.hasAttribute('data-reading-font'),
    ).toBeFalse();
  });

  it('restores and applies a valid stored font size', () => {
    const { storage, document } = createDependencies({
      readingFontSize: 'extra-large',
    });

    const service = new TypographyPreferenceService(storage, document);

    expect(service.readingFontSize()).toBe('extra-large');
    expect(document.documentElement.dataset['readingFontSize']).toBe(
      'extra-large',
    );
  });

  it('persists and applies font size changes', () => {
    const { storage, set, document } = createDependencies();
    const service = new TypographyPreferenceService(storage, document);

    service.setReadingFontSize('large');

    expect(service.readingFontSize()).toBe('large');
    expect(set).toHaveBeenCalledOnceWith('readingFontSize', 'large');
    expect(document.documentElement.dataset['readingFontSize']).toBe('large');
  });

  it('resets all reading preferences together', () => {
    const { storage, remove, document } = createDependencies({
      readingFont: 'system',
      readingFontSize: 'large',
    });
    const service = new TypographyPreferenceService(storage, document);

    service.resetReadingPreferences();

    expect(service.readingFont()).toBe('default');
    expect(service.readingFontSize()).toBe('default');
    expect(remove).toHaveBeenCalledWith('readingFont');
    expect(remove).toHaveBeenCalledWith('readingFontSize');
    expect(remove).toHaveBeenCalledTimes(2);
    expect(
      document.documentElement.hasAttribute('data-reading-font'),
    ).toBeFalse();
    expect(
      document.documentElement.hasAttribute('data-reading-font-size'),
    ).toBeFalse();
  });
});

function createDependencies(values: Record<string, unknown> = {}): {
  storage: LocalStorageService;
  set: jasmine.Spy;
  remove: jasmine.Spy;
  document: Document;
} {
  const set = jasmine.createSpy('set');
  const remove = jasmine.createSpy('remove');

  return {
    storage: {
      get: <T>(key: string): T | null =>
        Object.prototype.hasOwnProperty.call(values, key)
          ? (values[key] as T)
          : null,
      set,
      remove,
    },
    set,
    remove,
    document: document.implementation.createHTMLDocument(),
  };
}
