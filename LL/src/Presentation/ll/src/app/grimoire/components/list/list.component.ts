import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  Signal,
  afterEveryRender,
  booleanAttribute,
  computed,
  contentChild,
  contentChildren,
  forwardRef,
  inject,
  input,
  model,
  OnInit,
  signal,
} from '@angular/core';
import { FocusKeyManager, FocusableOption } from '@angular/cdk/a11y';
import { LgDensity, LgRarity, lgCx, lgUniqueId } from '../../core/grimoire-core';
import { LgRarityComponent } from '../../primitives/rarity/rarity.component';
import { LG_NONE, LG_TIMES, lgFormatNumber } from '../../core/grimoire-format';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LgIconName } from '../../core/grimoire-icons';
import { LgValuePipe } from '../../core/grimoire-numerals';
import { lgLive } from '../../core/grimoire-motion';
import { LgTagComponent } from '../../primitives/tag/tag.component';
import { LgBlockedController, LgBlockedTip, lgBlockedSpoken } from '../../core/grimoire-blocked';
import { LgBlockedState, lgBlockedReason, lgReadyWords } from '../../core/grimoire-states';

/** The columns a List shares with its rows: a column shows when any row uses it. */
export interface LgListColumns {
  thumb: boolean;
  qty: boolean;
  amount: boolean;
  trail: boolean;
}

/** `standard`: rows in panels, with columns. `scene`: the browsable name list over stage art (was EntryList, D-138). */
export type LgListVariant = 'standard' | 'scene';
export type LgListRhythm = 'separators' | 'zebra' | 'spacing';
/** The blocked states a row can be in: each takes a plain `reason`. */
export type LgListRowState = Extract<LgBlockedState, 'locked' | 'unavailable' | 'restricted'>;

/** What can take focus inside a trailing region. */
const FOCUSABLE = 'button, a[href], input, select, textarea, [tabindex]';

/** How a List's rows coordinate: the shared columns, the variant, the selection and the one tab stop. */
abstract class LgListRows {
  abstract readonly variant: Signal<LgListVariant>;
  abstract readonly selectable: Signal<boolean>;
  abstract readonly columns: Signal<LgListColumns>;
  abstract tracksSelection(): boolean;
  abstract isSelected(row: LgListRowComponent): boolean;
  abstract isTabStop(row: LgListRowComponent): boolean;
  abstract focused(row: LgListRowComponent): void;
  abstract select(row: LgListRowComponent): void;
}

/** What a row's action and trailing region read from their row. */
abstract class LgListRowRef {
  abstract readonly blockedId: string;
  abstract readonly blockedOptions: Signal<LgBlockedTip | null>;
  abstract labelledBy(): string;
  abstract hasAction(): boolean;
  abstract isTabStop(): boolean;
  abstract pressed(): boolean | null;
  abstract activate(): void;
  abstract actionFocused(): void;
}

/**
 * A row's action: a native `button` or `a` that covers the row, so the whole row is its target. It is labelled by the
 * row's name and rarity, and in a List that tracks a selection a press selects the row (`aria-pressed`).
 * `<li lgListRow name="Ember Fang"><button lgListRowAction (click)="inspect(item)"></button></li>`
 */
@Component({
  selector: 'button[lgListRowAction], a[lgListRowAction]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-listrow__action',
    '[attr.type]': "isButton ? 'button' : null",
    '[attr.aria-labelledby]': 'row.labelledBy()',
    '[attr.aria-pressed]': 'isButton ? row.pressed() : null',
    '[attr.tabindex]': 'row.isTabStop() ? 0 : -1',
    '[attr.aria-disabled]': "row.blockedOptions() ? 'true' : null",
    '[attr.aria-describedby]': 'blocked.describedBy()',
    '(focus)': 'onFocus()',
    '(blur)': 'blocked.blur()',
    '(mouseenter)': 'blocked.enter()',
    '(mouseleave)': 'blocked.leave()',
    '(click)': 'row.activate()',
  },
  template: '',
  styles: `
    /* It covers the row; the row draws the focus ring round itself, so the ring is the row's (list-row.component.css). */
    :host {
      position: absolute; inset: 0; margin: 0; padding: 0; border: 0; border-radius: inherit;
      background: none; color: inherit; font: inherit; cursor: pointer; outline: 2px solid transparent;
    }
    :host([aria-disabled='true']) { cursor: not-allowed; }
  `,
})
export class LgListRowActionComponent {
  protected readonly row = inject(LgListRowRef);
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  protected readonly blocked = new LgBlockedController(this.el, this.row.blockedOptions, () => this.row.blockedId);

