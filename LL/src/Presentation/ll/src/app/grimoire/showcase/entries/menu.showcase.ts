import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LG_MENU, LgButtonComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-menu-showcase',
  imports: [ShowcaseStoryDirective, ...LG_MENU, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Actions"
      notes="A Level 2 float of rows in body-compact; the item under the pointer or the keys takes the wash. A disabled item stays in view, passed over by the keys. Drawn in place here; a trigger opens it on the overlay."
    >
      <lg-menu label="Ashen Blade">
        <button lgMenuItem>Equip</button>
        <button lgMenuItem>Compare</button>
        <button lgMenuItem disabled>Sell</button>
        <button lgMenuItem>Salvage</button>
      </lg-menu>
    </ng-template>

    <ng-template
      scStory="Settings"
      notes="A checkbox item carries the Checkbox's box: an arcana-glow fill with its tick when checked."
    >
      <lg-menu label="Chat">
        <button lgMenuItemCheckbox [(checked)]="loot">Show loot</button>
        <button lgMenuItemCheckbox [(checked)]="system">
          Show system lines
        </button>
        <button lgMenuItem>Clear loot history</button>
      </lg-menu>
    </ng-template>

    <ng-template
      scStory="Open from a button"
      height="16rem"
      notes="Interactive: a press, Enter, Space or the down arrow opens it with focus on its first item; arrows move, typing finds an item, Enter does it, Escape closes it with focus back on the button."
    >
      <div class="sc-col">
        <button lgButton="quiet" [lgMenuTrigger]="actions">Item actions</button>
        <p class="sc-cap">
          {{ last() ? 'Chose: ' + last() : 'Nothing chosen yet.' }}
        </p>
      </div>
      <ng-template #actions>
        <lg-menu label="Ashen Blade">
          <button lgMenuItem (triggered)="last.set('Equip')">Equip</button>
          <button lgMenuItem (triggered)="last.set('Compare')">Compare</button>
          <button lgMenuItem disabled>Sell</button>
          <button lgMenuItem (triggered)="last.set('Salvage')">Salvage</button>
        </lg-menu>
      </ng-template>
    </ng-template>
  `,
})
export class MenuShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly loot = signal(true);
  protected readonly system = signal(false);
  protected readonly last = signal('');
}

export const MENU_SHOWCASE: ShowcaseEntry = {
  slug: 'menu',
  name: 'Menu',
  tier: 'primitives',
  summary: 'Actions on the thing it opened from.',
  covers: [
    'LgMenuComponent',
    'LgMenuItemComponent',
    'LgMenuItemCheckboxComponent',
    'LgMenuTriggerDirective',
  ],
  readme: 'src/app/grimoire/primitives/menu/README.md',
  component: MenuShowcaseComponent,
};
