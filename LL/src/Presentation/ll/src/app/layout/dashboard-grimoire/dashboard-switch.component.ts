import { Component, inject } from '@angular/core';
import { GrimoirePreviewPreferenceService } from '../../core/services/client-side/grimoire-preview/grimoire-preview-preference.service';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { DashboardGrimoireComponent } from './dashboard-grimoire.component';

/**
 * The game's frame: the current one, or the Grimoire shell when the player has turned on Settings → Interface → New
 * look. Goes away once the new look replaces the old.
 */
@Component({
  selector: 'app-dashboard-switch',
  imports: [DashboardComponent, DashboardGrimoireComponent],
  host: { style: 'display: contents' },
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
}