  constructor() {
    // A blocked row's action does not act: stop the press before (click) handlers on the element hear it.
    this.el.addEventListener(
      'click',
      (event) => {
        if (!this.row.blockedOptions()) return;
        event.stopImmediatePropagation();
        event.preventDefault();
        this.blocked.press(event);
      },
      true,
    );
  }

  protected onFocus(): void {
    this.row.actionFocused();
    this.blocked.focus();
  }
}

/**
 * A row's trailing region: one small action (a `sm` Button) or a status (Presence) at the row's end. In a row with an
 * action its controls leave the Tab order: Right reaches them from the row, Left goes back.
 */
@Component({
  selector: 'lg-list-row-trailing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-listrow__trail' },
  template: '<ng-content />',
  styles: `
    :host {
      position: relative; z-index: var(--lg-z-raised); justify-self: end;
      display: inline-flex; align-items: center; gap: var(--lg-inline-sm);
    }
  `,
})
export class LgListRowTrailingComponent {
  readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly row = inject(LgListRowRef, { optional: true });

  constructor() {
    afterEveryRender(() => {
      if (!this.row?.hasAction()) return;
      this.el.querySelectorAll(FOCUSABLE).forEach((el) => el.setAttribute('tabindex', '-1'));
    });
  }

  /** The first control in it, or null. */
  first(): HTMLElement | null {
    return this.el.querySelector<HTMLElement>(FOCUSABLE);
  }
}

/**
 * The list row: one line of an inventory, a ranking, a member list, an order book or a scene's name list, inside an
 * lg-list. `<li lgListRow name="Ember Fang" rarity="Epic" [quantity]="1" amount="9,400"></li>`
 *
 * Tags are projected `lg-tag`s; one action covers the row (`button[lgListRowAction]`); `lg-list-row-trailing` holds
 * what sits at its end. In a selectable List the row itself is the option. A row in a blocked `state` (locked) stays in
 * reach of the keyboard and shows its `reason`, but is never selected and its action does not act.
 */
