import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  booleanAttribute,
  computed,
  contentChildren,
  forwardRef,
  inject,
  input,
  numberAttribute,
  signal,
} from '@angular/core';
import { FocusKeyManager, FocusableOption } from '@angular/cdk/a11y';
import { LgDensity, lgUniqueId } from '../../core/grimoire-core';
import {
  LG_NBSP,
  LG_NONE,
  LgNumeric,
  lgFormatNumber,
  lgMissing,
  lgNumberParts,
  lgUnitSplit,
} from '../../core/grimoire-format';
import { LgTooltipController, lgSentences } from '../../primitives/tooltip/tooltip.directive';
import { LgDeltaComponent, LgDeltaPolarity } from '../delta/delta.component';

/** A row's data, for a feature that builds its rows as a list: each becomes a `div[lgLedgerRow]`'s inputs. */
export interface LgLedgerRow {
  id?: string;
  label: string;
  value?: LgNumeric;
  sub?: string;
  description?: string;
  tipMeta?: string;
  muted?: boolean;
}

/** How a Ledger's rows coordinate: its one tab stop, which the rows read and report to. */
abstract class LgLedgerRows {
  abstract isTabStop(row: LgLedgerRowComponent): boolean;
  abstract focused(row: LgLedgerRowComponent): void;
}

/**
 * One row of a Ledger: a label joined to its value by a dotted leader. A `div`, because a `dl` holds its term and
 * definition pairs in `div`s. With a `description` it explains itself: the explanation shows on hover and keyboard
 * focus, a press pins it, and it is the row's description for screen readers. A `delta` adds the change under the
 * value, coloured by `deltaPolarity` (the game's rules), never by its sign.
 */
@Component({
  selector: 'div[lgLedgerRow]',
  imports: [LgDeltaComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-ledger__row',
    '[class.is-muted]': 'muted()',
    '[class.is-explained]': 'explanation.isShown()',
    '[class.is-pinned]': 'explanation.isPinned()',
    '[attr.tabindex]': 'explains() ? (isTabStop() ? 0 : -1) : null',
    '(mouseenter)': 'explains() && explanation.enter()',
    '(mouseleave)': 'explains() && explanation.leave()',
    '(focus)': 'onFocus()',
    '(blur)': 'explains() && explanation.blur()',
    '(click)': 'explains() && explanation.press()',
  },
  template: `
    <dt class="lg-ledger__label"
      ><span class="lg-ledger__labeltext">{{ label() }}</span
      ><span class="lg-ledger__leader" aria-hidden="true"></span
    ></dt>
    @if (parts(); as q) {
      <dd class="lg-ledger__value"
        ><span class="lg-ledger__num lg-ledger__int">{{ q.int }}</span
        ><span class="lg-ledger__num lg-ledger__tail"
          >{{ q.frac }}@if (q.unit) {<span class="lg-unit">{{ q.sp ? nbsp : '' }}{{ q.unit }}</span>}</span
        ></dd
      >
    } @else {
      <dd class="lg-ledger__value is-text"><span class="lg-ledger__num">{{ value() }}</span></dd>
    }
    @if (hasDelta() || sub()) {
      <dd class="lg-ledger__sub"
        >@if (hasDelta()) {<lg-delta [direction]="direction()" [polarity]="polarity()" [value]="deltaShown()"
          />@if (sub()) {<span aria-hidden="true"> · </span>}}{{ sub() }}</dd
      >
    }
  `,
  styleUrl: './ledger-row.component.css',
})
export class LgLedgerRowComponent implements FocusableOption {
  readonly label = input.required<string>();
  /** A number, or a formatted value with its unit ("24.8%", "84 HP/5s"). */
  readonly value = input<LgNumeric>();
  /** A second line under the value: "240 Armor Rating". */
  readonly sub = input<string>();
  /** What the row means: its explanation. */
  readonly description = input<string>();
  /** A footnote in the explanation. */
  readonly tipMeta = input<string>();
  readonly muted = input(false, { transform: booleanAttribute });
  /** The change since the last look, signed; 0 shows ±0. */
  readonly delta = input<number | null>();
  /** Whether the change helps the player. Without it the change is neutral: the row can't know the stat's rules. */
  readonly deltaPolarity = input<LgDeltaPolarity>('neutral');
  /** The change as shown, when it differs from the number ("0.10"). */
  readonly deltaText = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly ledger = inject(LgLedgerRows, { optional: true });
  protected readonly nbsp = LG_NBSP;

