import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

/**
 * The current action at the head of the NavRail (D-109): the action, the time left, a thin progress bar and the way to
 * it. `interactive` renders a button that emits `activate`. Compact (the compact rail, D-117): the game's
 * own mark — a ring the progress rises in, the ✦ and the live dot — over the `short` word; the action and the time left
 * stay its accessible name, the action its tooltip.
 */
@Component({
  selector: 'lg-activity',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body
      >@if (compact()) {<span class="lg-activity__orb" aria-hidden="true"
          ><span class="lg-activity__well"><span class="lg-activity__rise" [style.--lg-activity-p]="fraction()"></span></span
          ><span class="lg-activity__glyph">✦</span><span class="lg-activity__live"></span></span
        ><span class="lg-activity__word" aria-hidden="true">{{ short() || label() || 'Idle' }}</span
        ><span class="lg-activity__label">{{ label() || 'Idle' }}</span
        >@if (remaining()) {<span class="lg-activity__time">{{ remaining() }}</span>}} @else {<span class="lg-activity__head"
        ><span class="lg-activity__label">{{ label() || 'Idle' }}</span
        >@if (remaining()) {<span class="lg-activity__time">{{ remaining() }}</span>}</span
      ><span class="lg-activity__bar" aria-hidden="true"
        ><span class="lg-activity__fill" [style.--lg-activity-p]="fraction()"></span></span
      >@if (interactive() && openLabel() !== '') {<span class="lg-activity__open">{{
        openLabel() || 'Go to action'
      }}</span>}}</ng-template
    >
    @if (interactive()) {
      <button type="button" [class]="classes()" [attr.title]="compact() ? label() : null" (click)="activate.emit()">
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <div [class]="classes()" [attr.title]="compact() ? label() : null"><ng-container [ngTemplateOutlet]="body" /></div>
    }
  `,
})
export class LgActivityComponent {
  /** "Engaged in Combat", "Idle". */
  readonly label = input.required<string>();
  /** Time left, as printed ("00:12"). */
  readonly remaining = input<string | null>();
  /** Progress as value of max, or `progress` 0–1. */
  readonly value = input<number>();
  readonly max = input<number>();
  readonly progress = input<number>();
  /** The way to it; "Go to action" by default, '' for none. */
  readonly openLabel = input<string>();
  readonly compact = input(false, { transform: booleanAttribute });
  /** Compact: the one word under the mark ("Battling", "Stopping"); the label by default. */
  readonly short = input<string>();
  /** Renders a button that emits `activate`. */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly fraction = computed(() => {
    const max = this.max();
    const p = max ? (this.value() || 0) / max : this.progress() || 0;
    return String(Math.max(0, Math.min(1, p)));
  });
  protected readonly classes = computed(() => (this.compact() ? 'lg-activity lg-activity--compact' : 'lg-activity'));
}