@Component({
  selector: 'li[lgListRow]',
  imports: [LgIconComponent, LgRarityComponent, LgTagComponent, LgValuePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgListRowRef, useExisting: forwardRef(() => LgListRowComponent) }],
  host: {
    '[class]': 'classes()',
    '[attr.data-density]': 'density() ?? null',
    '[attr.role]': "selectable() ? 'option' : null",
    '[attr.aria-selected]': 'selectable() ? selected() : null',
    '[attr.aria-disabled]': "selectable() && blockedOptions() ? 'true' : null",
    '[attr.aria-describedby]': 'selectable() ? optionBlocked.describedBy() : null',
    '[attr.tabindex]': 'selectable() ? (isTabStop() ? 0 : -1) : null',
    '(focus)': 'onFocus()',
    '(blur)': 'optionBlocked.blur()',
    '(mouseenter)': 'optionBlocked.enter()',
    '(mouseleave)': 'optionBlocked.leave()',
    '(click)': 'onClick($event)',
  },
  template: `
    <ng-content select="[lgListRowAction]" />
    @if (cols().thumb) {
      <span class="lg-listrow__thumb" aria-hidden="true"
        >@if (image(); as src) {<img [src]="src" alt="" />} @else if (icon(); as iconName) {<lg-icon
            [name]="iconName"
            [size]="20"
          />}</span
      >
    }
    <span class="lg-listrow__main"
      ><span class="lg-listrow__name" [id]="nameId" [attr.aria-hidden]="hasAction() ? 'true' : null">{{
        name()
      }}</span
      >@if (rarity(); as r) {<lg-rarity
          class="lg-listrow__code"
          [rarity]="r"
          [nameId]="rarityId"
          [attr.aria-hidden]="hasAction() ? 'true' : null"
        />}@if (state(); as s) {<span class="lg-listrow__tags"><lg-tag [state]="s" ariaHidden /></span>}<span
        class="lg-listrow__tags"
        [hidden]="!tags().length || !!state()"
        ><ng-content select="lg-tag" /></span
      >@if (meta()) {<span class="lg-listrow__meta">{{ meta() }}</span>}@if (readyWords(); as words) {<span
          class="lg-attention lg-listrow__attention"
          aria-hidden="true"
        ></span
        ><span class="lg-sr">, {{ words }}</span>}</span
    >
    @if (cols().qty) {
      <span class="lg-listrow__qty"
        >@if (quantity() != null) {<span class="lg-sr">Quantity </span>{{ times }}{{ format(quantity()) }}} @else {{{
          none
        }}}</span
      >
    }
    @if (cols().amount) {
      @let v = (amount() === undefined ? null : liveAmount.value()) | lgValue;
      <span [class]="amountClass()">{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></span>
    }
    @if (cols().trail && !trailing()) {
      <span class="lg-listrow__trail"></span>
    }
    <ng-content select="lg-list-row-trailing" />
    @if (blockedOptions(); as o) {
      <span class="lg-sr lg-blocked__desc" [id]="blockedId" aria-hidden="true">{{ spoken(o) }}</span>
    }
  `,
  styleUrl: './list-row.component.css',
})
export class LgListRowComponent extends LgListRowRef implements FocusableOption, OnInit {
  readonly name = input.required<string>();
  /** Which row this is, for the List's selection; the name when unset. */
  readonly key = input<string>();
  readonly rarity = input<LgRarity>();
  readonly icon = input<LgIconName>();
  readonly image = input<string>();
  readonly meta = input<string>();
  readonly quantity = input<number | null>();
  /** The amount column (a price, a score); leave it unset (undefined) for none, null shows —. */
  readonly amount = input<number | string | null | undefined>(undefined);
  /** The amount changes by itself (a refreshed price): it takes the live-update mark. */
  readonly live = input(false, { transform: booleanAttribute });
  readonly muted = input(false, { transform: booleanAttribute });
  /** Blocked: in reach, never selected, its reason in the tip. Locked takes how it unlocks as its `reason`. */
  readonly state = input<LgListRowState>();
  readonly reason = input<string>();
  /** The attention diamond at the row's end: true, or the words screen readers hear ("1 point to spend"). */
  readonly ready = input<boolean | string>();
  readonly density = input<LgDensity>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly list = inject(LgListRows, { optional: true });
  private readonly action = contentChild(LgListRowActionComponent, { read: ElementRef });
  protected readonly trailing = contentChild(LgListRowTrailingComponent);
  protected readonly tags = contentChildren(LgTagComponent);

  protected readonly format = lgFormatNumber;
  protected readonly times = LG_TIMES;
  protected readonly none = LG_NONE;
  protected readonly spoken = lgBlockedSpoken;
  readonly nameId = lgUniqueId('lg-listrow-name');
  readonly rarityId = this.nameId + '-rarity';
  readonly blockedId = this.nameId + '-why';

  /**
   * Whether its inputs are set. Rows in a @for are created one after another, so while one renders, the next may not
   * have its required name yet: the List compares only the keys of rows that have settled.
   */
  readonly settled = signal(false);
  /** The row's identity in the List's selection. */
  readonly id = computed(() => this.key() ?? this.name());
  /** The columns this row would need on its own. */
  readonly ownColumns = computed<LgListColumns>(() => ({
    thumb: !!(this.icon() || this.image()),
    qty: this.quantity() != null,
    amount: this.amount() !== undefined,
    trail: !!this.trailing(),
  }));
  protected readonly cols = computed(() => this.list?.columns() ?? this.ownColumns());
  protected readonly selectable = computed(() => !!this.list?.selectable());
  readonly blockedOptions = computed<LgBlockedTip | null>(() => {
    const s = this.state();
    if (!s) return null;
    const br = lgBlockedReason(s, { reason: this.reason() }, `List row "${this.name()}"`);
    return { reason: br.reason, word: br.word, tone: br.tone, spoken: br.spoken, place: 'end' };
  });
  /** Selected in its List; a blocked row never is. */
  readonly selected = computed(() => !this.blockedOptions() && !!this.list?.isSelected(this));
  protected readonly readyWords = computed(() => (this.state() ? null : lgReadyWords(this.ready())));
  protected readonly optionBlocked = new LgBlockedController(
    this.el,
    () => (this.selectable() ? this.blockedOptions() : null),
    () => this.blockedId,
  );

