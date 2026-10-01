import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  contentChildren,
  inject,
  input,
  output,
} from '@angular/core';
import { LG_SHELL, LgSlotDirective, lgHasSlot } from './grimoire-core';

/**
 * The top bar: who you are and what you carry. Put a progress Track in `lgSlot="center"` while a run is in progress;
 * the default content (CurrencyPills) sits at the end. `showMenu` adds the menu button (it shows on narrow screens).
 */
@Component({
  selector: 'lg-top-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <header class="lg-topbar">
      @if (showMenu()) {
        <button type="button" class="lg-topbar__menu" aria-label="Open navigation" (click)="openMenu()">
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true">
            <path d="M4 7h16M4 12h16M4 17h10" />
          </svg>
        </button>
      }
      <div class="lg-topbar__title">
        @if (eyebrow()) {
          <span class="lg-topbar__eyebrow">{{ eyebrow() }}</span>
        }
        <span class="lg-topbar__name">{{ title() }}</span>
      </div>
      @if (has('center')) {
        <div class="lg-topbar__center"><ng-content select="[lgSlot=center]" /></div>
      }
      <div class="lg-topbar__end"><ng-content /></div>
    </header>
  `,
})
export class LgTopBarComponent {
  readonly title = input.required<string>();
  /** "Lv. 17", a region, a section. */
  readonly eyebrow = input<string>();
  /** Show the menu button (React's onMenu). Inside a GameShell it opens the rail drawer; it also emits `menu`. */
  readonly showMenu = input(false, { transform: booleanAttribute });
  readonly menu = output<void>();

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly shell = inject(LG_SHELL, { optional: true });

  /** Inside a GameShell the menu button opens its rail drawer. */
  protected openMenu(): void {
    this.shell?.openRail();
    this.menu.emit();
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
