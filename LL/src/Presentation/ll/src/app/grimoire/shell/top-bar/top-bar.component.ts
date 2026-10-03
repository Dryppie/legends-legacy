import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  contentChild,
  inject,
  input,
  output,
} from '@angular/core';
import { LG_SHELL } from '../../core/grimoire-core';

/** The TopBar's centre: one "now" thing — a run's progress Track while a run is in progress, else an Objective. */
@Component({
  selector: 'lg-top-bar-center',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-topbar__center' },
  template: '<ng-content />',
  styles: `
    :host { flex: 1 1 11.25rem; display: flex; justify-content: center; min-width: 0; container: lg-topcenter / inline-size; }
    /* In a narrow GameShell, under 40rem, the "now" thing takes a second row (D-114). */
    @container lg-shell (max-width: 40rem) {
      :host { position: absolute; left: var(--lg-space-3); right: var(--lg-space-3); bottom: var(--lg-space-1); justify-content: stretch; }
    }
  `,
})
export class LgTopBarCenterComponent {}

/**
 * The top bar: who you are and what you carry. Put a progress Track in an `lg-top-bar-center` while a run is in
 * progress; the rest of the content (CurrencyPills) sits at the end. `showMenu` adds the menu button (it shows on
 * narrow screens).
 */
@Component({
  selector: 'lg-top-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-topbar', '[class.has-center]': '!!center()' },
  template: `
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
      <span class="lg-topbar__name">{{ heading() }}</span>
    </div>
    <ng-content select="lg-top-bar-center" />
    <div class="lg-topbar__end"><ng-content /></div>
  `,
  styleUrl: './top-bar.component.css',
})
export class LgTopBarComponent {
  /** The character's name. */
  readonly heading = input.required<string>();
  /** "Lv. 17", a region, a section. */
  readonly eyebrow = input<string>();
  /** Show the menu button. Inside a GameShell it opens the rail drawer; it also emits `menu`. */
  readonly showMenu = input(false, { transform: booleanAttribute });
  readonly menu = output<void>();

  protected readonly center = contentChild(LgTopBarCenterComponent);
  private readonly shell = inject(LG_SHELL, { optional: true });

  /** Inside a GameShell the menu button opens its rail drawer. */
  protected openMenu(): void {
    this.shell?.openRail();
    this.menu.emit();
  }
}
