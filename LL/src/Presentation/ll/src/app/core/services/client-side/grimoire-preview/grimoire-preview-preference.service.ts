import { Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

const CHARACTER_OVERVIEW_STORAGE_KEY = 'grimoirePreview.characterOverview';

/**
 * Screens moving to the Grimoire design system can be tried before they replace the current ones. Each preview is
 * off until the player turns it on in Settings → Interface, and is remembered on this device.
 */
@Injectable({ providedIn: 'root' })
export class GrimoirePreviewPreferenceService {
  private readonly _characterOverview = signal(false);
  /** Show the Grimoire Character Overview instead of the current one. */
  readonly characterOverview = this._characterOverview.asReadonly();

  constructor(private readonly storage: LocalStorageService) {
    if (this.storage.get<boolean>(CHARACTER_OVERVIEW_STORAGE_KEY) === true) {
      this._characterOverview.set(true);
    }
  }

  setCharacterOverview(enabled: boolean): void {
    this._characterOverview.set(enabled);
    this.storage.set(CHARACTER_OVERVIEW_STORAGE_KEY, enabled);
  }
}
