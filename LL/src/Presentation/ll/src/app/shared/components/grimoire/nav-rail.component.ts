import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  contentChildren,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LgSlotDirective, lgCx, lgHasSlot, lgUniqueId } from './grimoire-core';
import { LgIconComponent } from './icon.component';
import { LG_ICONS, LgIconName } from './grimoire-icons';
import { LG_STATES, lgBlockedReason, lgReadyWords } from './grimoire-states';
import { LgWhyDirective, LgWhyOptions, lgWhySpoken } from './grimoire-a11y';

export interface LgNavItem {
  id: string;
  title: string;
  /** A short line under the title ("Stats, vitals, loadout"), so a destination is never known by its icon alone (D-104). Hidden in compact. */
  description?: string;
  icon?: LgIconName;
  /** Router path. */
  route?: string;
  /** A plain link, when there is no route. */
  href?: string;
  badge?: string | number;
  badgeLabel?: string;
  locked?: boolean;
  /** Locked: how it unlocks. */
  reason?: string;
  /** Something waiting with nothing to count: the attention diamond at the item's end. true, or the words ("Quest
   *  ready"). A count `badge` comes first, and Locked before both (Standards · State combinations). */
  ready?: boolean | string;
}

export interface LgNavSection {
  label: string;
  items: readonly LgNavItem[];
}

/**
 * The main navigation. A locked item stays in the Tab order: it says "Locked", its unlock condition opens beside it on
 * hover and focus, and a click or Enter shows the condition instead of navigating.
 * Logo or wordmark in `lgSlot="header"`, extras in `lgSlot="footer"`.
 */
@Component({
  selector: 'lg-nav-rail',
  imports: [LgIconComponent, NgTemplateOutlet, RouterLink, LgWhyDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <nav [class]="compact() ? 'lg-rail lg-rail--compact' : 'lg-rail'" [attr.aria-label]="label() || 'Game'">
      @if (has('header')) {
        <div class="lg-rail__header"><ng-content select="[lgSlot=header]" /></div>
      }
      <div class="lg-rail__sections">
        @for (section of rows(); track section.label) {
          <div class="lg-rail__section">
            <div class="lg-rail__group">{{ section.label }}</div>
            <ul>
              @for (row of section.items; track row.item.id) {
                <li>
                  <ng-template #inner
                    >@if (row.item.icon) {<lg-icon [name]="row.item.icon" [size]="20" />}@if (row.item.description) {<span
                        class="lg-rail__text"
                        ><span class="lg-rail__title">{{ row.item.title }}</span
                        ><span class="lg-rail__desc">{{ row.item.description }}</span></span
                      >} @else {<span class="lg-rail__title">{{ row.item.title }}</span>}@if (row.item.locked) {<span class="lg-rail__lock" aria-hidden="true"
                        >@if (lockIcon) {<lg-icon [name]="lockIcon" [size]="12" />} @else {{{ lockedWord }}}</span
                      >} @else if (row.item.badge != null && row.item.badge !== '') {<span
                        class="lg-rail__badge"
                        [attr.aria-label]="row.item.badgeLabel || null"
                        >{{ row.item.badge }}</span
                      >} @else if (row.item.ready) {<span class="lg-attention lg-rail__attention" aria-hidden="true"></span
                        ><span class="lg-sr">, {{ row.item.badgeLabel || readyWords(row.item.ready) }}</span
                      >}@if (row.why; as w) {<span class="lg-sr lg-why__desc" [id]="row.whyId" aria-hidden="true">{{
                        spoken(w)
                      }}</span>}</ng-template
                  >
                  @if (row.item.route && !row.item.locked) {
                    <a
                      [routerLink]="row.item.route"
                      [class]="row.classes"
                      [attr.aria-current]="row.active ? 'page' : null"
                      [attr.title]="compact() ? row.item.title : null"
                      (click)="navigate.emit(row.item.id)"
                      ><ng-container [ngTemplateOutlet]="inner"
                    /></a>
                  } @else {
                    <a
                      [attr.href]="row.item.href || '#'"
                      [class]="row.classes"
                      [attr.aria-current]="row.active ? 'page' : null"
                      [attr.title]="compact() && !row.item.locked ? row.item.title : null"
                      [lgWhy]="row.why"
                      [lgWhyId]="row.whyId"
                      (click)="row.item.locked ? null : onPlainClick($event, row.item)"
                      ><ng-container [ngTemplateOutlet]="inner"
                    /></a>
                  }
                </li>
              }
            </ul>
          </div>
        }
      </div>
      @if (has('footer')) {
        <div class="lg-rail__footer"><ng-content select="[lgSlot=footer]" /></div>
      }
    </nav>
  `,
})
export class LgNavRailComponent {
  readonly sections = input.required<readonly LgNavSection[]>();
  readonly activeId = input<string>();
  /** Icons only. */
  readonly compact = input(false, { transform: booleanAttribute });
  readonly label = input('Game');
  readonly navigate = output<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly base = lgUniqueId('lgr');
  protected readonly spoken = lgWhySpoken;
  protected readonly readyWords = lgReadyWords;
  protected readonly lockedWord = LG_STATES.locked.word;
  /** The 12px lock marker once it is drawn (Foundations · Iconography); until then the word. */
  protected readonly lockIcon = ('lock' in LG_ICONS ? 'lock' : null) as LgIconName | null;

  protected readonly rows = computed(() =>
    this.sections().map((section, si) => ({
      label: section.label,
      items: section.items.map((item, ii) => {
        const active = item.id === this.activeId() && !item.locked;
        const why: LgWhyOptions | null = item.locked
          ? {
              reason: lgBlockedReason('locked', item, `NavRail item "${item.title}"`).reason,
              word: 'Locked',
              place: 'end',
              title: this.compact() ? item.title : null,
            }
          : null;
        return {
          item,
          active,
          why,
          whyId: `${this.base}-${si}-${ii}`,
          classes: lgCx('lg-rail__item', active && 'is-active', item.locked && 'is-locked'),
        };
      }),
    })),
  );

  protected onPlainClick(event: Event, item: LgNavItem): void {
    if (!item.href || item.href === '#') event.preventDefault();
    this.navigate.emit(item.id);
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
