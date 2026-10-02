import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { LgSigilComponent, LgSigilLabelPosition, LgSigilSize } from '../sigil/sigil.component';
import { lgMoveKey } from '../../core/grimoire-a11y';

export interface LgConstellationItem {
  id: string;
  label: string;
  value: string | number;
  x: number;
  y: number;
  labelPosition?: LgSigilLabelPosition;
  size?: LgSigilSize;
  state?: 'default' | 'ready' | 'locked';
  /** Locked: how it unlocks. */
  reason?: string;
}

export interface LgConstellationRing {
  cx: number;
  cy: number;
  r: number;
  strong?: boolean;
}

/**
 * The stat star chart: Sigils placed on thin orbit rings, in a fixed-aspect coordinate space. Nodes are drawn as ticks
 * across their ring. When selectable, it is one tab stop; arrow keys, Home and End move between the Sigils.
 */
@Component({
  selector: 'lg-constellation',
  imports: [LgSigilComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div
      class="lg-constellation"
      [style.aspect-ratio]="width() + ' / ' + height()"
      role="group"
      [attr.aria-label]="label()"
      (focusin)="onFocus($event)"
      (keydown)="onKeydown($event)"
    >
      <svg
        class="lg-constellation__rings"
        [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
        preserveAspectRatio="xMidYMid meet"
        aria-hidden="true"
        focusable="false"
      >
        @for (ring of rings(); track $index) {
          <circle [attr.cx]="ring.cx" [attr.cy]="ring.cy" [attr.r]="ring.r" [attr.class]="ring.strong ? 'is-strong' : null" />
        }
        @for (tick of ticks(); track $index) {
          <line [attr.x1]="tick.x1" [attr.y1]="tick.y1" [attr.x2]="tick.x2" [attr.y2]="tick.y2" class="lg-constellation__tick" />
        }
      </svg>
      @for (item of items(); track item.id; let i = $index) {
        <div
          [class]="'lg-constellation__node lg-constellation__node--' + (item.labelPosition || 'right') + ' lg-constellation__node--' + (item.size || 'md')"
          [style.left.%]="(item.x / width()) * 100"
          [style.top.%]="(item.y / height()) * 100"
        >
          <lg-sigil
            [label]="item.label"
            [value]="item.value"
            [labelPosition]="item.labelPosition || 'right'"
            [size]="item.size || 'md'"
            [state]="item.id === selectedId() ? 'selected' : item.state || 'default'"
            [reason]="item.reason"
            [interactive]="selectable()"
            [tabIndex]="i === active() ? 0 : -1"
            [dataIndex]="i"
            (activate)="select.emit(item.id)"
          />
        </div>
      }
    </div>
  `,
})
export class LgConstellationComponent {
  readonly items = input.required<readonly LgConstellationItem[]>();
  readonly width = input(1000);
  readonly height = input(700);
  readonly rings = input<readonly LgConstellationRing[]>([]);
  readonly nodes = input<readonly { x: number; y: number }[]>([]);
  readonly selectedId = input<string>();
  /** The Sigils are buttons that emit `select`. */
  readonly selectable = input(true, { transform: booleanAttribute });
  readonly label = input('Attributes');
  readonly select = output<string>();

  private readonly focused = signal<number | null>(null);
  protected readonly active = computed(() => {
    const f = this.focused();
    if (f != null) return f;
    return Math.max(0, this.items().findIndex((it) => it.id === this.selectedId()));
  });
  /** Each node is a tick across its nearest ring, like an astrolabe's graduations. */
  protected readonly ticks = computed(() =>
    this.nodes().map((n) => {
      let best: { d: number; r: LgConstellationRing } | null = null;
      for (const r of this.rings()) {
        const d = Math.abs(Math.hypot(n.x - r.cx, n.y - r.cy) - r.r);
        if (!best || d < best.d) best = { d, r };
      }
      let ux = 0;
      let uy = 1;
      if (best) {
        const dx = n.x - best.r.cx;
        const dy = n.y - best.r.cy;
        const len = Math.hypot(dx, dy) || 1;
        ux = dx / len;
        uy = dy / len;
      }
      return { x1: n.x - ux * 6, y1: n.y - uy * 6, x2: n.x + ux * 6, y2: n.y + uy * 6 };
    }),
  );

  protected onFocus(event: FocusEvent): void {
    const i = (event.target as Element).getAttribute?.('data-index');
    if (i != null) this.focused.set(+i);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (!this.selectable()) return;
    const j = lgMoveKey(event.key, this.active(), this.items().length);
    if (j == null) return;
    event.preventDefault();
    this.focused.set(j);
    (event.currentTarget as HTMLElement).querySelector<HTMLElement>(`[data-index="${j}"]`)?.focus();
  }
}
