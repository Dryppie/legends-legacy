import { Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

const NEW_LOOK_STORAGE_KEY = 'grimoirePreview.newLook';
/** The first preview's key, from when each screen had its own switch; read once so that choice carries over. */
const LEGACY_CHARACTER_OVERVIEW_KEY = 'grimoirePreview.characterOverview';

/**
 * The game is moving to the Grimoire design system one screen at a time. One switch in Settings → Interface shows
 * every screen that has moved in its new look; screens that haven't moved keep the current one. Off until the player
 * turns it on, and remembered on this device. Goes away once the new look replaces the old.
 */
@Injectable({ providedIn: 'root' })
export class GrimoirePreviewPreferenceService {
  private readonly _newLook = signal(false);
  /** Show the new look wherever a screen has one. */
  readonly newLook = this._newLook.asReadonly();

  constructor(private readonly storage: LocalStorageService) {
    const saved = this.storage.get<boolean>(NEW_LOOK_STORAGE_KEY) ?? this.storage.get<boolean>(LEGACY_CHARACTER_OVERVIEW_KEY);
    if (saved === true) this._newLook.set(true);
  }

  setNewLook(enabled: boolean): void {
    this._newLook.set(enabled);
    this.storage.set(NEW_LOOK_STORAGE_KEY, enabled);
  }
}
