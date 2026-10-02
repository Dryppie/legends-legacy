import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  effect,
  inject,
  input,
} from '@angular/core';
import { LgRarity, lgCx, lgUniqueId } from '../../core/grimoire-core';
import { lgFormatNumber } from '../../core/grimoire-format';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LG_ICONS, LgIconName } from '../../core/grimoire-icons';
import {
  LG_STATES,
  LgShortfall,
  LgStateName,
  lgBlockedReason,
  lgIsBlocked,
  lgReadyWords,
  lgStateWarn,
} from '../../core/grimoire-states';
import { LgBlockedController, LgBlockedTip, lgBlockedSpoken } from '../../core/grimoire-blocked';
import { LgRarityComponent } from '../../primitives/rarity/rarity.component';

/** `xs` is 32px and shows the rarity code alone, centred: at that size the art and the code would overlap. */
export type LgItemSlotSize = 'xs' | 'sm' | 'md' | 'wide';

/**
 * The item frame: a square for an item, Essence or equipment slot, edged in its rarity, with the rarity code in its
 * corner. Its host is the element: `<button lgItemSlot>` to pick or toggle it (a press is the native `(click)`;
 * `selected` sets `aria-pressed`), `<a lgItemSlot>` to open it, `<div lgItemSlot>` to show it.
 *
 * States (Standards · States): a blocked slot (locked, unavailable, restricted, insufficient, cooldown) gives its
 * reason, and as a button stays focusable and does not act — your (click) handler never runs; `not-owned` fades the
 * art; `undiscovered` withholds the name and art; any other state with a word (equipped, listed, borrowed…) leads the
 * meta line. Marks keep fixed corners (Standards · State combinations): the rarity code top start; the attention
 * diamond top end (`ready`, or Claimable); the ownership mark bottom start — the in-use square for Equipped and
 * Attuned, else the favourite ribbon (or its word until it is drawn); the quantity bottom end. A blocked or undiscovered
 * slot takes no attention mark. A button or link needs a `name` or `slotLabel`: it is the control's name.
 */
@Component({
  selector: 'button[lgItemSlot], a[lgItemSlot], div[lgItemSlot]',
  imports: [LgIconComponent, LgRarityComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.type]': "isButton ? 'button' : null",
    '[attr.aria-pressed]': 'isButton && !blocked() ? selected() : null',
    '[attr.aria-label]': 'isControl ? accessibleLabel() : null',
    '[attr.aria-disabled]': "isControl && blocked() ? 'true' : null",
    '[attr.aria-describedby]': 'blockedTip.describedBy()',
    '(mouseenter)': 'blockedTip.enter()',
    '(mouseleave)': 'blockedTip.leave()',
    '(focus)': 'blockedTip.focus()',
    '(blur)': 'blockedTip.blur()',
  },
  template: `
    <span class="lg-slot__frame"
      >@if (!undiscovered() && size() !== 'xs') {
        @if (image(); as src) {<img [src]="src" alt="" class="lg-slot__img" />} @else if (icon(); as iconName) {<lg-icon
            [name]="iconName"
            [size]="24"
          />}
      }@if (shownRarity(); as r) {<span class="lg-slot__code"
          ><lg-rarity [rarity]="r" [tip]="!blocked()" [attr.aria-hidden]="isControl ? 'true' : null" /></span
        >}@if (attention()) {<span class="lg-attention lg-slot__attention" aria-hidden="true"></span
        >}@if (inUse()) {<span class="lg-slot__mark lg-slot__mark--inuse" aria-hidden="true"></span
        >}@if (ribbon()) {<span class="lg-slot__mark" aria-hidden="true"
          ><lg-icon [name]="favouriteIcon" [size]="12" /></span
        >@if (!isControl) {<span class="lg-sr">{{ favouriteWord }}</span>}}@if (readyWord() && !isControl) {<span
          class="lg-sr"
          >{{ readyWord() }}</span
        >}@if ((quantity() ?? 0) > 1) {<span class="lg-slot__qty">×{{ format(quantity()) }}</span>}</span
    >@if (captioned()) {<span class="lg-slot__caption"
        ><span class="lg-slot__name">{{ label() }}</span
        >@if (blocked()) {<span [id]="whyId" [class]="reasonClass()"
            >@if (reasonInfo()?.word) {<span class="lg-slot__word" aria-hidden="true">{{ reasonInfo()?.word }}</span
              ><span class="lg-sr lg-blocked__desc">{{ reasonInfo()?.word }}. </span>}{{ reasonInfo()?.reason }}</span
          >}@if (metaParts().length) {<span class="lg-slot__meta">{{ metaParts().join(' · ') }}</span>}</span
      >}@if (blocked() && !captioned() && !isControl) {<span class="lg-sr">{{ srReason() }}</span
      >}@if (why() && !captioned()) {<span class="lg-sr lg-blocked__desc" [id]="whyId" aria-hidden="true">{{
        spoken(why()!)
      }}</span>}
  `,
  styleUrl: './item-slot.component.css',
})
export class LgItemSlotComponent {
  readonly name = input<string>();
  /** Shown when empty: "Off-hand". */
  readonly slotLabel = input<string>();
  readonly meta = input<string>();
  readonly rarity = input<LgRarity>();
  readonly image = input<string>();
  readonly icon = input<LgIconName>();
  readonly quantity = input<number>();
  /** On a button: pressed (`aria-pressed`) and edged in arcana-glow. */
  readonly selected = input(false, { transform: booleanAttribute });
  readonly size = input<LgItemSlotSize>();
  /** false hides the caption under the frame. */
  readonly caption = input(true, { transform: booleanAttribute });
  readonly state = input<LgStateName>();
  /** Why it is blocked: Locked's unlock condition, or why it is unavailable or restricted. */
  readonly reason = input<string>();
  /** Insufficient: what is missing. */
  readonly shortfall = input<readonly LgShortfall[]>();
  /** Cooldown: seconds left. */
  readonly remaining = input<number>();
  readonly favourite = input(false, { transform: booleanAttribute });
  /** Something waiting for the player: the attention diamond in the top end corner. true, or the words ("Upgrade
   *  available"), which join the accessible name. The claimable state draws it too. */
  readonly ready = input<boolean | string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  /** A button or a link: it takes focus, so its name is its aria-label and a blocked one gives its reason. */
  protected readonly isControl = this.isButton || this.el.tagName === 'A';
  protected readonly format = lgFormatNumber;
  protected readonly spoken = lgBlockedSpoken;
  protected readonly favouriteWord = LG_STATES.favourite.word;
  protected readonly favouriteIcon = 'favourite' as LgIconName;
  protected readonly whyId = lgUniqueId('lgi') + '-why';

