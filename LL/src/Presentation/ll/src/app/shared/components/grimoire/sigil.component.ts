import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_HEX_INNER, LG_HEX_OUTER, lgCx, lgUniqueId } from './grimoire-core';
import { LgWhyDirective, LgWhyOptions, lgWhySpoken } from './grimoire-a11y';
import { lgBlockedReason } from './grimoire-states';

export type LgSigilState = 'default' | 'selected' | 'ready' | 'locked';
export type LgSigilLabelPosition = 'right' | 'left' | 'top' | 'bottom' | 'none';
export type LgSigilSize = 'sm' | 'md' | 'lg';

/**
 * The hex stat badge: a short value in a verdigris hexagon with its name beside it. An interactive Sigil is a toggle
 * button; a locked one stays focusable, shows its unlock condition (`reason`) and does not emit `activate`.
 */
@Component({
  selector: 'lg-sigil',
  imports: [NgTemplateOutlet, LgWhyDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <ng-template #badge
      ><span class="lg-sigil__badge"
        ><svg class="lg-sigil__hex" viewBox="0 0 100 100" aria-hidden="true" focusable="false"
          ><polygon class="lg-sigil__outer" [attr.points]="hexOuter" /><polygon
            class="lg-sigil__inner"
            [attr.points]="hexInner" /></svg
        ><span class="lg-sigil__value">{{ value() }}</span></span
      ></ng-template
    >
    <ng-template #labelTpl><span class="lg-sigil__label">{{ label() }}</span></ng-template>
    <ng-template #body
      >@if (showLabel() && labelFirst()) {<ng-container [ngTemplateOutlet]="labelTpl" />}<ng-container
        [ngTemplateOutlet]="badge"
      />@if (showLabel() && !labelFirst()) {<ng-container [ngTemplateOutlet]="labelTpl" />}</ng-template
    >
    @if (interactive()) {
      <button
        type="button"
        [class]="classes()"
        [attr.tabindex]="tabIndex() ?? null"
        [attr.data-index]="dataIndex() ?? null"
        [attr.aria-pressed]="why() ? null : state() === 'selected'"
        [attr.aria-label]="srText()"
        [attr.title]="title() ?? null"
        [lgWhy]="why()"
        [lgWhyId]="whyId"
        (click)="why() ? null : activate.emit()"
      >
        <ng-container [ngTemplateOutlet]="body" />@if (why(); as w) {<span
            class="lg-sr lg-why__desc"
            [id]="whyId"
            aria-hidden="true"
            >{{ spoken(w) }}</span
          >}
      </button>
    } @else {
      <div [class]="classes()" [attr.data-index]="dataIndex() ?? null" [attr.aria-label]="srText()" [attr.title]="title() ?? null">
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
  /** Locked: how it unlocks. */
  readonly reason = input<string>();
  readonly title = input<string>();
  /** Renders a toggle button and emits `activate` on a press. */
  readonly interactive = input(false, { transform: booleanAttribute });
  /** Roving tabindex, set by Constellation. */
  readonly tabIndex = input<number>();
  readonly dataIndex = input<number>();
  readonly activate = output<void>();

  protected readonly hexOuter = LG_HEX_OUTER;
  protected readonly hexInner = LG_HEX_INNER;
  protected readonly whyId = lgUniqueId('lgs') + '-why';
  protected readonly spoken = lgWhySpoken;

  protected readonly position = computed<LgSigilLabelPosition>(
    () => this.labelPosition() || (this.label() ? 'right' : 'none'),
  );
  protected readonly showLabel = computed(() => !!this.label() && this.position() !== 'none');
  protected readonly labelFirst = computed(() => this.position() === 'left' || this.position() === 'top');
  /** A locked Sigil that would be a button stays one: focusable, aria-disabled, with its unlock condition. */
  protected readonly why = computed<LgWhyOptions | null>(() =>
    this.interactive() && this.state() === 'locked'
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
  /** The lock is in the description when there is one, so the name doesn't say it twice. */
  protected readonly srText = computed(
    () =>
      (this.label() ? this.label() + ' ' : '') +
      this.value() +
      (this.state() === 'locked' && !this.why() ? ', locked' : this.state() === 'ready' ? ', can be raised' : ''),
  );
}
