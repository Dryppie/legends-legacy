import { Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

export type ChatLayout = 'docked' | 'floating';

const CHAT_LAYOUT_STORAGE_KEY = 'chatLayout';
const DOCKED_CHAT_OPEN_STORAGE_KEY = 'dockedChatOpen';

@Injectable({ providedIn: 'root' })
export class ChatLayoutPreferenceService {
  private readonly _layout = signal<ChatLayout>('docked');
  readonly layout = this._layout.asReadonly();
  private readonly _dockedOpen = signal(true);
  readonly dockedOpen = this._dockedOpen.asReadonly();

  constructor(private readonly storage: LocalStorageService) {
    const storedLayout = this.storage.get<ChatLayout>(CHAT_LAYOUT_STORAGE_KEY);
    if (storedLayout === 'docked' || storedLayout === 'floating') {
      this._layout.set(storedLayout);
    }

    const storedDockedOpen = this.storage.get<boolean>(
      DOCKED_CHAT_OPEN_STORAGE_KEY,
    );
    if (storedDockedOpen !== null) {
      this._dockedOpen.set(storedDockedOpen);
    }
  }

  setLayout(layout: ChatLayout): void {
    this._layout.set(layout);
    this.storage.set(CHAT_LAYOUT_STORAGE_KEY, layout);
  }

  setDockedOpen(open: boolean): void {
    this._dockedOpen.set(open);
    this.storage.set(DOCKED_CHAT_OPEN_STORAGE_KEY, open);
  }
}