  readonly explains = computed(() => !!this.description());
  protected readonly isTabStop = computed(() => !!this.ledger?.isTabStop(this));
  protected readonly hasDelta = computed(() => this.delta() != null);
  protected readonly parts = computed(() => {
    const v = this.value();
    return lgNumberParts(lgMissing(v) ? LG_NONE : v);
  });
  protected readonly explanation = new LgTooltipController(
    this.el,
    () =>
      this.explains()
        ? { title: this.label(), text: this.description()!, meta: this.tipMeta(), kind: 'explanation' as const }
        : null,
    { pin: () => true, description: () => lgSentences([this.description(), this.tipMeta()]) },
  );
  protected readonly direction = computed(() => {
    const d = this.delta() ?? 0;
    return d > 0 ? 'up' : d < 0 ? 'down' : 'none';
  });
  protected readonly polarity = computed<LgDeltaPolarity>(() =>
    this.delta() === 0 ? 'neutral' : this.deltaPolarity() || 'neutral',
  );
  /** The change in the value's own unit: −1.2s for a value of 6.8s. */
  protected readonly deltaShown = computed(() => {
    const text = this.deltaText();
    if (text != null) return text;
    const v = this.value();
    const unit = (typeof v === 'string' && lgUnitSplit(v)?.unit) || '';
    return lgFormatNumber(Math.abs(this.delta() ?? 0)) + unit;
  });

  /** FocusableOption: the key manager moves focus here. */
  focus(): void {
    this.el.focus();
  }

  /** FocusableOption: typeahead matches the label. */
  getLabel(): string {
    return this.label();
  }

  protected onFocus(): void {
    if (!this.explains()) return;
    this.ledger?.focused(this);
    this.explanation.focus();
  }
}

/**
 * The labelled value list: a heading, then label/value rows joined by dotted leaders, each able to explain itself.
 *
 *   <lg-ledger heading="Offense">
 *     <div lgLedgerRow label="Power" [value]="142" description="Raw force behind every blow and spell."></div>
 *   </lg-ledger>
 *
 * Values align on the decimal point. The rows that explain themselves are one tab stop: Up, Down, Home, End and the
 * first letters of a label move between them (the CDK's FocusKeyManager). `columns="2"` sets the rows two-up, for
 * secondary stats in a narrow column such as the Folio's. Put several Ledgers in `<div class="lg-ledgergrid">`.
 */
@Component({
  selector: 'lg-ledger',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgLedgerRows, useExisting: forwardRef(() => LgLedgerComponent) }],
  host: {
    class: 'lg-ledger',
    role: 'region',
    '[attr.aria-labelledby]': 'headingId',
    '[attr.data-density]': 'density() ?? null',
    '[attr.data-columns]': 'columns() === 2 ? 2 : null',
  },
  template: `
    <h3 class="lg-ledger__title" [id]="headingId">{{ heading() }}</h3>
    <dl class="lg-ledger__rows" (keydown)="onKeydown($event)"><ng-content /></dl>
  `,
  styleUrl: './ledger.component.css',
})
export class LgLedgerComponent extends LgLedgerRows {
  readonly heading = input.required<string>();
  /** Row height, padding and type: 48 / 40 / 32px rows; compact sets values in numeral-compact. */
  readonly density = input<LgDensity>();
  /** 2 sets the rows two-up. */
  readonly columns = input(1, { transform: numberAttribute });

  protected readonly headingId = lgUniqueId('lg-ledger-heading');
  private readonly rows = contentChildren(LgLedgerRowComponent);
  private readonly explaining = computed(() => this.rows().filter((row) => row.explains()));
  private readonly keys = new FocusKeyManager(this.explaining, inject(Injector))
    .withVerticalOrientation()
    .withHorizontalOrientation(null)
    .withHomeAndEnd()
    .withTypeAhead();
  /** The row that holds the tab stop: the last one focused, or the first that explains itself. */
  private readonly current = signal<LgLedgerRowComponent | null>(null);
  private readonly tabStop = computed(() => {
    const rows = this.explaining();
    const current = this.current();
    return current && rows.includes(current) ? current : (rows[0] ?? null);
  });

  constructor() {
    super();
    const changes = this.keys.change.subscribe(() => this.current.set(this.keys.activeItem));
    inject(DestroyRef).onDestroy(() => {
      changes.unsubscribe();
      this.keys.destroy();
    });
  }

  isTabStop(row: LgLedgerRowComponent): boolean {
    return this.tabStop() === row;
  }

  focused(row: LgLedgerRowComponent): void {
    this.keys.updateActiveItem(row);
    this.current.set(row);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' || event.key === 'Tab') return;
    if (!this.keys.activeItem && this.tabStop()) this.keys.updateActiveItem(this.tabStop()!);
    this.keys.onKeydown(event);
  }
}

/** The Ledger and its rows, for a standalone `imports` array. */
export const LG_LEDGER = [LgLedgerComponent, LgLedgerRowComponent] as const;
