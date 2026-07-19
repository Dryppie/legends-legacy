import { Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

export type SidebarLayout = 'detailed' | 'compact';

const SIDEBAR_LAYOUT_STORAGE_KEY = 'sidebarLayout';

@Injectable({ providedIn: 'root' })
export class SidebarLayoutPreferenceService {
  private readonly _layout = signal<SidebarLayout>('detailed');
  readonly layout = this._layout.asReadonly();

  constructor(private readonly storage: LocalStorageService) {
    const storedLayout = this.storage.get<SidebarLayout>(
      SIDEBAR_LAYOUT_STORAGE_KEY,
    );

    if (storedLayout === 'detailed' || storedLayout === 'compact') {
      this._layout.set(storedLayout);
    }
  }

  setLayout(layout: SidebarLayout): void {
    this._layout.set(layout);
    this.storage.set(SIDEBAR_LAYOUT_STORAGE_KEY, layout);
  }
}
