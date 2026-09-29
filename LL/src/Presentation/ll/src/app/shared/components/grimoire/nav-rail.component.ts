import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  contentChildren,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LgIconComponent } from './icon.component';
import { LgIconName } from './grimoire-icons';
import { LgSlotDirective, lgHasSlot } from './grimoire-core';

export interface LgNavItem {
  id: string;
  title: string;
  icon?: LgIconName;
  /** Router path; without it the item emits `navigate` instead. */
  route?: string;
  badge?: string | number;
  badgeLabel?: string;
  locked?: boolean;
}

export interface LgNavSection {
  label: string;
  items: readonly LgNavItem[];
}

/** The game's primary navigation. Logo/wordmark in `lgSlot="header"`, extras in `lgSlot="footer"`. */
@Component({
  selector: 'lg-nav-rail',
  imports: [LgIconComponent, NgTemplateOutlet, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "compact() ? 'lg-rail lg-rail--compact' : 'lg-rail'",
    role: 'navigation',
    '[attr.aria-label]': 'label()',
  },
  template: `
    @if (has('header')) {
      <div class="lg-rail__header"><ng-content select="[lgSlot=header]" /></div>
    }
    <div class="lg-rail__sections">
      @for (section of sections(); track section.label) {
        <div class="lg-rail__section">
          <div class="lg-rail__group">{{ section.label }}</div>
          <ul>
            @for (item of section.items; track item.id) {
              <li>
                <ng-template #inner>
                  @if (item.icon) {
                    <lg-icon [name]="item.icon" [size]="18" />
                  }
                  <span class="lg-rail__title">{{ item.title }}</span>
                  @if (item.badge !== undefined && item.badge !== null && item.badge !== '') {
                    <span class="lg-rail__badge" [attr.aria-label]="item.badgeLabel ?? null">{{ item.badge }}</span>
                  }
                </ng-template>
                @if (item.route && !item.locked) {
                  <a
                    class="lg-rail__item"
                    [class.is-active]="item.id === activeId()"
                    [routerLink]="item.route"
                    [attr.aria-current]="item.id === activeId() ? 'page' : null"
                    [attr.title]="compact() ? item.title : null"
                    (click)="navigate.emit(item.id)"
                  >
                    <ng-container [ngTemplateOutlet]="inner" />
                  </a>
                } @else {
                  <button
                    type="button"
                    class="lg-rail__item"
                    [class.is-active]="item.id === activeId()"
                    [class.is-locked]="!!item.locked"
                    [disabled]="!!item.locked"
                    [attr.aria-current]="item.id === activeId() ? 'page' : null"
                    [attr.title]="compact() ? item.title : null"
                    (click)="navigate.emit(item.id)"
                  >
                    <ng-container [ngTemplateOutlet]="inner" />
                  </button>
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
  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
