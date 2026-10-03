import {
  AfterContentChecked,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Signal,
  booleanAttribute,
  computed,
  forwardRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LG_ICONS, LgIconName } from '../../core/grimoire-icons';
import { LG_STATES, lgBlockedReason, lgReadyWords } from '../../core/grimoire-states';
import { LgBlockedController, LgBlockedTip, lgBlockedSpoken } from '../../core/grimoire-blocked';
import { LgTooltipController } from '../../primitives/tooltip/tooltip.directive';

/** What a NavRail tells its sections, items and regions: whether it is the compact rail. */
export abstract class LgNavRailRef {
  abstract readonly compact: Signal<boolean>;
}

/** The rail's header: the Logo and wordmark, or in the game the current action as an Activity (D-109). */
@Component({
  selector: 'lg-nav-rail-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-rail__header' },
  template: '<ng-content />',
  styles: `
    :host { display: flex; align-items: center; gap: var(--lg-space-3); padding-left: var(--lg-space-1); }
    /* In a GameShell the compact rail's header is a band as tall as the TopBar, its Activity centred in it, the same
       rule under it over the frame's backdrop (D-115, D-117). The rail's class, not its injector: a header may be
       passed through a component of the host's own before it reaches the rail. */
    @container lg-shell (min-width: 60rem) {
      :host-context(.lg-rail--compact) {
        align-self: stretch; justify-content: center; min-height: var(--lg-topbar-height); padding-left: 0;
        margin-inline: calc(-1 * var(--lg-space-1));
        border-bottom: var(--lg-rail-header-rule, none);
      }
    }
  `,
})
export class LgNavRailHeaderComponent {}

/** The rail's footer, at its foot: extras such as a link to the patch notes. */
@Component({
  selector: 'lg-nav-rail-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-rail__footer' },
  template: '<ng-content />',
  styles: ':host { display: block; margin-top: auto; }',
})
export class LgNavRailFooterComponent {}

/** One of the rail's sections (Character, World, City, System): a group named by its label, holding its items. */
@Component({
  selector: 'lg-nav-section',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-rail__section',
    role: 'group',
    '[attr.aria-labelledby]': 'labelId',
    '[class.is-compact]': 'rail?.compact()',
  },
  template: `<div class="lg-rail__group" [attr.id]="labelId">{{ label() }}</div
    ><div class="lg-rail__items"><ng-content /></div>`,
  styleUrl: './nav-section.component.css',
})
export class LgNavSectionComponent {
  /** "Character": a quiet capital label, three words at most. */
  readonly label = input.required<string>();
  protected readonly rail = inject(LgNavRailRef, { optional: true });
  protected readonly labelId = lgUniqueId('lgns');
}

/**
 * A destination: `<a lgNavItem routerLink="/game/character/inventory" routerLinkActive ariaCurrentWhenActive="page"
 * icon="inventory" description="Items, gear, misc">Inventory</a>`. The content is its title. The current item is the
 * one whose `aria-current` is "page" — set by `routerLinkActive`, or by you when the place isn't a route. A press is the
 * link, and your (click) hears it.
 *
 * `locked` (with its `reason`, how it unlocks) keeps it in the Tab order: it says "Locked", its condition opens beside
 * it on hover and focus, and a click or Enter shows the condition instead of navigating — your (click) never runs. A
 * locked item needs no link at all. (Not `state="locked"`: RouterLink, on the same element, has a `state` input.)
 */
@Component({
  selector: 'a[lgNavItem]',
  imports: [LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-rail__item',
    '[class.is-locked]': 'locked()',
    '[class.is-compact]': 'compact()',
    '[class.has-description]': '!!description()',
    // A locked item may have no href: it stays a link in the Tab order.
    '[attr.role]': "locked() ? 'link' : null",
    '[attr.tabindex]': "locked() ? '0' : null",
    '[attr.aria-disabled]': "locked() ? 'true' : null",
    '[attr.aria-describedby]': 'blockedTip.describedBy()',
    '(mouseenter)': 'blockedTip.enter(); tooltip.enter()',
    '(mouseleave)': 'blockedTip.leave(); tooltip.leave()',
    '(focus)': 'blockedTip.focus(); tooltip.focus()',
    '(blur)': 'blockedTip.blur(); tooltip.blur()',
    '(keydown.enter)': 'onEnter($event)',
  },
  template: `@if (icon(); as name) {<lg-icon [name]="name" [size]="20" />}<span class="lg-rail__text"
      ><span class="lg-rail__title"><ng-content /></span
      >@if (description()) {<span class="lg-rail__desc">{{ description() }}</span>}</span
    >@if (locked()) {<span class="lg-rail__lock" aria-hidden="true"
        >@if (lockIcon) {<lg-icon [name]="lockIcon" [size]="12" />} @else {{{ lockedWord }}}</span
      >} @else if (badge() != null && badge() !== '') {<span class="lg-rail__badge" [attr.aria-label]="badgeLabel() || null">{{
        badge()
      }}</span>} @else if (ready()) {<span class="lg-attention lg-rail__attention" aria-hidden="true"></span
      ><span class="lg-sr">, {{ badgeLabel() || readyWords(ready()) }}</span>}@if (why(); as w) {<span
        class="lg-sr lg-blocked__desc"
        [attr.id]="whyId"
        aria-hidden="true"
        >{{ spoken(w) }}</span
      >}`,
  styleUrl: './nav-item.component.css',
})
export class LgNavItemComponent implements AfterContentChecked {
  readonly icon = input<LgIconName>();
  /** A short line under the title ("Stats, vitals, loadout"), so a destination is never known by its icon alone
   *  (D-104). Hidden in compact. */
  readonly description = input<string>();
  /** A count: new items, quests ready to turn in. */
  readonly badge = input<string | number | null>();
  /** What the badge, or the ready mark, says to screen readers: "3 new items". */
  readonly badgeLabel = input<string>();
  /** Something waiting with nothing to count: the attention diamond at the item's end. true, or the words ("Quest
   *  ready"). A count `badge` comes first, and Locked before both (Standards · State combinations). */
  readonly ready = input<boolean | string>();
  /** Locked (Standards · States): unavailable but in reach. */
  readonly locked = input(false, { transform: booleanAttribute });
  /** Locked: how it unlocks. */
  readonly reason = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly rail = inject(LgNavRailRef, { optional: true });
  /** The title's words, read from the content: the compact rail's tooltip and the reason tip's heading. */
  private readonly name = signal('');

