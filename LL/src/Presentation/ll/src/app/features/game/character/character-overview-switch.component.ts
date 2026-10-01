import { Component, inject } from '@angular/core';
import { GrimoirePreviewPreferenceService } from '../../../core/services/client-side/grimoire-preview/grimoire-preview-preference.service';
import { CharacterOverviewGrimoireComponent } from './character-overview-grimoire/character-overview-grimoire.component';
import { CharacterOverviewComponent } from './character-overview/character-overview.component';

/**
 * The Character Overview route: the current page, or the Grimoire one when the player has turned on the new look in
 * Settings → Interface. Goes away once the Grimoire Overview replaces the current one.
 */
@Component({
  selector: 'app-character-overview-switch',
  imports: [CharacterOverviewComponent, CharacterOverviewGrimoireComponent],
  // No box of its own, so either page sits in the frame exactly as the current one always has.
  host: { style: 'display: contents' },
  template: `
    @if (preview.newLook()) {
      <app-character-overview-grimoire />
    } @else {
      <app-character-overview />
    }
  `,
})
export class CharacterOverviewSwitchComponent {
  protected readonly preview = inject(GrimoirePreviewPreferenceService);
}
