import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type LgDeltaDirection = 'up' | 'down' | 'none';
export type LgDeltaPolarity = 'better' | 'worse' | 'neutral';

/**
 * A stat change: glyph, sign and value, coloured by polarity. The glyph and sign say which way the number moved; the
 * polarity (from the game's rules, never from the sign) says whether that helps the player. Screen readers hear the
 * judgement as a word. Unchanged is ±0, with no shape.
 */
@Component({
  selector: 'lg-delta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "'lg-delta lg-delta--' + polarity()" },
  template: `@if (glyph()) {<span class="lg-delta__glyph" aria-hidden="true">{{ glyph() }}</span
      >}<span class="lg-delta__value"
      >@if (glyph()) {{{ sign() }}} @else {<span aria-hidden="true">{{ sign() }}</span>}{{ value() }}</span
    >@if (word()) {<span class="lg-sr">, {{ word() }}</span>}`,
  styleUrl: './delta.component.css',
})
export class LgDeltaComponent {
  readonly direction = input.required<LgDeltaDirection>();
  /** The size of the change, formatted, without a sign: "12%", "1.2s", "0". */
  readonly value = input.required<string | number>();
  readonly polarity = input.required<LgDeltaPolarity>();

  protected readonly glyph = computed(() =>
    this.direction() === 'up' ? '▲' : this.direction() === 'down' ? '▼' : null,
  );
  protected readonly sign = computed(() =>
    this.direction() === 'up' ? '+' : this.direction() === 'down' ? '−' : '±',
  );
  protected readonly word = computed(() => {
    const p = this.polarity();
    return p === 'better' ? 'better' : p === 'worse' ? 'worse' : this.direction() === 'none' ? 'unchanged' : null;
  });
}
