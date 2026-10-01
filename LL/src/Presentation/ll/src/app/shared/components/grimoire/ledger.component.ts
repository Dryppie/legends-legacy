import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { LgDensity, lgCx, lgUniqueId } from './grimoire-core';
import { LG_NBSP, LG_NONE, LgNumeric, lgMissing, lgNumberParts } from './grimoire-format';
import { lgMoveKey } from './grimoire-a11y';

export interface LgLedgerRow {
  id?: string;
  label: string;
  /** A number, or a formatted value with its unit ("24.8%", "84 HP/5s"). */
  value?: LgNumeric;
  /** Second line under the value, e.g. "240 Armor Rating". */
  sub?: string;
  /** What the row means; shown on hover and keyboard focus, pinned by a click. */
  description?: string;
  /** A footnote in the explanation. */
  tipMeta?: string;
  muted?: boolean;
}

/**
 * The labelled value list: titled label/value rows joined by dotted leaders, each able to explain itself. Values align
 * on the decimal point. Rows that explain themselves are one tab stop (Up, Down, Home, End); a click or tap pins a
 * row's explanation; Escape closes it. Put several in `<div class="lg-ledgergrid">`.
 */
@Component({
  selector: 'lg-ledger',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <section #root class="lg-ledger" [attr.aria-labelledby]="id + '-t'" [attr.data-density]="density() ?? null">
      <h3 class="lg-ledger__title" [id]="id + '-t'">{{ title() }}</h3>
      <dl class="lg-ledger__rows" (keydown)="onKeydown($event)">
        @for (row of view(); track row.key; let i = $index) {
          <div
            [class]="row.classes"
            [attr.tabindex]="row.described ? (i === active() ? 0 : -1) : null"
            [attr.aria-describedby]="row.described ? id + '-tip-' + i : null"
            [attr.data-index]="i"
            (focus)="row.described ? focused.set(i) : null"
            (click)="row.described ? toggle(i) : null"
            (mouseleave)="row.described && dismissed() === i ? dismissed.set(null) : null"
            (blur)="row.described && dismissed() === i ? dismissed.set(null) : null"
          >
            <dt class="lg-ledger__label"
              ><span class="lg-ledger__labeltext">{{ row.row.label }}</span
              ><span class="lg-ledger__leader" aria-hidden="true"></span
            ></dt>
            @if (row.parts; as q) {
              <dd class="lg-ledger__value"
                ><span class="lg-ledger__num lg-ledger__int">{{ q.int }}</span
                ><span class="lg-ledger__num lg-ledger__tail"
                  >{{ q.frac }}@if (q.unit) {<span class="lg-unit">{{ q.sp ? nbsp : '' }}{{ q.unit }}</span>}</span
                ></dd
              >
            } @else {
              <dd class="lg-ledger__value is-text"><span class="lg-ledger__num">{{ row.row.value }}</span></dd>
            }
            @if (row.row.sub) {
              <dd class="lg-ledger__sub">{{ row.row.sub }}</dd>
            }
            @if (row.described) {
              <div [id]="id + '-tip-' + i" role="tooltip" class="lg-ledger__tip">
                <strong>{{ row.row.label }}</strong>
                <p>{{ row.row.description }}</p>
                @if (row.row.tipMeta) {
                  <span class="lg-ledger__tipmeta">{{ row.row.tipMeta }}</span>
                }
              </div>
            }
          </div>
        }
      </dl>
    </section>
  `,
})
export class LgLedgerComponent {
  readonly title = input.required<string>();
  readonly rows = input.required<readonly LgLedgerRow[]>();
  /** Row height, padding and type: 48 / 40 / 32px rows; compact sets values in numeral-compact. */
  readonly density = input<LgDensity>();

  protected readonly id = lgUniqueId('lgl');
  protected readonly nbsp = LG_NBSP;
  protected readonly focused = signal<number | null>(null);
  protected readonly pinned = signal<number | null>(null);
  protected readonly dismissed = signal<number | null>(null);
  private readonly root = viewChild.required<ElementRef<HTMLElement>>('root');

  private readonly described = computed(() =>
    this.rows()
      .map((r, i) => (r.description ? i : -1))
      .filter((i) => i >= 0),
  );
  protected readonly active = computed(() => this.focused() ?? this.described()[0]);
  protected readonly view = computed(() =>
    this.rows().map((row, i) => ({
      row,
      key: row.id || row.label,
      described: !!row.description,
      parts: lgNumberParts(lgMissing(row.value) ? LG_NONE : row.value),
      classes: lgCx(
        'lg-ledger__row',
        row.muted && 'is-muted',
        this.pinned() === i && 'is-pinned',
        this.dismissed() === i && 'is-dismissed',
      ),
    })),
  );

  constructor() {
    // A pinned explanation closes when the pointer goes down anywhere else.
    const away = (e: PointerEvent) => {
      if (!this.root().nativeElement.contains(e.target as Node)) this.pinned.set(null);
    };
    effect((onCleanup) => {
      if (this.pinned() == null) return;
      document.addEventListener('pointerdown', away);
      onCleanup(() => document.removeEventListener('pointerdown', away));
    });
    inject(DestroyRef).onDestroy(() => document.removeEventListener('pointerdown', away));
  }

  protected toggle(i: number): void {
    this.pinned.set(this.pinned() === i ? null : i);
    this.dismissed.set(null);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      const i = +((event.target as Element).getAttribute('data-index') ?? -1);
      if (this.pinned() === i) {
        this.pinned.set(null);
        event.stopPropagation();
      } else if (this.dismissed() !== i) {
        this.dismissed.set(i);
        event.stopPropagation();
      }
      return;
    }
    const described = this.described();
    const at = described.indexOf(this.active());
    const key = event.key === 'ArrowLeft' || event.key === 'ArrowRight' ? '' : event.key;
    const j = lgMoveKey(key, at, described.length);
    if (j == null || at < 0) return;
    event.preventDefault();
    this.focused.set(described[j]);
    this.dismissed.set(null);
    (event.currentTarget as HTMLElement).querySelector<HTMLElement>(`[data-index="${described[j]}"]`)?.focus();
  }
}