  protected readonly spoken = lgBlockedSpoken;
  protected readonly readyWords = lgReadyWords;
  protected readonly lockedWord = LG_STATES.locked.word;
  /** The 12px lock marker once it is drawn (Foundations · Iconography); until then the word. */
  protected readonly lockIcon = ('lock' in LG_ICONS ? 'lock' : null) as LgIconName | null;
  protected readonly whyId = lgUniqueId('lgnav') + '-why';

  protected readonly compact = computed(() => !!this.rail?.compact());
  protected readonly why = computed<LgBlockedTip | null>(() =>
    this.locked()
      ? {
          reason: lgBlockedReason('locked', { reason: this.reason() }, `NavRail item "${this.name()}"`).reason,
          word: 'Locked',
          place: 'end',
          // In compact the title isn't shown, so the tip names the item.
          title: this.compact() ? this.name() : null,
        }
      : null,
  );
  protected readonly blockedTip = new LgBlockedController(this.el, this.why, () => this.whyId);
  /** Compact: the title is the tooltip. It is already the item's name, so the tooltip adds no description. */
  protected readonly tooltip = new LgTooltipController(
    this.el,
    () => (this.compact() && !this.locked() ? this.name() || null : null),
    { place: () => 'end', description: () => '' },
  );

  constructor() {
    // A locked item does not navigate: stop the press before the link and your (click) hear it, and give the reason.
    this.el.addEventListener(
      'click',
      (event) => {
        if (!this.locked()) return;
        event.stopImmediatePropagation();
        event.preventDefault();
        this.blockedTip.press(event);
      },
      true,
    );
  }

  ngAfterContentChecked(): void {
    const title = this.el.querySelector(':scope > .lg-rail__text > .lg-rail__title');
    const words = (title?.textContent ?? '').trim();
    if (words !== this.name()) this.name.set(words);
  }

  /** Enter on a locked item without an href, which the browser doesn't turn into a click: show the condition. */
  protected onEnter(event: Event): void {
    if (this.locked() && !this.el.hasAttribute('href')) this.blockedTip.press(event);
  }
}

/**
 * The main navigation: sections of destinations, composed.
 *
 *   <lg-nav-rail label="Game" [compact]="compact()">
 *     <lg-nav-rail-header><button lgActivity …></button></lg-nav-rail-header>
 *     <lg-nav-section label="Character">
 *       <a lgNavItem routerLink="/game/character/character-overview" routerLinkActive ariaCurrentWhenActive="page"
 *          icon="overview" description="Stats, vitals, loadout">Overview</a>
 *       <a lgNavItem locked reason="Unlocks at level 20" icon="colosseum">Colosseum</a>
 *     </lg-nav-section>
 *     <lg-nav-rail-footer>…</lg-nav-rail-footer>
 *   </lg-nav-rail>
 *
 * `compact` shows icons only: each title stays the item's name and becomes its tooltip.
 */
@Component({
  selector: 'lg-nav-rail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgNavRailRef, useExisting: forwardRef(() => LgNavRailComponent) }],
  host: {
    class: 'lg-rail',
    role: 'navigation',
    '[attr.aria-label]': "label() || 'Game'",
    '[class.lg-rail--compact]': 'compact()',
  },
  template: `<ng-content select="lg-nav-rail-header" /><div class="lg-rail__sections"><ng-content /></div
    ><ng-content select="lg-nav-rail-footer" />`,
  styleUrl: './nav-rail.component.css',
})
export class LgNavRailComponent implements LgNavRailRef {
  /** Icons only. */
  readonly compact = input(false, { transform: booleanAttribute });
  readonly label = input('Game');
}
