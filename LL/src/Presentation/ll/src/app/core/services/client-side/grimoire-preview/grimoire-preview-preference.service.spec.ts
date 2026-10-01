import { LocalStorageService } from '../local-storage/local-storage.service';
import { GrimoirePreviewPreferenceService } from './grimoire-preview-preference.service';

describe('GrimoirePreviewPreferenceService', () => {
  it('keeps the current look until the new look is turned on', () => {
    const { storage } = createStorage();

    expect(new GrimoirePreviewPreferenceService(storage).newLook()).toBeFalse();
  });

  it('restores and persists the new look', () => {
    const { storage, set } = createStorage({ 'grimoirePreview.newLook': true });
    const service = new GrimoirePreviewPreferenceService(storage);

    expect(service.newLook()).toBeTrue();
    service.setNewLook(false);

    expect(service.newLook()).toBeFalse();
    expect(set).toHaveBeenCalledOnceWith('grimoirePreview.newLook', false);
  });

  it('carries over the earlier Character Overview preview', () => {
    const { storage } = createStorage({ 'grimoirePreview.characterOverview': true });

    expect(new GrimoirePreviewPreferenceService(storage).newLook()).toBeTrue();
  });

  it('prefers the single switch over the earlier one', () => {
    const { storage } = createStorage({ 'grimoirePreview.newLook': false, 'grimoirePreview.characterOverview': true });

    expect(new GrimoirePreviewPreferenceService(storage).newLook()).toBeFalse();
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
