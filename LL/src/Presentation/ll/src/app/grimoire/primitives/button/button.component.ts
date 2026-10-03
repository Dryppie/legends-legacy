import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  inject,
  input,
} from '@angular/core';
import { LgDensity, lgCx, lgUniqueId } from '../../core/grimoire-core';
import { NgTemplateOutlet } from '@angular/common';
import { LgIconComponent } from '../icon/icon.component';
import { LgKeyComponent } from '../key/key.component';
import { LgIconName } from '../../core/grimoire-icons';
import { LG_STATES, LgBlockedState, LgShortfall, lgBlockedReason, lgIsBlocked } from '../../core/grimoire-states';
import { LgBlockedController } from '../../core/grimoire-blocked';

export type LgButtonVariant = 'primary' | 'solid' | 'quiet' | 'danger' | 'link';
export type LgButtonState = 'available' | LgBlockedState | 'pending';

/**
 * The command button, with an optional key cap: `<button lgButton="solid" hotkey="E">Level up</button>`.
 *
 * A Button is available, plain disabled (`disabled`: rare, when the limit is plain beside it), blocked with a reason
 * (`state` unavailable, locked, restricted, insufficient or cooldown, with `reason`, `shortfall` or `remaining`) or
 * pending (Standards · States). A blocked Button stays focusable (aria-disabled), shows its reason tip on hover and
 * focus, and answers a press with the reason; your (click) handler does not run while it is blocked or pending.
 */
@Component({
  selector: 'button[lgButton], a[lgButton]',
  imports: [LgIconComponent, LgKeyComponent, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    '[attr.type]': "isButton ? type() : null",
    '[attr.disabled]': "isButton && disabled() && !blocked() && !pending() ? '' : null",
    '[attr.data-density]': 'density() ?? null',
    '[attr.aria-keyshortcuts]': 'hotkey() || null',
    '[attr.aria-busy]': "pending() ? 'true' : null",
    '[attr.aria-disabled]': "blocked() || pending() ? 'true' : null",
    '[attr.aria-describedby]': 'blockedTip.describedBy()',
    '(mouseenter)': 'blockedTip.enter()',
    '(mouseleave)': 'blockedTip.leave()',
    '(focus)': 'blockedTip.focus()',
    '(blur)': 'blockedTip.blur()',
  },
  template: `
    @if (icon(); as iconName) {
      <lg-icon [name]="iconName" [size]="size() === 'sm' ? 16 : 20" />
    }
    <ng-template #content><ng-content /></ng-template>
    @if (stacked()) {
      <span class="lg-btn__labels"
        ><span class="lg-btn__label" [class.is-off]="pending()" [attr.aria-hidden]="pending() ? 'true' : null"
          ><ng-container [ngTemplateOutlet]="content" /></span
        ><span class="lg-btn__label" [class.is-off]="!pending()" [attr.aria-hidden]="pending() ? null : 'true'">{{
          pendingLabel() || pendingWord
        }}</span></span
      >
    } @else {
      <span class="lg-btn__label"><ng-container [ngTemplateOutlet]="content" /></span>
    }
    @if (hotkey()) {
      <kbd lgKey>{{ hotkey() }}</kbd>
    }
    @if (blocked()) {
      <span class="lg-sr lg-blocked__desc" [id]="blockedId" aria-hidden="true">{{ blockedTip.spoken() }}</span>
    }
  `,
})
export class LgButtonComponent {
  readonly variant = input<LgButtonVariant, LgButtonVariant | ''>('primary', {
    alias: 'lgButton',
    transform: (value: LgButtonVariant | '') => value || 'primary',
  });
  readonly size = input<'md' | 'sm'>();
  readonly density = input<LgDensity>();
  /** Shows a key cap and sets aria-keyshortcuts. Wire the key yourself. */
  readonly hotkey = input<string>();
  readonly icon = input<LgIconName>();
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly state = input<LgButtonState>('available');
  /** Why it is blocked: Locked's unlock condition, or why it is unavailable or restricted. */
  readonly reason = input<string>();
  /** Insufficient: what is missing; the reason is built from it. */
  readonly shortfall = input<readonly LgShortfall[]>();
  /** Cooldown: seconds left; the reason is built from it. */
  readonly remaining = input<number>();
  /** The pending label ("Claiming…"); with it the Button keeps room for both labels, so nothing shifts. */
  readonly pendingLabel = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  protected readonly pendingWord = LG_STATES.pending.word;
  protected readonly blockedId = lgUniqueId('lgb') + '-why';

  protected readonly stateName = computed(() => (this.state() && this.state() !== 'available' ? this.state() : null));
  protected readonly blocked = computed(() => lgIsBlocked(this.stateName()));
  protected readonly pending = computed(() => this.stateName() === 'pending');
  protected readonly stacked = computed(() => this.pending() || !!this.pendingLabel());

  protected readonly blockedTip = new LgBlockedController(
    this.el,
    () => {
      const s = this.stateName();
      if (!lgIsBlocked(s)) return null;
      const label = (this.el.querySelector('.lg-btn__label')?.textContent || '').trim();
      const br = lgBlockedReason(
        s,
        { reason: this.reason(), shortfall: this.shortfall(), remaining: this.remaining() },
        `Button "${label}"`,
      );
      return { reason: br.reason, word: br.word, tone: br.tone, spoken: br.spoken };
    },
    () => this.blockedId,
  );

  protected readonly hostClass = computed(() =>
    lgCx(
      'lg-btn',
      'lg-btn--' + this.variant(),
      this.size() === 'sm' && 'lg-btn--sm',
      this.size() === 'md' && 'lg-btn--md',
      this.blocked() && 'is-blocked',
      this.stateName() && 'is-' + this.stateName(),
    ),
  );

  constructor() {
    // A blocked or pending Button does not act: stop the press before (click) handlers on the element hear it.
    this.el.addEventListener(
      'click',
      (event) => {
        if (!this.blocked() && !this.pending()) return;
        event.stopImmediatePropagation();
        event.preventDefault();
        if (this.blocked()) this.blockedTip.press(event);
      },
      true,
    );
  }
}
