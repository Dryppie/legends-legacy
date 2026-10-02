import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  Signal,
  computed,
  contentChildren,
  forwardRef,
  inject,
  input,
  model,
  OnInit,
  signal,
} from '@angular/core';
import { FocusKeyManager, FocusableOption } from '@angular/cdk/a11y';
import { LgDensity, lgUniqueId } from '../../core/grimoire-core';

/** `primary`: what you are browsing (engraved section tabs). `secondary`: how it is filtered (small capitals). */
export type LgTabsLevel = 'primary' | 'secondary';

/** What a tab reads from the strip it sits in: its level. */
abstract class LgTabsLevelRef {
  abstract readonly level: Signal<LgTabsLevel>;
}

/** How a Tabs coordinates its tabs and panels. */
abstract class LgTabGroup extends LgTabsLevelRef {
  abstract isSelected(tab: LgTabComponent): boolean;
  abstract isTabStop(tab: LgTabComponent): boolean;
  abstract select(tab: LgTabComponent): void;
  abstract panelId(key: string): string | null;
  abstract tabId(key: string): string | null;
  abstract isShown(key: string): boolean;
}

/**
 * A tab: a native button in an lg-tabs. Its words are its content; `count` follows them.
 * `<button lgTab key="creatures">Creatures</button>`
 */
@Component({
  selector: 'button[lgTab]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    type: 'button',
    role: 'tab',
    '[class]': "'lg-tab lg-tab--' + (group?.level() ?? 'primary')",
    '[id]': 'id',
    '[attr.aria-selected]': 'selected()',
    '[attr.aria-controls]': 'group?.panelId(key()) ?? null',
    '[attr.tabindex]': 'group && !group.isTabStop(this) ? -1 : 0',
    '(click)': 'group?.select(this)',
  },
  template: `<ng-content />@if (count() != null) {<span class="lg-tab__count">{{ count() }}</span>}`,
  styleUrl: './tab.component.css',
})
export class LgTabComponent implements FocusableOption, OnInit {
  /** Which tab this is: the Tabs' `selected` value, and its panel's `key`. */
  readonly key = input.required<string>();
  /** A count after the words, 0 included. */
  readonly count = input<number | null>();

  protected readonly group = inject(LgTabGroup, { optional: true });
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  readonly id = lgUniqueId('lg-tab');
  /** Whether its key is set: tabs in a @for are created one after another, so the Tabs compare only settled ones. */
  readonly settled = signal(false);
  protected readonly selected = computed(() => !!this.group?.isSelected(this));

  ngOnInit(): void {
    this.settled.set(true);
  }

  /** FocusableOption: the key manager moves focus here. */
  focus(): void {
    this.el.focus();
  }

  /** Whether the element is this tab or inside it. */
  contains(el: Element): boolean {
    return this.el.contains(el);
  }
}

/**
 * A tab's panel: shown while its tab is selected, labelled by it. `<lg-tab-panel key="creatures">…</lg-tab-panel>`
 * inside the lg-tabs, after the tabs. Its content stays rendered while hidden.
 */
@Component({
  selector: 'lg-tab-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'tabpanel',
    tabindex: '0',
    class: 'lg-tab-panel',
    '[id]': 'id',
    '[attr.aria-labelledby]': 'group?.tabId(key()) ?? null',
    '[attr.hidden]': "group && !group.isShown(key()) ? '' : null",
  },
  template: '<ng-content />',
  styles: `
    :host { display: block; outline: 2px solid transparent; }
    :host([hidden]) { display: none; }
    :host(:focus-visible) { box-shadow: var(--lg-focus-ring); border-radius: var(--lg-radius-control); }
  `,
})
export class LgTabPanelComponent implements OnInit {
  /** The key of the tab it belongs to. */
  readonly key = input.required<string>();
  protected readonly group = inject(LgTabGroup, { optional: true });
  readonly id = lgUniqueId('lg-tab-panel');
  /** Whether its key is set (see LgTabComponent.settled). */
  readonly settled = signal(false);

  ngOnInit(): void {
    this.settled.set(true);
  }
}

/**
 * The tabs: a strip of `button[lgTab]`s, then the `lg-tab-panel`s they show, if any. A filter strip has no panels.
 *
 *   <lg-tabs label="Archive" [(selected)]="section">
 *     <button lgTab key="creatures">Creatures</button>
 *     <button lgTab key="regions">Regions</button>
 *     <lg-tab-panel key="creatures">…</lg-tab-panel>
 *     <lg-tab-panel key="regions">…</lg-tab-panel>
 *   </lg-tabs>
 *
 * One tab stop, the selected tab: Left and Right move and select, wrapping; Home and End jump (the CDK's
 * FocusKeyManager). For tabs that are routes, use `nav[lgTabNav]` with `a[lgTabLink]`s.
 */
