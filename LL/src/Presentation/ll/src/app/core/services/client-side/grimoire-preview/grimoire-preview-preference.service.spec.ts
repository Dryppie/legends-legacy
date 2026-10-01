import { LocalStorageService } from '../local-storage/local-storage.service';
import { GrimoirePreviewPreferenceService } from './grimoire-preview-preference.service';

describe('GrimoirePreviewPreferenceService', () => {
  it('keeps the current Character Overview until the preview is turned on', () => {
    const { storage } = createStorage();

    expect(new GrimoirePreviewPreferenceService(storage).characterOverview()).toBeFalse();
  });

  it('restores and persists the Character Overview preview', () => {
    const { storage, set } = createStorage({ 'grimoirePreview.characterOverview': true });
    const service = new GrimoirePreviewPreferenceService(storage);

    expect(service.characterOverview()).toBeTrue();
    service.setCharacterOverview(false);

    expect(service.characterOverview()).toBeFalse();
    expect(set).toHaveBeenCalledOnceWith('grimoirePreview.characterOverview', false);
  });
});

function createStorage(values: Record<string, unknown> = {}): {
  storage: LocalStorageService;
  set: jasmine.Spy;
} {
  const set = jasmine.createSpy('set');
  return {
    storage: {
      get: <T>(key: string): T | null =>
        Object.prototype.hasOwnProperty.call(values, key) ? (values[key] as T) : null,
      set,
      remove: jasmine.createSpy('remove'),
    },
    set,
  };
}
