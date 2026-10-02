import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgButtonComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-button-showcase',
  imports: [ShowcaseStoryDirective, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Primary"
      notes="The default command. A key cap shows the shortcut and sets aria-keyshortcuts."
    >
      <div class="sc-row">
        <button lgButton hotkey="E">Level up</button>
        <button lgButton>Details</button>
        <a lgButton href="#">As a link</a>
      </div>
    </ng-template>

    <ng-template scStory="Solid" notes="The one main command of a region.">
      <div class="sc-row">
        <button lgButton="solid" hotkey="↵">Enter dungeon</button>
        <button lgButton="solid" icon="inventory">Equip</button>
      </div>
    </ng-template>

    <ng-template
      scStory="Quiet"
      notes="Secondary commands: Cancel, Back, Close."
    >
      <div class="sc-row">
        <button lgButton="quiet">Cancel</button>
        <button lgButton="quiet" icon="settings">Settings</button>
      </div>
    </ng-template>

    <ng-template
      scStory="Danger"
      notes="Commands that consume or remove something."
    >
      <button lgButton="danger">Abandon</button>
    </ng-template>

    <ng-template
      scStory="Link"
      notes="A command set in running text or a panel head."
    >
      <div class="sc-row">
        <button lgButton="link">Manage</button>
        <a lgButton="link" href="#">Open Essences</a>
      </div>
    </ng-template>

    <ng-template scStory="Small" notes="size=sm, with and without an icon.">
      <div class="sc-row">
        <button lgButton size="sm" icon="inventory">Inventory</button>
        <button lgButton="solid" size="sm">Claim</button>
        <button lgButton="quiet" size="sm">Cancel</button>
      </div>
    </ng-template>

    <ng-template
      scStory="Unavailable"
      notes="Blocked with a reason: focusable, aria-disabled; hover, focus or press shows the reason."
    >
      <button lgButton state="unavailable" reason="Not while in a dungeon">
        Sell
      </button>
    </ng-template>

    <ng-template scStory="Locked">
      <button lgButton state="locked" reason="Unlocks at level 20" hotkey="A">
        Enter Arena
      </button>
    </ng-template>

    <ng-template scStory="Restricted">
      <button lgButton state="restricted" reason="Officers only">
        Withdraw
      </button>
    </ng-template>

    <ng-template
      scStory="Insufficient"
      notes="The reason is built from the shortfall."
    >
      <button lgButton="solid" state="insufficient" [shortfall]="shortfall">
        Upgrade
      </button>
    </ng-template>

    <ng-template
      scStory="Cooldown"
      notes="The reason is built from the seconds left."
    >
      <button lgButton state="cooldown" [remaining]="252">Teleport</button>
    </ng-template>

    <ng-template
      scStory="Pending"
      notes="Waiting on the server: the progressive label sits in reserved room, so nothing shifts."
    >
      <div class="sc-row">
        <button lgButton="solid" state="pending" pendingLabel="Claiming…">
          Claim
        </button>
        <button lgButton="solid" pendingLabel="Claiming…">Claim</button>
      </div>
    </ng-template>

    <ng-template
      scStory="Disabled"
      notes="Plain disabled is rare: only when the limit is printed beside the control."
    >
      <button lgButton size="sm" disabled>Previous</button>
    </ng-template>

    <ng-template
      scStory="Densities"
      notes="Comfortable 44px, standard 40px, compact 32px."
    >
      <div class="sc-col">
        @for (d of densities; track d) {
          <div class="sc-cell" [attr.data-density]="d">
            <p class="sc-cap">{{ d }}</p>
            <div class="sc-row">
              <button lgButton="solid" icon="inventory">Equip</button>
              <button lgButton>Sell</button>
              <button lgButton="quiet">Cancel</button>
            </div>
          </div>
        }
      </div>
    </ng-template>
  `,
})
export class ButtonShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly shortfall = [{ amount: 250, name: 'Cinders' }];
  protected readonly densities = [
    'comfortable',
    'standard',
    'compact',
  ] as const;
}

export const BUTTON_SHOWCASE: ShowcaseEntry = {
  slug: 'button',
  name: 'Button',
  tier: 'primitives',
  summary: 'The command button.',
  covers: ['LgButtonComponent'],
  readme: 'src/app/grimoire/primitives/button/README.md',
  component: ButtonShowcaseComponent,
};
