import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LgRarity, lgCx } from './grimoire-core';
import { LgItemSlotComponent } from './item-slot.component';
import { LgTagComponent } from './tag.component';
import { LgIconName } from './grimoire-icons';
import { LG_STATES, lgReadyWords, lgStateWarn } from './grimoire-states';
import { LgWhyDirective, LgWhyOptions } from './grimoire-a11y';

export type LgLoadoutSlotState = 'attuned' | 'open' | 'locked';

export interface LgLoadoutAbility {
  name: string;
  cooldown?: string;
}

/**
 * An Essence loadout slot, in three states: attuned (a neutral Attuned Tag), open (an empty frame and "Empty") and
 * locked (a dashed frame, a Locked Tag and the unlock condition printed as the name). A locked slot that would be a
 * button stays one, aria-disabled, its printed condition part of its name.
 */
@Component({
  selector: 'lg-loadout-slot',
  imports: [LgItemSlotComponent, LgTagComponent, NgTemplateOutlet, LgWhyDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body>
      <lg-item-slot
        [icon]="currentState() === 'attuned' ? icon() || 'essences' : undefined"
        [image]="currentState() === 'attuned' ? image() : undefined"
        [rarity]="currentState() === 'attuned' ? rarity() : undefined"
        [caption]="false"
        size="sm"
      />
      <div class="lg-loadout__body">
        <div class="lg-loadout__head">
          <span class="lg-loadout__slot">Slot {{ index() != null ? index()! + 1 : '' }}</span>
          @if (currentState() === 'attuned') {
            <lg-tag state="attuned" />
          } @else if (currentState() === 'locked') {
            <lg-tag state="locked" />
          }
          <!-- The Tag, then the attention diamond, at the head's end (Standards · State combinations). -->
          @if (readyWord() && currentState() !== 'locked') {
            <span class="lg-attention lg-loadout__attention" aria-hidden="true"></span>
            <span class="lg-sr">{{ readyWord() }}</span>
          }
        </div>
        <div [class]="nameClass()">{{ nameText() }}</div>
        @if (currentState() === 'attuned' && (active() || passive())) {
          <div class="lg-loadout__abilities">
            @if (active(); as ability) {
              <span><b>Active</b> {{ ability.name }}{{ ability.cooldown ? ' · ' + ability.cooldown : '' }}</span>
            }
            @if (passive(); as ability) {
              <span><b>Passive</b> {{ ability.name }}</span>
            }
          </div>
        }
        @if (currentState() === 'open' && hint()) {
          <div class="lg-loadout__abilities">{{ hint() }}</div>
        }
      </div>
    </ng-template>

    @if (interactive()) {
      <button
        type="button"
        [class]="classes()"
        [lgWhy]="why()"
        (click)="currentState() === 'locked' ? null : activate.emit()"
      >
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <div [class]="classes()"><ng-container [ngTemplateOutlet]="body" /></div>
    }
  `,
})
export class LgLoadoutSlotComponent {
  /** Zero-based slot index. */
  readonly index = input<number>();
  /** Defaults to attuned when there is a name, else open. */
  readonly state = input<LgLoadoutSlotState>();
  readonly name = input<string>();
  readonly rarity = input<LgRarity>();
  readonly active = input<LgLoadoutAbility>();
  readonly passive = input<LgLoadoutAbility>();
  readonly icon = input<LgIconName>();
  readonly image = input<string>();
  /** Locked: how it unlocks ("Unlocks at level 20"). */
  readonly reason = input<string>();
  /** Older name for `reason`. */
  readonly unlockLabel = input<string>();
  /** Open slots: a short prompt. */
  readonly hint = input<string>();
  /** The attention diamond at the end of the head, after the Tag: true, or the words ("Essence ready to attune"). */
  readonly ready = input<boolean | string>();
  /** Renders a button that emits `activate` (React's onClick). */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly readyWord = computed(() => lgReadyWords(this.ready()));
  protected readonly currentState = computed<LgLoadoutSlotState>(() => this.state() || (this.name() ? 'attuned' : 'open'));
  private readonly unlock = computed(() => {
    if (this.currentState() !== 'locked') return '';
    const u = this.reason() || this.unlockLabel() || '';
    if (!u) {
      const i = this.index();
      lgStateWarn('loadout:' + i, `LoadoutSlot ${i != null ? i + 1 : ''} is locked with no reason; say how it unlocks`);
    }
    return u;
  });
  /** The condition is printed inside the button, so it is already in the name ("Slot 3 Locked Unlocks at level 20"). */
  protected readonly why = computed<LgWhyOptions | null>(() =>
    this.currentState() === 'locked' && this.interactive()
      ? { reason: this.unlock(), word: 'Locked', printed: true, describe: false }
      : null,
  );
  protected readonly classes = computed(() => lgCx('lg-loadout', 'is-' + this.currentState()));
  protected readonly nameClass = computed(() => {
    const r = this.rarity();
    return lgCx('lg-loadout__name', r && this.currentState() === 'attuned' && 'lg-itemlink--' + r.toLowerCase());
  });
  protected readonly nameText = computed(() => {
    const s = this.currentState();
    return s === 'attuned' ? this.name() : s === 'open' ? 'Empty' : this.unlock() || LG_STATES.locked.word;
  });
}
