import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input } from '@angular/core';
import { LG_HEX_INNER, LG_HEX_OUTER, lgCx, lgUniqueId } from '../../core/grimoire-core';
import { LgBlockedController, LgBlockedTip, lgBlockedSpoken } from '../../core/grimoire-blocked';
import { lgBlockedReason } from '../../core/grimoire-states';

export type LgSigilState = 'default' | 'selected' | 'ready' | 'locked';
export type LgSigilLabelPosition = 'right' | 'left' | 'top' | 'bottom' | 'none';
export type LgSigilSize = 'sm' | 'md' | 'lg';

/**
 * The hex stat badge: a short value in a verdigris hexagon with its name beside it. Its host is the element:
 * `<button lgSigil>` is a toggle (`state="selected"` sets `aria-pressed`; a press is the native `(click)`),
 * `<div lgSigil>` shows it. A locked button stays focusable, shows its unlock condition (`reason`) and does not act —
 * your (click) handler never runs. For a tooltip, add `lgTooltip` to the host.
 */
@Component({
  selector: 'button[lgSigil], div[lgSigil]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.type]': "isButton ? 'button' : null",
    '[attr.aria-pressed]': "isButton && !why() ? state() === 'selected' : null",
    '[attr.aria-label]': 'srText()',
    '[attr.aria-disabled]': "why() ? 'true' : null",
    '[attr.aria-describedby]': 'blocked.describedBy()',
    '(mouseenter)': 'blocked.enter()',
    '(mouseleave)': 'blocked.leave()',
    '(focus)': 'blocked.focus()',
    '(blur)': 'blocked.blur()',
  },
  template: `@if (showLabel() && labelFirst()) {<span class="lg-sigil__label">{{ label() }}</span>}<span class="lg-sigil__badge"
      ><svg class="lg-sigil__hex" viewBox="0 0 100 100" aria-hidden="true" focusable="false"
        ><polygon class="lg-sigil__outer" [attr.points]="hexOuter" /><polygon
          class="lg-sigil__inner"
          [attr.points]="hexInner" /></svg
      ><span class="lg-sigil__value">{{ value() }}</span></span
    >@if (showLabel() && !labelFirst()) {<span class="lg-sigil__label">{{ label() }}</span>}@if (why(); as w) {<span
        class="lg-sr lg-blocked__desc"
        [id]="whyId"
        aria-hidden="true"
        >{{ spoken(w) }}</span
      >}`,
  styleUrl: './sigil.component.css',
})
export class LgSigilComponent {
  readonly value = input.required<string | number>();
  readonly label = input<string>();
  readonly labelPosition = input<LgSigilLabelPosition>();
  readonly size = input<LgSigilSize>('md');
  readonly state = input<LgSigilState>('default');
  /** Locked: how it unlocks. */
  readonly reason = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  protected readonly hexOuter = LG_HEX_OUTER;
  protected readonly hexInner = LG_HEX_INNER;
  protected readonly whyId = lgUniqueId('lgs') + '-why';
  protected readonly spoken = lgBlockedSpoken;

  protected readonly position = computed<LgSigilLabelPosition>(
    () => this.labelPosition() || (this.label() ? 'right' : 'none'),
  );
  protected readonly showLabel = computed(() => !!this.label() && this.position() !== 'none');
  protected readonly labelFirst = computed(() => this.position() === 'left' || this.position() === 'top');
  /** A locked Sigil that would be a button stays one: focusable, aria-disabled, with its unlock condition. */
  protected readonly why = computed<LgBlockedTip | null>(() =>
    this.isButton && this.state() === 'locked'
      ? {
          reason: lgBlockedReason('locked', { reason: this.reason() }, `Sigil "${this.label() || ''}"`).reason,
          word: 'Locked',
        }
      : null,
  );
  protected readonly classes = computed(() =>
    lgCx(
      'lg-sigil',
      'lg-sigil--' + this.size(),
      'lg-sigil--label-' + this.position(),
      this.state() !== 'default' && 'is-' + this.state(),
    ),
  );
  protected readonly blocked = new LgBlockedController(this.el, this.why, () => this.whyId);
  /** The lock is in the description when there is one, so the name doesn't say it twice. */
  protected readonly srText = computed(
    () =>
      (this.label() ? this.label() + ' ' : '') +
      this.value() +
      (this.state() === 'locked' && !this.why() ? ', locked' : this.state() === 'ready' ? ', can be raised' : ''),
  );

  constructor() {
    // A locked Sigil does not act: stop the press before (click) handlers on the element hear it, and say why.
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