@Component({
  selector: 'lg-tabs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: LgTabGroup, useExisting: forwardRef(() => LgTabsComponent) },
    { provide: LgTabsLevelRef, useExisting: forwardRef(() => LgTabsComponent) },
  ],
  host: { class: 'lg-tabs', '[attr.data-density]': 'density() ?? null' },
  template: `
    <div
      [class]="'lg-tabs__list lg-tabs__list--' + level()"
      role="tablist"
      [attr.aria-label]="label() ?? null"
      (keydown)="onKeydown($event)"
    >
      <ng-content select="[lgTab]" />
    </div>
    <ng-content />
  `,
  styleUrl: './tabs.component.css',
})
export class LgTabsComponent extends LgTabGroup {
  /** The selected tab's key; two-way bindable: `[(selected)]`. */
  readonly selected = model.required<string>();
  readonly level = input<LgTabsLevel>('primary');
  readonly density = input<LgDensity>();
  readonly label = input<string>();

  private readonly tabs = contentChildren(LgTabComponent);
  private readonly panels = contentChildren(LgTabPanelComponent);
  private readonly keys = new FocusKeyManager(this.tabs, inject(Injector))
    .withHorizontalOrientation('ltr')
    .withVerticalOrientation(false)
    .withWrap()
    .withHomeAndEnd();
  /** The tab that holds the tab stop: the selected one, else the first. */
  private readonly tabStop = computed(() => {
    const tabs = this.tabs();
    return tabs.find((tab) => this.isSelected(tab)) ?? tabs[0] ?? null;
  });

  constructor() {
    super();
    // Selection follows focus: the tab the keys move to is selected.
    const changes = this.keys.change.subscribe(() => {
      const tab = this.keys.activeItem;
      if (tab) this.select(tab);
    });
    inject(DestroyRef).onDestroy(() => {
      changes.unsubscribe();
      this.keys.destroy();
    });
  }

  isSelected(tab: LgTabComponent): boolean {
    return tab.settled() && tab.key() === this.selected();
  }

  isTabStop(tab: LgTabComponent): boolean {
    return this.tabStop() === tab;
  }

  select(tab: LgTabComponent): void {
    this.selected.set(tab.key());
  }

  panelId(key: string): string | null {
    return this.panels().find((panel) => panel.settled() && panel.key() === key)?.id ?? null;
  }

  tabId(key: string): string | null {
    return this.tabs().find((tab) => tab.settled() && tab.key() === key)?.id ?? null;
  }

  isShown(key: string): boolean {
    return key === this.selected();
  }

  protected onKeydown(event: KeyboardEvent): void {
    const tab = this.tabs().find((t) => t.contains(event.target as Element));
    if (!tab) return;
    if (this.keys.activeItem !== tab) this.keys.updateActiveItem(tab);
    this.keys.onKeydown(event);
  }
}

/**
 * Route tabs: a `nav` of `a[lgTabLink]`s, each with `routerLink`, `routerLinkActive` and
 * `ariaCurrentWhenActive="page"`. The current one is the one marked `aria-current="page"`; Grimoire never reads the
 * Router. Each link is its own tab stop, as links are.
 */
@Component({
  selector: 'nav[lgTabNav]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgTabsLevelRef, useExisting: forwardRef(() => LgTabNavComponent) }],
  host: {
    '[class]': "'lg-tabs__list lg-tabs__list--' + level()",
    '[attr.aria-label]': 'label() ?? null',
    '[attr.data-density]': 'density() ?? null',
  },
  template: '<ng-content />',
  styles: `
    :host { display: flex; align-items: center; flex-wrap: wrap; }
    :host(.lg-tabs__list--primary) { gap: 0; flex-wrap: nowrap; overflow-x: auto; scrollbar-width: none; max-width: 100%; }
    :host(.lg-tabs__list--secondary) { gap: var(--lg-space-5); }
  `,
})
export class LgTabNavComponent extends LgTabsLevelRef {
  readonly level = input<LgTabsLevel>('primary');
  readonly density = input<LgDensity>();
  /** Names the navigation ("Archive sections"). */
  readonly label = input<string>();
}

/** A route tab: a native link in a `nav[lgTabNav]`. `<a lgTabLink routerLink="creatures" routerLinkActive ariaCurrentWhenActive="page">` */
@Component({
  selector: 'a[lgTabLink]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "'lg-tab lg-tab--' + (strip?.level() ?? 'primary')" },
  template: `<ng-content />@if (count() != null) {<span class="lg-tab__count">{{ count() }}</span>}`,
  styleUrl: './tab.component.css',
})
export class LgTabLinkComponent {
  /** A count after the words, 0 included. */
  readonly count = input<number | null>();
  protected readonly strip = inject(LgTabsLevelRef, { optional: true });
}

/** The Tabs, their tabs and panels, and the route tabs, for a standalone `imports` array. */
export const LG_TABS = [
  LgTabsComponent,
  LgTabComponent,
  LgTabPanelComponent,
  LgTabNavComponent,
  LgTabLinkComponent,
] as const;
