import { LocalStorageService } from '../local-storage/local-storage.service';
import { ChatChannelVisibilityPreferenceService } from './chat-channel-visibility-preference.service';

describe('ChatChannelVisibilityPreferenceService', () => {
  it('restores valid hidden channels and ignores stale values', () => {
    const { storage } = createStorage({
      hiddenChatChannels: ['trade', 'unknown', 'trade'],
    });

    const service = new ChatChannelVisibilityPreferenceService(storage);

    expect(service.isVisible('general')).toBeTrue();
    expect(service.isVisible('trade')).toBeFalse();
  });

  it('persists hidden channels in a stable order', () => {
    const { storage, set } = createStorage();
    const service = new ChatChannelVisibilityPreferenceService(storage);

    service.setVisible('system', false);
    service.setVisible('general', false);

    expect(service.isVisible('system')).toBeFalse();
    expect(service.isVisible('general')).toBeFalse();
    expect(set).toHaveBeenCalledWith('hiddenChatChannels', [
      'general',
      'system',
    ]);
  });

  it('removes a channel from storage when it is shown again', () => {
    const { storage, set } = createStorage({
      hiddenChatChannels: ['help', 'system'],
    });
    const service = new ChatChannelVisibilityPreferenceService(storage);

    service.setVisible('help', true);

    expect(service.isVisible('help')).toBeTrue();
    expect(set).toHaveBeenCalledOnceWith('hiddenChatChannels', ['system']);
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
