import { Component, inject } from '@angular/core';
import { GrimoirePreviewPreferenceService } from '../../core/services/client-side/grimoire-preview/grimoire-preview-preference.service';
import { LgAnnouncer } from '@grimoire';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { DashboardGrimoireComponent } from './dashboard-grimoire.component';

/** Ctrl + Space, and only that: no Alt, Shift or Meta, and not a held key repeating. */
export function isNewLookShortcut(e: Pick<KeyboardEvent, 'ctrlKey' | 'altKey' | 'shiftKey' | 'metaKey' | 'code' | 'repeat'>): boolean {
  return e.ctrlKey && !e.altKey && !e.shiftKey && !e.metaKey && !e.repeat && e.code === 'Space';
}

/**
 * The game's frame: the current one, or the Grimoire shell when the player has turned on Settings → Interface → New
 * look. Ctrl + Space flips the same setting from anywhere, for comparing the two while testing. Goes away once the new
 * look replaces the old.
 */
@Component({
  selector: 'app-dashboard-switch',
  imports: [DashboardComponent, DashboardGrimoireComponent],
  host: { style: 'display: contents', '(window:keydown)': 'onKeydown($event)' },
  template: `
    @if (preview.newLook()) {
      <app-dashboard-grimoire />
    } @else {
      <app-dashboard />
    }
  `,
})
export class DashboardSwitchComponent {
  protected readonly preview = inject(GrimoirePreviewPreferenceService);
  private readonly announcer = inject(LgAnnouncer);

  protected onKeydown(e: KeyboardEvent): void {
    if (!isNewLookShortcut(e)) return;
    e.preventDefault();
    const next = !this.preview.newLook();
    this.preview.setNewLook(next);
    this.announcer.announce(next ? 'New look on' : 'New look off', { key: 'new-look' });
  }
}
