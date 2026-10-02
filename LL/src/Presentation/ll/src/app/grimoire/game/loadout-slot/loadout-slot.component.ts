import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  inject,
  input,
} from '@angular/core';
import { LgRarity, lgCx } from '../../core/grimoire-core';
import { LgItemSlotComponent } from '../item-slot/item-slot.component';
import { LgTagComponent } from '../../primitives/tag/tag.component';
import { LgIconName } from '../../core/grimoire-icons';
import { LG_STATES, lgReadyWords, lgStateWarn } from '../../core/grimoire-states';
import { LgBlockedController, LgBlockedTip } from '../../core/grimoire-blocked';

export type LgLoadoutSlotState = 'attuned' | 'open' | 'locked';

export interface LgLoadoutAbility {
  name: string;
  cooldown?: string;
}

/**
 * An Essence loadout slot, in three states: attuned (a neutral Attuned Tag), open (an empty frame and "Empty") and
 * locked (a dashed frame, a Locked Tag and the unlock condition printed as the name). Its host is the element:
 * `<button lgLoadoutSlot>` to open or attune it (a press is the native `(click)`), `<a lgLoadoutSlot>` to go to it,
 * `<div lgLoadoutSlot>` to show it. A locked button stays one, aria-disabled, its printed condition part of its name,
 * and your (click) handler does not run. `compact` is one short row for a full loadout (D-103).
 */
@Component({
  selector: 'button[lgLoadoutSlot], a[lgLoadoutSlot], div[lgLoadoutSlot]',
  imports: [LgItemSlotComponent, LgTagComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.type]': "isButton ? 'button' : null",
    '[attr.aria-disabled]': "isControl && currentState() === 'locked' ? 'true' : null",
  },
  template: `
    <div
      lgItemSlot
      class="lg-loadout__frame"
      [icon]="currentState() === 'attuned' ? icon() || 'essences' : undefined"
      [image]="currentState() === 'attuned' ? image() : undefined"
      [rarity]="currentState() === 'attuned' ? rarity() : undefined"
      [caption]="false"
      [size]="compact() ? 'xs' : 'sm'"
    ></div>
    <div class="lg-loadout__body">
      <div class="lg-loadout__head">
        <span [class]="compact() ? 'lg-sr' : 'lg-loadout__slot'">Slot {{ index() != null ? index()! + 1 : '' }}</span>
        <!-- Compact (D-103): the name takes the slot label's place, and only a locked slot keeps its Tag. -->
        @if (compact()) {
          <div [class]="nameClass()">{{ nameText() }}</div>
        }
        @if (currentState() === 'attuned' && !compact()) {
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
      @if (!compact()) {
        <div [class]="nameClass()">{{ nameText() }}</div>
      }
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
  `,
  styleUrl: './loadout-slot.component.css',
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
  /** One short row for a full loadout (D-103): the name leads, the slot number is for screen readers, an attuned slot has no Tag. */
  readonly compact = input(false, { transform: booleanAttribute });

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  /** A button or a link: a locked one stays focusable and says why instead of acting. */
  protected readonly isControl = this.isButton || this.el.tagName === 'A';

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
  protected readonly why = computed<LgBlockedTip | null>(() =>
    this.currentState() === 'locked' && this.isControl
      ? { reason: this.unlock(), word: 'Locked', printed: true, describe: false }
      : null,
  );
  private readonly blocked = new LgBlockedController(this.el, this.why, () => '');
  protected readonly classes = computed(() =>
    lgCx('lg-loadout', 'is-' + this.currentState(), this.compact() && 'lg-loadout--compact'),
  );
  protected readonly nameClass = computed(() => {
    const r = this.rarity();
    return lgCx('lg-loadout__name', r && this.currentState() === 'attuned' && 'lg-loadout__name--' + r.toLowerCase());
  });
  protected readonly nameText = computed(() => {
    const s = this.currentState();
    return s === 'attuned' ? this.name() : s === 'open' ? 'Empty' : this.unlock() || LG_STATES.locked.word;
  });

  constructor() {
    // A locked slot does not act: stop the press before (click) handlers on the element hear it, and say why.
    this.el.addEventListener(
      'click',
      (event) => {
        if (!this.why()) return;
        event.stopImmediatePropagation();
        event.preventDefault();
        this.blocked.press(event);
      },
      true,
    );
  }
}
