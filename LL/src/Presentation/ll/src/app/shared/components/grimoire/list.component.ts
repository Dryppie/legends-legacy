import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterEveryRender,
  booleanAttribute,
  computed,
  contentChildren,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { LG_RARITY_CODES, LgDensity, LgRarity, LgSlotDirective, lgCx, lgHasSlot } from './grimoire-core';
import { LG_NONE, LG_TIMES, lgFormatNumber } from './grimoire-format';
import { LgIconComponent } from './icon.component';
import { LgIconName } from './grimoire-icons';
import { LgNumComponent } from './num.component';
import { lgMoveKey } from './grimoire-a11y';
import { lgLive } from './grimoire-motion';

export interface LgListColumns {
  thumb: boolean;
  qty: boolean;
  value: boolean;
  trail: boolean;
}

/**
 * The list row: one line of an inventory, a ranking, a member list or an order book, inside an lg-list.
 * `<li lgListRow title="Ember Fang" rarity="Epic" [quantity]="1" value="9,400"></li>`
 * Slots: `lgSlot="thumb"`, `lgSlot="tags"`, `lgSlot="meta"`, `lgSlot="trailing"`. With `interactive` the name is a
 * button that emits `activate`.
 */
@Component({
  selector: 'li[lgListRow]',
  imports: [LgIconComponent, LgNumComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.data-density]': 'density() ?? null',
    '[attr.data-index]': 'index()',
    // `title` and `value` are inputs here; keep the static attributes off the li (a tooltip, an ordinal).
    '[attr.title]': 'null',
    '[attr.value]': 'null',
  },
  template: `
    @if (cols().thumb) {
      <span class="lg-listrow__thumb" aria-hidden="true"
        >@if (image(); as src) {<img [src]="src" alt="" />} @else if (icon(); as iconName) {<lg-icon
            [name]="iconName"
            [size]="20"
          />} @else {<ng-content select="[lgSlot=thumb]" />}</span
      >
    }
    <span class="lg-listrow__main"
      >@if (interactive()) {<button
          type="button"
          class="lg-listrow__hit"
          [attr.tabindex]="tabbable() === false ? -1 : 0"
          [attr.aria-pressed]="selected() == null ? null : !!selected()"
          (click)="activate.emit()"
        >
          <span class="lg-listrow__name">{{ title() }}</span></button
        >} @else {<span class="lg-listrow__name">{{ title() }}</span>}@if (rarity(); as r) {<span
          class="lg-listrow__code"
          aria-hidden="true"
          >{{ codes[r] || r }}</span
        ><span class="lg-sr">, {{ r }}</span>}@if (has('tags')) {<span class="lg-listrow__tags"
          ><ng-content select="[lgSlot=tags]" /></span
        >}@if (meta() || has('meta')) {<span class="lg-listrow__meta">{{ meta() }}<ng-content select="[lgSlot=meta]" /></span
        >}</span
    >
    @if (cols().qty) {
      <span class="lg-listrow__qty"
        >@if (quantity() != null) {<span class="lg-sr">Quantity </span>{{ times }}{{ format(quantity()) }}} @else {{{
          none
        }}}</span
      >
    }
    @if (cols().value) {
      <span [class]="valueClass()"><lg-num [value]="value() === undefined ? null : liveValue.value()" /></span>
    }
    @if (cols().trail) {
      <span class="lg-listrow__trail"><ng-content select="[lgSlot=trailing]" /></span>
    }
  `,
})
export class LgListRowComponent {
  readonly title = input.required<string>();
  readonly rarity = input<LgRarity>();
  readonly icon = input<LgIconName>();
  readonly image = input<string>();
  readonly meta = input<string>();
  readonly quantity = input<number | null>();
  /** The value column; leave it unset (undefined) for no value. */
  readonly value = input<number | string | null | undefined>(undefined);
  /** The value changes by itself (a refreshed price): it takes the live-update mark. */
  readonly live = input(false, { transform: booleanAttribute });
  readonly selected = input<boolean | null>(null);
  readonly muted = input(false, { transform: booleanAttribute });
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly density = input<LgDensity>();
  readonly activate = output<void>();

  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly codes: Record<string, string> = LG_RARITY_CODES;
  protected readonly format = lgFormatNumber;
  protected readonly times = LG_TIMES;
  protected readonly none = LG_NONE;

  // Set by the lg-list it sits in: the shared columns, its index, and whether it is the list's tab stop.
  private readonly listCols = signal<LgListColumns | null>(null);
  protected readonly index = signal<number | null>(null);
  protected readonly tabbable = signal<boolean | null>(null);

