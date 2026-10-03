import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  inject,
  input,
} from '@angular/core';
import { lgCx, lgUniqueId } from '../../core/grimoire-core';
import { LgIconName } from '../../core/grimoire-icons';
import { LgBlockedState, lgBlockedReason, lgIsBlocked } from '../../core/grimoire-states';
import { LgBlockedController } from '../../core/grimoire-blocked';
import { LgIconComponent } from '../icon/icon.component';
import { LgTooltipController } from '../tooltip/tooltip.directive';

export type LgIconButtonVariant = 'quiet' | 'framed';

/**
 * A common action shown by its icon alone (Foundations · Iconography · Icons and labels): close, back, expand,
 * menu, search, filter, sort, refresh, copy, link, the drag grip. `<button lgIconButton="close" label="Close"></button>`.
 *
 * `label` is required: it is the button's accessible name and its tooltip, the action in words ("Close", not "Cross").
 * Anything that commits or changes state keeps its word: use a Button. The host is a rectangle at `radius-control`, as
 * tall as the controls beside it (`--lg-control`), never a circle (Foundations · Shape). `pressed` makes it a toggle
 * (`aria-pressed`). Blocked with `state` and `reason`, it stays focusable and answers a press with the reason; your
 * (click) handler does not run.
 */
@Component({
  selector: 'button[lgIconButton], a[lgIconButton]',
  imports: [LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    '[attr.type]': "isButton ? type() : null",
    '[attr.aria-label]': 'label()',
    '[attr.disabled]': "isButton && disabled() && !blocked() ? '' : null",
    '[attr.aria-disabled]': "blocked() ? 'true' : null",
    '[attr.aria-pressed]': 'pressed() == null ? null : pressed()',
    '[attr.aria-describedby]': 'blockedTip.describedBy()',
    '(mouseenter)': 'blockedTip.enter(); tooltip.enter()',
    '(mouseleave)': 'blockedTip.leave(); tooltip.leave()',
    '(focus)': 'blockedTip.focus(); tooltip.focus()',
    '(blur)': 'blockedTip.blur(); tooltip.blur()',
  },
  template: `<lg-icon [name]="icon()" [size]="size() === 'sm' ? 16 : 20" />@if (blocked()) {<span
        class="lg-sr lg-blocked__desc"
        [id]="blockedId"
        aria-hidden="true"
        >{{ blockedTip.spoken() }}</span
      >}`,
  styleUrl: './icon-button.component.css',
})
export class LgIconButtonComponent {
  /** The icon: one of the common actions. */
  readonly icon = input.required<LgIconName>({ alias: 'lgIconButton' });
  /** The action in words: the accessible name and the tooltip ("Close", "Make chat taller"). */
  readonly label = input.required<string>();
  /** `quiet` (the default): no edge until hovered. `framed`: the primary Button's edge, beside Buttons. */
  readonly variant = input<LgIconButtonVariant>('quiet');
  /** An explicit size pins it to one density, as the Button's: `md` reads Standard, `sm` Compact. */
  readonly size = input<'md' | 'sm'>();
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false, { transform: booleanAttribute });
  /** A toggle: true or false sets `aria-pressed`, and true shows it selected. Left unset, it is a plain action. */
  readonly pressed = input<boolean | null | undefined>(undefined);
  readonly state = input<'available' | LgBlockedState>('available');
  /** Why it is blocked; Locked's unlock condition. */
  readonly reason = input<string>();
  /** On cooldown: seconds left. */
  readonly remaining = input<number>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  protected readonly blockedId = lgUniqueId('lgib') + '-why';
  protected readonly blocked = computed(() => lgIsBlocked(this.state()));

  protected readonly blockedTip = new LgBlockedController(
    this.el,
    () => {
      const s = this.state();
      if (!lgIsBlocked(s)) return null;
      const br = lgBlockedReason(
        s,
        { reason: this.reason(), remaining: this.remaining() },
        `Icon button "${this.label()}"`,
      );
      // The icon shows no words, so the tip names the action above its reason.
      return { reason: br.reason, word: br.word, tone: br.tone, spoken: br.spoken, title: this.label() };
    },
    () => this.blockedId,
  );
  /** The label is the tooltip. It is already the name, so the tooltip adds no description. */
  protected readonly tooltip = new LgTooltipController(this.el, () => (this.blocked() ? null : this.label()), {
    description: () => '',
  });

  protected readonly hostClass = computed(() =>
    lgCx(
      'lg-iconbtn',
      'lg-iconbtn--' + this.variant(),
      this.size() && 'lg-iconbtn--' + this.size(),
      this.pressed() && 'is-pressed',
      this.blocked() && 'is-blocked',
      this.blocked() && 'is-' + this.state(),
    ),
  );

  constructor() {
    // A blocked icon button does not act: stop the press before (click) handlers on the element hear it.
    this.el.addEventListener(
      'click',
      (event) => {
        if (!this.blocked()) {
          this.tooltip.hide();
          return;
        }
        event.stopImmediatePropagation();
        event.preventDefault();
        this.blockedTip.press(event);
      },
      true,
    );
  }
}