  protected readonly liveAmount = lgLive(
    () => this.amount(),
    () => ({ mark: this.live() }),
  );
  protected readonly amountClass = computed(() =>
    lgCx('lg-listrow__value', this.live() && this.liveAmount.className()),
  );
  protected readonly classes = computed(() => {
    const r = this.rarity();
    return lgCx(
      'lg-listrow',
      this.list?.variant() === 'scene' && 'lg-listrow--scene',
      r && 'lg-listrow--' + r.toLowerCase(),
      this.selected() && 'is-selected',
      this.muted() && 'is-muted',
      this.state() && 'is-blocked',
      this.hasAction() && 'has-action',
      this.selectable() && 'is-option',
    );
  });

  ngOnInit(): void {
    this.settled.set(true);
  }

  hasAction(): boolean {
    return !!this.action();
  }

  isTabStop(): boolean {
    return this.list ? this.list.isTabStop(this) : true;
  }

  labelledBy(): string {
    return this.rarity() ? `${this.nameId} ${this.rarityId}` : this.nameId;
  }

  pressed(): boolean | null {
    return this.list?.tracksSelection() ? this.selected() : null;
  }

  activate(): void {
    if (!this.blockedOptions() && this.list?.tracksSelection()) this.list.select(this);
  }

  actionFocused(): void {
    this.list?.focused(this);
  }

  /** FocusableOption: the key manager moves focus here — the row itself in a selectable List, else its action. */
  focus(): void {
    if (this.selectable()) this.el.focus();
    else (this.action()?.nativeElement as HTMLElement | undefined)?.focus();
  }

  /** FocusableOption: typeahead matches the name. */
  getLabel(): string {
    return this.name();
  }

  /** Whether the element is inside this row. */
  contains(el: Element): boolean {
    return this.el.contains(el);
  }

  /** Whether the element is inside this row's trailing region. */
  inTrailing(el: Element): boolean {
    return !!this.trailing()?.el.contains(el);
  }

  /** Moves focus into the trailing region; false when it has no control. */
  focusTrailing(): boolean {
    const target = this.trailing()?.first();
    target?.focus();
    return !!target;
  }

  /** A press on a blocked row: pin its reason and say it again. */
  pressBlocked(event: Event): void {
    this.optionBlocked.press(event);
  }

  protected onFocus(): void {
    if (!this.selectable()) return;
    this.list?.focused(this);
    this.optionBlocked.focus();
  }

  protected onClick(event: Event): void {
    if (!this.selectable()) return;
    if (this.blockedOptions()) this.optionBlocked.press(event);
    else this.list?.select(this);
  }
}

/**
 * The list: rows that share columns through CSS subgrid — thumbnail, name, quantity, amount, trailing region. A column
 * appears only if some row uses it, and every row then keeps its cell, so amounts line up.
 *
 * Two kinds. A list of things to act on (`role="list"`): rows with an action are one tab stop; Up, Down, Home, End and
 * a name's first letters move between them, Right moves into a row's trailing region and Left back. A `selectable`
 * list (`role="listbox"`): the rows are options, Up and Down wrap and the selection follows focus; a blocked row is
 * reached but never selected. Either can track the selection as `[(selected)]`, the selected row's `key`.
 * `variant="scene"` is the browsable name list over stage art: display-face names, faded ends; give it a height.
 */
@Component({
  selector: 'lg-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgListRows, useExisting: forwardRef(() => LgListComponent) }],
  host: {
    '[class]': 'hostClasses()',
    '[attr.data-density]': 'density() ?? null',
  },
  template: `
    <ul
      [class]="classes()"
      [attr.role]="selectable() ? 'listbox' : 'list'"
      [attr.aria-label]="label() ?? null"
      [style.grid-template-columns]="template()"
      (keydown)="onKeydown($event)"
    >
      <ng-content />
    </ul>
  `,
  styleUrl: './list.component.css',
})
export class LgListComponent extends LgListRows {
  readonly label = input<string>();
  readonly density = input<LgDensity>();
  readonly rhythm = input<LgListRhythm>('separators');
  readonly variant = input<LgListVariant>('standard');
  /** The rows are options: role listbox, and the selection follows focus. */
  readonly selectable = input(false, { transform: booleanAttribute });
  /** The selected row's key; two-way bindable: `[(selected)]`. Unset, the List tracks no selection. */
  readonly selected = model<string | null | undefined>(undefined);
  /** A scene list's ends fade out. */
  readonly fade = input(true, { transform: booleanAttribute });

