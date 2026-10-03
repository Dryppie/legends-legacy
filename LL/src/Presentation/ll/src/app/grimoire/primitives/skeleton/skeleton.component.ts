import { ChangeDetectionStrategy, Component, computed, input, numberAttribute } from '@angular/core';

export type LgSkeletonShape = 'text' | 'rows' | 'block';

/**
 * The Loading state's still blocks, in the shape of what is coming (Standards · States · Data): lines of text, list or
 * table rows, or a block (a portrait, a tile). They are still — no shimmer, no pulse (Foundations · Motion · Loops) —
 * and appear only after 300ms, so a quick load never flashes them. They are for the eye: screen readers hear the
 * region's `aria-busy` and its words, which a Region state gives (`lg-region-state state="loading"`).
 *
 *   <lg-skeleton count="3" />                 three lines, the last shorter
 *   <lg-skeleton shape="rows" count="5" />    five rows at the region's row height
 *   <lg-skeleton shape="block" width="4rem" height="4rem" />
 */
@Component({
  selector: 'lg-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "'lg-skeleton lg-skeleton--' + shape()",
    'aria-hidden': 'true',
    '[style.width]': 'width() || null',
    '[style.height]': "shape() === 'block' ? height() || null : null",
  },
  template: `@if (shape() !== 'block') {
    @for (n of items(); track n) {<span class="lg-skeleton__bar"></span>}
  }`,
  styleUrl: './skeleton.component.css',
})
export class LgSkeletonComponent {
  /** `text` (the default): lines of the region's body text. `rows`: rows at `--lg-row`. `block`: one block. */
  readonly shape = input<LgSkeletonShape>('text');
  /** How many lines or rows: 1 line, or 3 rows, by default. */
  readonly count = input<number | undefined, unknown>(undefined, { transform: numberAttribute });
  /** Any CSS width; the region's width by default. */
  readonly width = input<string>();
  /** A block's height: any CSS height, `--lg-thumb` by default. */
  readonly height = input<string>();

  protected readonly items = computed(() => {
    const n = this.count();
    const count = n != null && n >= 1 ? Math.floor(n) : this.shape() === 'rows' ? 3 : 1;
    return Array.from({ length: Math.min(count, 50) }, (_, i) => i);
  });
}