  /** The columns this row would need on its own. */
  readonly ownColumns = computed<LgListColumns>(() => ({
    thumb: !!(this.icon() || this.image() || this.has('thumb')),
    qty: this.quantity() != null,
    value: this.value() !== undefined,
    trail: this.has('trailing'),
  }));
  protected readonly cols = computed(() => this.listCols() ?? this.ownColumns());

  protected readonly liveValue = lgLive(
    () => this.value(),
    () => ({ mark: this.live() }),
  );
  protected readonly valueClass = computed(() => lgCx('lg-listrow__value', this.live() && this.liveValue.className()));
  protected readonly classes = computed(() => {
    const r = this.rarity();
    return lgCx('lg-listrow', r && 'lg-listrow--' + r.toLowerCase(), this.selected() && 'is-selected', this.muted() && 'is-muted');
  });

  /** Called by lg-list. */
  setListContext(cols: LgListColumns, index: number, tabbable: boolean): void {
    this.listCols.set(cols);
    this.index.set(index);
    this.tabbable.set(tabbable);
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}

/**
 * The list: rows that share columns through CSS subgrid — thumbnail, name, quantity, value, trailing action. A column
 * appears only if some row uses it, and every row then keeps its cell, so values line up. The list is one tab stop:
 * Up, Down, Home and End move between rows; Right moves into a row's trailing action and Left back to its name.
 */
@Component({
  selector: 'lg-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ul
      #list
      [class]="classes()"
      role="list"
      [attr.aria-label]="label() ?? null"
      [attr.data-density]="density() ?? null"
      [style.grid-template-columns]="columns()"
      (keydown)="onKeydown($event)"
      (focusin)="onFocus($event)"
    >
      <ng-content />
    </ul>
  `,
})
export class LgListComponent {
  readonly label = input<string>();
  readonly density = input<LgDensity>();
  readonly rhythm = input<'separators' | 'zebra' | 'spacing'>();

  private readonly rows = contentChildren(LgListRowComponent);
  private readonly list = viewChild.required<ElementRef<HTMLElement>>('list');
  private readonly focused = signal<number | null>(null);

  protected readonly classes = computed(() =>
    lgCx('lg-list', this.rhythm() && this.rhythm() !== 'separators' && 'lg-list--' + this.rhythm()),
  );
  private readonly has = computed<LgListColumns>(() => {
    const has = { thumb: false, qty: false, value: false, trail: false };
    for (const row of this.rows()) {
      const c = row.ownColumns();
      has.thumb ||= c.thumb;
      has.qty ||= c.qty;
      has.value ||= c.value;
      has.trail ||= c.trail;
    }
    return has;
  });
  protected readonly columns = computed(() => {
    const has = this.has();
    return (
      (has.thumb ? 'auto ' : '') +
      'minmax(0, 1fr)' +
      (has.qty ? ' auto' : '') +
      (has.value ? ' auto' : '') +
      (has.trail ? ' auto' : '')
    );
  });
  private readonly active = computed(() => {
    const f = this.focused();
    if (f != null) return f;
    return Math.max(0, this.rows().findIndex((r) => r.selected()));
  });

  constructor() {
    effect(() => {
      const has = this.has();
      const active = this.active();
      this.rows().forEach((row, i) => row.setListContext(has, i, i === active));
    });
    // Trailing actions leave the tab order; Right reaches them.
    afterEveryRender(() => {
      this.list()
        .nativeElement.querySelectorAll('.lg-listrow__trail button, .lg-listrow__trail a, .lg-listrow__tags button')
        .forEach((b) => b.setAttribute('tabindex', '-1'));
    });
  }

  protected onFocus(event: FocusEvent): void {
    const row = (event.target as Element).closest?.('.lg-listrow');
    if (row) this.focused.set(+row.getAttribute('data-index')!);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    const row = target.closest?.('.lg-listrow');
    if (!row) return;
    const i = +row.getAttribute('data-index')!;
    if (event.key === 'ArrowRight') {
      const t = row.querySelector<HTMLElement>('.lg-listrow__trail button, .lg-listrow__trail a');
      if (t) {
        event.preventDefault();
        t.focus();
      }
      return;
    }
    if (event.key === 'ArrowLeft') {
      const hit = row.querySelector<HTMLElement>('.lg-listrow__hit');
      if (hit && target !== hit) {
        event.preventDefault();
        hit.focus();
      }
      return;
    }
    const j = lgMoveKey(event.key, i, this.rows().length);
    if (j == null) return;
    const next = (event.currentTarget as HTMLElement).querySelector<HTMLElement>(
      `.lg-listrow[data-index="${j}"] .lg-listrow__hit`,
    );
    if (!next) return;
    event.preventDefault();
    this.focused.set(j);
    next.focus();
  }
}
