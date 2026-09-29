import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LgRarity } from './grimoire-core';
import { LgIconName } from './grimoire-icons';
import { LgItemSlotComponent } from './item-slot.component';
import { LgTagComponent } from './tag.component';

export type LgLoadoutSlotState = 'attuned' | 'open' | 'locked';

export interface LgLoadoutAbility {
  name: string;
  cooldown?: string;
}

/** One Essence loadout slot: the Essence, its rarity and abilities, or an open/locked slot. */
@Component({
  selector: 'lg-loadout-slot',
  imports: [LgItemSlotComponent, LgTagComponent, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body>
      <lg-item-slot
        size="sm"
        [caption]="false"
        [icon]="state() === 'locked' ? undefined : icon()"
        [image]="state() === 'locked' ? undefined : image()"
        [rarity]="state() === 'attuned' ? rarity() : undefined"
      />
      <div class="lg-loadout__body">
        <div class="lg-loadout__head">
          <span class="lg-loadout__slot">Slot {{ index() + 1 }}</span>
          @switch (state()) {
            @case ('attuned') { <lg-tag tone="new">Attuned</lg-tag> }
            @case ('open') { <lg-tag>Open</lg-tag> }
            @default { <lg-tag>Locked</lg-tag> }
          }
        </div>
        <div class="lg-loadout__name" [class]="nameClass()">
          @switch (state()) {
            @case ('attuned') { {{ name() }} }
            @case ('open') { Empty }
            @default { {{ unlockLabel() ?? 'Locked' }} }
          }
        </div>
        @if (state() === 'attuned' && (active() || passive())) {
          <div class="lg-loadout__abilities">
            @if (active(); as ability) {
              <span><b>Active</b> {{ ability.name }}@if (ability.cooldown) { · {{ ability.cooldown }} }</span>
            }
            @if (passive(); as ability) {
              <span><b>Passive</b> {{ ability.name }}</span>
            }
          </div>
        }
        @if (state() === 'open' && hint()) {
          <div class="lg-loadout__abilities">{{ hint() }}</div>
        }
      </div>
    </ng-template>

    @if (interactive() && state() !== 'locked') {
      <button type="button" [class]="classes()" (click)="activate.emit()">
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <div [class]="classes()"><ng-container [ngTemplateOutlet]="body" /></div>
    }
  `,
})
export class LgLoadoutSlotComponent {
  /** Zero-based slot index. */
  readonly index = input(0);
  readonly state = input<LgLoadoutSlotState>('open');
  readonly name = input<string>();
  readonly rarity = input<LgRarity>();
  readonly active = input<LgLoadoutAbility>();
  readonly passive = input<LgLoadoutAbility>();
  readonly icon = input<LgIconName>('essences');
  readonly image = input<string>();
  /** Locked slots: "Unlocks at level 20". */
  readonly unlockLabel = input<string>();
  /** Open slots: a short prompt. */
  readonly hint = input<string>();
  /** Renders a button (e.g. to open the Essence preview). */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly classes = computed(() => `lg-loadout is-${this.state()}`);
  protected readonly nameClass = computed(() => {
    const rarity = this.rarity();
    return this.state() === 'attuned' && rarity ? `lg-itemlink--${rarity.toLowerCase()}` : '';
  });
}
