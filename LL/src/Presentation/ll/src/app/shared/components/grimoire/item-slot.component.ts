import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_RARITY_CODES, LgRarity, lgCx, lgUniqueId } from './grimoire-core';
import { lgFormatNumber } from './grimoire-format';
import { LgIconComponent } from './icon.component';
import { LG_ICONS, LgIconName } from './grimoire-icons';
import {
  LG_STATES,
  LgShortfall,
  LgStateName,
  lgBlockedReason,
  lgIsBlocked,
  lgReadyWords,
  lgStateWarn,
} from './grimoire-states';
import { LgWhyDirective, LgWhyOptions, lgWhySpoken } from './grimoire-a11y';

/**
 * The item frame: a square for an item, Essence or equipment slot, edged in its rarity, with the rarity code in its
 * corner. States (Standards · States): a blocked slot (locked, unavailable, restricted, insufficient, cooldown) gives
 * its reason; `not-owned` fades the art; `undiscovered` withholds the name and art; any other state with a word
 * (equipped, listed, borrowed…) leads the meta line. Marks keep fixed corners (Standards · State combinations): the
 * rarity code top start; the attention diamond top end (`ready`, or Claimable); the ownership mark bottom start — the
 * in-use square for Equipped and Attuned, else the favourite ribbon (or its word until it is drawn); the quantity
 * bottom end. A blocked or undiscovered slot takes no attention mark.
 */
@Component({
  selector: 'lg-item-slot',
  imports: [LgIconComponent, NgTemplateOutlet, LgWhyDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body>
      <span class="lg-slot__frame"
        >@if (!undiscovered()) {
          @if (image(); as src) {<img [src]="src" alt="" class="lg-slot__img" />} @else if (icon(); as iconName) {<lg-icon
              [name]="iconName"
              [size]="24"
            />}
        }@if (shownRarity(); as r) {<span class="lg-slot__code" [attr.title]="r" aria-hidden="true">{{ codes[r] || r }}</span
          >@if (!interactive()) {<span class="lg-sr">{{ r }}</span>}}@if (attention()) {<span
            class="lg-attention lg-slot__attention"
            aria-hidden="true"
          ></span
          >}@if (inUse()) {<span class="lg-slot__mark lg-slot__mark--inuse" aria-hidden="true"></span
          >}@if (ribbon()) {<span class="lg-slot__mark" aria-hidden="true"
            ><lg-icon [name]="favouriteIcon" [size]="12" /></span
          >@if (!interactive()) {<span class="lg-sr">{{ favouriteWord }}</span>}}@if (readyWord() && !interactive()) {<span
            class="lg-sr"
            >{{ readyWord() }}</span
          >}@if ((quantity() ?? 0) > 1) {<span class="lg-slot__qty"
            >×{{ format(quantity()) }}</span
          >}</span
      >@if (captioned()) {<span class="lg-slot__caption"
          ><span class="lg-slot__name">{{ label() }}</span
          >@if (blocked()) {<span [id]="whyId" [class]="reasonClass()"
              >@if (reasonInfo()?.word) {<span class="lg-slot__word" aria-hidden="true">{{ reasonInfo()?.word }}</span
                ><span class="lg-sr lg-why__desc">{{ reasonInfo()?.word }}. </span>}{{ reasonInfo()?.reason }}</span
            >}@if (metaParts().length) {<span class="lg-slot__meta">{{ metaParts().join(' · ') }}</span>}</span
        >}@if (blocked() && !captioned() && !interactive()) {<span class="lg-sr">{{ srReason() }}</span
        >}@if (why() && !captioned()) {<span class="lg-sr lg-why__desc" [id]="whyId" aria-hidden="true">{{
          spoken(why()!)
        }}</span>}
    </ng-template>

    @if (interactive()) {
      <button
        type="button"
        [class]="classes()"
        [attr.aria-pressed]="blocked() ? null : !!selected()"
        [attr.aria-label]="accessibleLabel()"
        [lgWhy]="why()"
        [lgWhyId]="whyId"
        (click)="blocked() ? null : activate.emit()"
      >
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <div [class]="classes()"><ng-container [ngTemplateOutlet]="body" /></div>
    }
  `,
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
  readonly selected = input(false, { transform: booleanAttribute });
  readonly size = input<'sm' | 'md' | 'wide'>();
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
  /** Renders a toggle button and emits `activate` on a press (React's onClick). */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly codes: Record<string, string> = LG_RARITY_CODES;
  protected readonly format = lgFormatNumber;
  protected readonly spoken = lgWhySpoken;
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
  protected readonly why = computed<LgWhyOptions | null>(() => {
    const br = this.reasonInfo();
    if (!br || !this.interactive()) return null;
    return { reason: br.reason, word: br.word, tone: br.tone, spoken: br.spoken, printed: this.captioned() };
  });
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
}
