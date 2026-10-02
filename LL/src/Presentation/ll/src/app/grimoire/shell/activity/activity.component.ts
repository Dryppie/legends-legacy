import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  inject,
  input,
} from '@angular/core';
import { LgTooltipController } from '../../primitives/tooltip/tooltip.directive';

/**
 * The current action at the head of the NavRail (D-109): the action, the time left, a thin progress bar and the way to
 * it. Its host is the element: `<button lgActivity>` or `<a lgActivity>` to go to the action (a press is the native
 * `(click)`, or the link), `<div lgActivity>` to show it. Compact (the compact rail, D-117): the game's own mark — a
 * ring the progress rises in, the ✦ and the live dot — over the `short` word; the action and the time left stay its
 * accessible name, the action its tooltip.
 */
@Component({
  selector: 'button[lgActivity], a[lgActivity], div[lgActivity]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "compact() ? 'lg-activity lg-activity--compact' : 'lg-activity'",
    '[attr.type]': "isButton ? 'button' : null",
    '(mouseenter)': 'tooltip.enter()',
    '(mouseleave)': 'tooltip.leave()',
    '(focus)': 'tooltip.focus()',
    '(blur)': 'tooltip.blur()',
  },
  template: `@if (compact()) {<span class="lg-activity__orb" aria-hidden="true"
        ><span class="lg-activity__well"><span class="lg-activity__rise" [style.--lg-activity-p]="fraction()"></span></span
        ><span class="lg-activity__glyph">✦</span><span class="lg-activity__live"></span></span
      ><span class="lg-activity__word" aria-hidden="true">{{ short() || label() || 'Idle' }}</span
      ><span class="lg-activity__label">{{ label() || 'Idle' }}</span
      >@if (remaining()) {<span class="lg-activity__time">{{ remaining() }}</span>}} @else {<span class="lg-activity__head"
        ><span class="lg-activity__label">{{ label() || 'Idle' }}</span
        >@if (remaining()) {<span class="lg-activity__time">{{ remaining() }}</span>}</span
      ><span class="lg-activity__bar" aria-hidden="true"
        ><span class="lg-activity__fill" [style.--lg-activity-p]="fraction()"></span></span
      >@if (isControl && openLabel() !== '') {<span class="lg-activity__open">{{
        openLabel() || 'Go to action'
      }}</span>}}`,
  styleUrl: './activity.component.css',
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
  /** The way to it, on a button or link; "Go to action" by default, '' for none. */
  readonly openLabel = input<string>();
  readonly compact = input(false, { transform: booleanAttribute });
  /** Compact: the one word under the mark ("Battling", "Stopping"); the label by default. */
  readonly short = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  protected readonly isControl = this.isButton || this.el.tagName === 'A';

  protected readonly fraction = computed(() => {
    const max = this.max();
    const p = max ? (this.value() || 0) / max : this.progress() || 0;
    return String(Math.max(0, Math.min(1, p)));
  });
  /** Compact, the action is its tooltip; its words are already its name, so no description. */
  protected readonly tooltip = new LgTooltipController(
    this.el,
    () => (this.compact() ? this.label() || 'Idle' : null),
    { place: () => 'end', description: () => '' },
  );
}
