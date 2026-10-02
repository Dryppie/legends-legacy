import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { LgPoint, lgPointsAttr, lgPolygon } from '../../core/grimoire-core';

function gcd(a: number, b: number): number {
  return b ? gcd(b, a % b) : a;
}

/** The attribute sign: engraved star-polygon line art for Folio headers. Give each attribute its own point count. */
@Component({
  selector: 'lg-emblem',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <svg
      class="lg-emblem"
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 100 100"
      aria-hidden="true"
      focusable="false"
    >
      <circle cx="50" cy="50" r="48" class="lg-emblem__ring" />
      <circle cx="50" cy="50" r="44" class="lg-emblem__ring lg-emblem__ring--thin" />
      @for (point of outer(); track $index) {
        <line x1="50" y1="50" [attr.x2]="point[0].toFixed(2)" [attr.y2]="point[1].toFixed(2)" class="lg-emblem__spoke" />
      }
      <polygon [attr.points]="frame()" class="lg-emblem__frame" />
      @for (star of stars(); track $index) {
        <polygon [attr.points]="star" class="lg-emblem__star" />
      }
      <polygon [attr.points]="inner()" class="lg-emblem__inner" />
      <circle cx="50" cy="50" r="9" class="lg-emblem__core" />
      <circle cx="50" cy="50" r="3" class="lg-emblem__heart" />
      @for (point of outer(); track $index) {
        <circle [attr.cx]="point[0].toFixed(2)" [attr.cy]="point[1].toFixed(2)" r="1.6" class="lg-emblem__dot" />
      }
    </svg>
  `,
})
export class LgEmblemComponent {
  /** Star points, 5–12. */
  readonly points = input(6);
  /** Vertex step of the star polygon; defaults to 2 (≤6 points) or 3. */
  readonly skip = input<number>();
  readonly size = input(160);

  protected readonly outer = computed<LgPoint[]>(() => lgPolygon(this.points(), 44));
  protected readonly frame = computed(() => lgPointsAttr(this.outer()));
  protected readonly inner = computed(() =>
    lgPointsAttr(lgPolygon(this.points(), 24, 50, 50, -90 + 180 / this.points())),
  );
  protected readonly stars = computed(() => {
    const n = this.points();
    const k = this.skip() ?? (n >= 7 ? 3 : 2);
    const outer = this.outer();
    const loops = gcd(n, k);
    const result: string[] = [];
    for (let loop = 0; loop < loops; loop++) {
      const pts: LgPoint[] = [];
      let index = loop;
      for (let j = 0; j < n / loops; j++) {
        pts.push(outer[index]);
        index = (index + k) % n;
      }
      result.push(lgPointsAttr(pts));
    }
    return result;
  });
}