  protected readonly stateName = computed(() => {
    const s = this.state();
    if (!s || s === 'available' || s === 'default') return null;
    if (!LG_STATES[s]) lgStateWarn('slot:' + s, `"${s}" is not a state`);
    return s;
  });
  private readonly info = computed(() => (this.stateName() ? LG_STATES[this.stateName()!] ?? null : null));
  protected readonly blocked = computed(() => lgIsBlocked(this.stateName()));
  protected readonly undiscovered = computed(() => this.stateName() === 'undiscovered');
  protected readonly shownRarity = computed(() => (this.undiscovered() ? null : this.rarity() ?? null));
  protected readonly label = computed(() =>
    this.undiscovered() ? LG_STATES.undiscovered.word : this.name() || this.slotLabel(),
  );
  protected readonly captioned = computed(() => !!this.label() && this.caption() !== false);
  protected readonly inUse = computed(() => this.stateName() === 'equipped' || this.stateName() === 'attuned');
  protected readonly ribbon = computed(() => this.favourite() && !this.inUse() && 'favourite' in LG_ICONS);
  protected readonly attention = computed(
    () => !this.blocked() && !this.undiscovered() && (!!this.ready() || this.stateName() === 'claimable'),
  );
  protected readonly readyWord = computed(() =>
    !this.blocked() && !this.undiscovered() ? lgReadyWords(this.ready()) : null,
  );
  protected readonly reasonInfo = computed(() => {
    const s = this.stateName();
    if (!lgIsBlocked(s)) return null;
    return lgBlockedReason(
      s,
      { reason: this.reason(), shortfall: this.shortfall(), remaining: this.remaining() },
      `ItemSlot "${this.label() || ''}"`,
    );
  });
  /** With a caption the reason is printed under the name and is the description; without one it is the reason tip. */
  protected readonly why = computed<LgBlockedTip | null>(() => {
    const br = this.reasonInfo();
    if (!br || !this.isControl) return null;
    return { reason: br.reason, word: br.word, tone: br.tone, spoken: br.spoken, printed: this.captioned() };
  });
  protected readonly blockedTip = new LgBlockedController(this.el, this.why, () => this.whyId);
  private readonly stateWord = computed(() => {
    const info = this.info();
    return info && !this.blocked() && !this.undiscovered() ? info.word : null;
  });
  protected readonly metaParts = computed(() =>
    [this.stateWord(), this.favourite() && !this.ribbon() ? this.favouriteWord : null, this.meta()].filter(
      (x): x is string => !!x,
    ),
  );
  protected readonly reasonClass = computed(() =>
    lgCx('lg-slot__reason', this.reasonInfo()?.tone === 'warning' && 'is-warning'),
  );
  protected readonly srReason = computed(() => {
    const br = this.reasonInfo();
    return br ? (br.word ? br.word + '. ' : '') + br.reason : '';
  });
  protected readonly classes = computed(() => {
    const r = this.shownRarity();
    const hasArt = !this.undiscovered() && (!!this.image() || !!this.icon());
    return lgCx(
      'lg-slot',
      r && 'lg-slot--' + r.toLowerCase(),
      this.selected() && !this.blocked() && 'is-selected',
      !hasArt && 'is-empty',
      this.size() && 'lg-slot--' + this.size(),
      this.blocked() && 'is-blocked',
      this.stateName() && 'is-' + this.stateName(),
    );
  });
  protected readonly accessibleLabel = computed(() =>
    [
      this.label(),
      this.shownRarity(),
      (this.quantity() ?? 0) > 1 ? 'quantity ' + lgFormatNumber(this.quantity()) : null,
      this.stateWord() ? this.stateWord()!.toLowerCase() : null,
      this.readyWord() ? this.readyWord()!.toLowerCase() : null,
      this.favourite() ? 'favourite' : null,
    ]
      .filter(Boolean)
      .join(', '),
  );

  constructor() {
    // A blocked slot does not act: stop the press before (click) handlers on the element hear it, and give the reason.
    this.el.addEventListener(
      'click',
      (event) => {
        if (!this.blocked() || !this.isControl) return;
        event.stopImmediatePropagation();
        event.preventDefault();
        this.blockedTip.press(event);
      },
      true,
    );
    // A control needs a name: without `name` or `slotLabel` it would be an unnamed button (WCAG 4.1.2).
    effect(() => {
      if (this.isControl && !this.label())
        lgStateWarn('slot-name', 'An ItemSlot that is a button or link needs a name or slotLabel: it is its accessible name');
    });
  }
}
