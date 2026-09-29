import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_HEX_INNER, LG_HEX_OUTER } from './grimoire-core';

export type LgSigilState = 'default' | 'selected' | 'ready' | 'locked';
export type LgSigilLabelPosition = 'right' | 'left' | 'top' | 'bottom' | 'none';
export type LgSigilSize = 'sm' | 'md' | 'lg';

/** Hexagon stat badge: a short value in a verdigris hex with its name beside it. */
@Component({
  selector: 'lg-sigil',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <ng-template #badge>
      <span class="lg-sigil__badge">
        <svg class="lg-sigil__hex" viewBox="0 0 100 100" aria-hidden="true" focusable="false">
          <polygon class="lg-sigil__outer" [attr.points]="hexOuter" />
          <polygon class="lg-sigil__inner" [attr.points]="hexInner" />
        </svg>
        <span class="lg-sigil__value">{{ value() }}</span>
      </span>
    </ng-template>
    <ng-template #body>
      @if (labelFirst()) {
        <span class="lg-sigil__label">{{ label() }}</span>
      }
      <ng-container [ngTemplateOutlet]="badge" />
      @if (labelLast()) {
        <span class="lg-sigil__label">{{ label() }}</span>
      }
    </ng-template>

    @if (interactive()) {
      <button
        type="button"
        [class]="classes()"
        [disabled]="state() === 'locked'"
        [attr.aria-pressed]="state() === 'selected'"
        [attr.aria-label]="accessibleLabel()"
        [attr.title]="title() ?? null"
        (click)="activate.emit()"
      >
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <div
        [class]="classes()"
        role="img"
        [attr.aria-label]="accessibleLabel()"
        [attr.title]="title() ?? null"
      >
        <ng-container [ngTemplateOutlet]="body" />
      </div>
    }
  `,
})
export class LgSigilComponent {
  readonly value = input.required<string | number>();
  readonly label = input<string>();
  readonly labelPosition = input<LgSigilLabelPosition>();
  readonly size = input<LgSigilSize>('md');
  readonly state = input<LgSigilState>('default');
  readonly title = input<string>();
  /** Renders a toggle button and emits `activate` on click. */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly hexOuter = LG_HEX_OUTER;
  protected readonly hexInner = LG_HEX_INNER;

  protected readonly position = computed<LgSigilLabelPosition>(
    () => this.labelPosition() ?? (this.label() ? 'right' : 'none'),
  );
  protected readonly labelFirst = computed(
    () => !!this.label() && (this.position() === 'left' || this.position() === 'top'),
  );
  protected readonly labelLast = computed(
    () => !!this.label() && (this.position() === 'right' || this.position() === 'bottom'),
  );
  protected readonly classes = computed(() =>
    [
      'lg-sigil',
      `lg-sigil--${this.size()}`,
      `lg-sigil--label-${this.position()}`,
      this.state() !== 'default' ? `is-${this.state()}` : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
  protected readonly accessibleLabel = computed(() => {
    const suffix =
      this.state() === 'locked'
        ? ', locked'
        : this.state() === 'ready'
          ? ', can be raised'
          : '';
    return `${this.label() ? this.label() + ' ' : ''}${this.value()}${suffix}`;
  });
}
