import { Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

export type ChatLayout = 'docked' | 'floating';

const CHAT_LAYOUT_STORAGE_KEY = 'chatLayout';

@Injectable({ providedIn: 'root' })
export class ChatLayoutPreferenceService {
  private readonly _layout = signal<ChatLayout>('docked');
  readonly layout = this._layout.asReadonly();

  constructor(private readonly storage: LocalStorageService) {
    const storedLayout = this.storage.get<ChatLayout>(CHAT_LAYOUT_STORAGE_KEY);
    if (storedLayout === 'docked' || storedLayout === 'floating') {
      this._layout.set(storedLayout);
    }
  }

  setLayout(layout: ChatLayout): void {
    this._layout.set(layout);
    this.storage.set(CHAT_LAYOUT_STORAGE_KEY, layout);
  }
}