  private readonly rows = contentChildren(LgListRowComponent);
  /** The rows the keys move between: every row in a selectable List, else the rows with an action. */
  private readonly reachable = computed(() =>
    this.selectable() ? this.rows() : this.rows().filter((row) => row.hasAction()),
  );
  private readonly keys = new FocusKeyManager(this.reachable, inject(Injector))
    .withVerticalOrientation()
    .withHorizontalOrientation(null)
    .withHomeAndEnd()
    .withTypeAhead();
  /** The row that holds the tab stop: the last one focused, else the selected one, else the first. */
  private readonly current = signal<LgListRowComponent | null>(null);
  private readonly tabStop = computed(() => {
    const rows = this.reachable();
    const current = this.current();
    if (current && rows.includes(current)) return current;
    return rows.find((row) => this.isSelected(row)) ?? rows[0] ?? null;
  });

  protected readonly hostClasses = computed(() =>
    lgCx(this.variant() === 'scene' && 'lg-list--scene', this.variant() === 'scene' && this.fade() && 'lg-list--fade'),
  );
  protected readonly classes = computed(() =>
    lgCx('lg-list', this.rhythm() !== 'separators' && 'lg-list--' + this.rhythm()),
  );
  readonly columns = computed<LgListColumns>(() => {
    const has = { thumb: false, qty: false, amount: false, trail: false };
    for (const row of this.rows()) {
      const c = row.ownColumns();
      has.thumb ||= c.thumb;
      has.qty ||= c.qty;
      has.amount ||= c.amount;
      has.trail ||= c.trail;
    }
    return has;
  });
  protected readonly template = computed(() => {
    const has = this.columns();
    return (
      (has.thumb ? 'auto ' : '') +
      'minmax(0, 1fr)' +
      (has.qty ? ' auto' : '') +
      (has.amount ? ' auto' : '') +
      (has.trail ? ' auto' : '')
    );
  });

  constructor() {
    super();
    const changes = this.keys.change.subscribe(() => {
      const row = this.keys.activeItem;
      this.current.set(row);
      if (row && this.selectable() && !row.blockedOptions()) this.select(row);
    });
    inject(DestroyRef).onDestroy(() => {
      changes.unsubscribe();
      this.keys.destroy();
    });
  }

  tracksSelection(): boolean {
    return this.selected() !== undefined;
  }

  isSelected(row: LgListRowComponent): boolean {
    const selected = this.selected();
    return selected != null && row.settled() && selected === row.id();
  }

  isTabStop(row: LgListRowComponent): boolean {
    return this.tabStop() === row;
  }

  focused(row: LgListRowComponent): void {
    this.keys.updateActiveItem(row);
    this.current.set(row);
  }

  select(row: LgListRowComponent): void {
    this.selected.set(row.id());
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Tab' || event.key === 'Escape') return;
    const target = event.target as Element;
    const row = this.rows().find((r) => r.contains(target));
    if (!row) return;
    const selectable = this.selectable();
    if (!selectable && row.inTrailing(target)) {
      if (event.key === 'ArrowLeft' && row.hasAction()) {
        event.preventDefault();
        row.focus();
        return;
      }
      if (event.key === 'Enter' || event.key === ' ' || event.key === 'ArrowRight') return;
    } else if (!selectable && event.key === 'ArrowRight') {
      if (row.focusTrailing()) event.preventDefault();
      return;
    }
    if (event.key === 'Enter' || event.key === ' ') {
      // An option answers Enter and Space itself; an action is a native control and does.
      if (!selectable) return;
      event.preventDefault();
      if (row.blockedOptions()) row.pressBlocked(event);
      else this.select(row);
      return;
    }
    // A row without an action is out of the keys' reach: its trailing controls are ordinary tab stops.
    if (!this.reachable().includes(row)) return;
    if (this.keys.activeItem !== row) this.keys.updateActiveItem(row);
    this.keys.withWrap(selectable);
    this.keys.onKeydown(event);
  }
}

/** The List, its rows and their regions, for a standalone `imports` array. */
export const LG_LIST = [
  LgListComponent,
  LgListRowComponent,
  LgListRowActionComponent,
  LgListRowTrailingComponent,
] as const;
