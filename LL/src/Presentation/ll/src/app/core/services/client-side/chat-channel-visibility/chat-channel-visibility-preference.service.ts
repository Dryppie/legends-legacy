import { Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

export const CHAT_CHANNEL_VISIBILITY_KEYS = [
  'general',
  'invites',
  'guild',
  'raid',
  'whisper',
  'trade',
  'help',
  'system',
] as const;

export type ChatChannelVisibilityKey =
  (typeof CHAT_CHANNEL_VISIBILITY_KEYS)[number];

const HIDDEN_CHAT_CHANNELS_STORAGE_KEY = 'hiddenChatChannels';

@Injectable({ providedIn: 'root' })
export class ChatChannelVisibilityPreferenceService {
  private readonly hiddenChannels = signal<readonly ChatChannelVisibilityKey[]>(
    [],
  );

  constructor(private readonly storage: LocalStorageService) {
    const stored = this.storage.get<unknown>(HIDDEN_CHAT_CHANNELS_STORAGE_KEY);
    if (Array.isArray(stored)) {
      this.hiddenChannels.set(
        stored.filter(isChatChannelVisibilityKey).filter(onlyUnique),
      );
    }
  }

  isVisible(channel: ChatChannelVisibilityKey): boolean {
    return !this.hiddenChannels().includes(channel);
  }

  setVisible(channel: ChatChannelVisibilityKey, visible: boolean): void {
    const hiddenChannels = new Set(this.hiddenChannels());
    if (visible) {
      hiddenChannels.delete(channel);
    } else {
      hiddenChannels.add(channel);
    }

    const next = CHAT_CHANNEL_VISIBILITY_KEYS.filter((key) =>
      hiddenChannels.has(key),
    );
    this.hiddenChannels.set(next);
    this.storage.set(HIDDEN_CHAT_CHANNELS_STORAGE_KEY, next);
  }
}

function isChatChannelVisibilityKey(
  value: unknown,
): value is ChatChannelVisibilityKey {
  return (
    typeof value === 'string' &&
    (CHAT_CHANNEL_VISIBILITY_KEYS as readonly string[]).includes(value)
  );
}

function onlyUnique<T>(value: T, index: number, values: readonly T[]): boolean {
  return values.indexOf(value) === index;
}
