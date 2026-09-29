import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
} from '@angular/core';
import { LgIconComponent } from './icon.component';
import { LgIconName } from './grimoire-icons';

export type LgButtonVariant = 'primary' | 'solid' | 'quiet' | 'danger' | 'link';

/**
 * Pill button with an optional key cap.
 * `<button lgButton="solid" hotkey="E">Level up</button>`
 */
@Component({
  selector: 'button[lgButton], a[lgButton]',
  imports: [LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    '[attr.type]': 'isButton ? type() : null',
    '[attr.aria-keyshortcuts]': 'hotkey() || null',
  },
  template: `
    @if (icon(); as iconName) {
      <lg-icon [name]="iconName" [size]="size() === 'sm' ? 16 : 18" />
    }
    <span class="lg-btn__label"><ng-content /></span>
    @if (hotkey()) {
      <kbd class="lg-key">{{ hotkey() }}</kbd>
    }
  `,
})
export class LgButtonComponent {
  readonly variant = input<LgButtonVariant, LgButtonVariant | ''>('primary', {
    alias: 'lgButton',
    transform: (value: LgButtonVariant | '') => value || 'primary',
  });
  readonly size = input<'md' | 'sm'>('md');
  /** Shows a key cap and sets aria-keyshortcuts. Wire the key yourself. */
  readonly hotkey = input<string>();
  readonly icon = input<LgIconName>();
  readonly type = input<'button' | 'submit' | 'reset'>('button');

  protected readonly isButton =
    (inject(ElementRef).nativeElement as HTMLElement).tagName === 'BUTTON';

  protected readonly hostClass = computed(() =>
    [
      'lg-btn',
      `lg-btn--${this.variant()}`,
      this.size() === 'sm' ? 'lg-btn--sm' : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
}
