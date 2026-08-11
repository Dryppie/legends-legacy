import { LocalStorageService } from '../local-storage/local-storage.service';
import { ChatLayoutPreferenceService } from './chat-layout-preference.service';

describe('ChatLayoutPreferenceService', () => {
  it('restores a collapsed docked chat', () => {
    const { storage } = createStorage({ dockedChatOpen: false });

    const service = new ChatLayoutPreferenceService(storage);

    expect(service.dockedOpen()).toBeFalse();
  });

  it('persists docked chat changes', () => {
    const { storage, set } = createStorage();
    const service = new ChatLayoutPreferenceService(storage);

    service.setDockedOpen(false);

    expect(service.dockedOpen()).toBeFalse();
    expect(set).toHaveBeenCalledOnceWith('dockedChatOpen', false);
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
        Object.prototype.hasOwnProperty.call(values, key)
          ? (values[key] as T)
          : null,
      set,
      remove: jasmine.createSpy('remove'),
    },
    set,
  };
}
