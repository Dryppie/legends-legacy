import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  inject,
  input,
  model,
} from '@angular/core';
import { LgTagComponent, LgTagTone } from './tag.component';

export interface LgEntry {
  id: string;
  name: string;
  tag?: string;
  tagTone?: LgTagTone;
  locked?: boolean;
}

/**
 * Tall list of names (creatures, members, prophecies) with a starred selection and
 * faded ends. Give it a height; it scrolls. Up/Down move the selection.
 */
@Component({
  selector: 'lg-entry-list',
  imports: [LgTagComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ul
      class="lg-entrylist"
      [class.lg-entrylist--fade]="fade()"
      role="listbox"
      [attr.aria-label]="label()"
      (keydown)="onKeydown($event)"
    >
      @for (entry of items(); track entry.id) {
        <li
          role="option"
          class="lg-entrylist__item"
          [class.is-active]="entry.id === activeId()"
          [class.is-locked]="!!entry.locked"
          [attr.aria-selected]="entry.id === activeId()"
          [attr.aria-disabled]="entry.locked || null"
          [attr.tabindex]="entry.id === activeId() ? 0 : -1"
          (click)="choose(entry)"
        >
          <span class="lg-entrylist__marker" aria-hidden="true">
            <svg viewBox="0 0 16 16" width="14" height="14">
              <path d="M8 0 L9.6 6.4 L16 8 L9.6 9.6 L8 16 L6.4 9.6 L0 8 L6.4 6.4 Z" />
            </svg>
          </span>
          <span class="lg-entrylist__name">{{ entry.name }}</span>
          @if (entry.locked) {
            <span class="lg-entrylist__lock">Locked</span>
          }
          @if (entry.tag) {
            <lg-tag [tone]="entry.tagTone ?? 'new'">{{ entry.tag }}</lg-tag>
          }
        </li>
      }
    </ul>
  `,
})
export class LgEntryListComponent {
  readonly items = input.required<readonly LgEntry[]>();
  readonly activeId = model<string>();
  readonly fade = input(true, { transform: booleanAttribute });
  readonly label = input('Entries');

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected choose(entry: LgEntry): void {
    if (!entry.locked) this.activeId.set(entry.id);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    const items = this.items();
    if (!items.length) return;
    event.preventDefault();
    const step = event.key === 'ArrowDown' ? 1 : -1;
    const start = Math.max(0, items.findIndex((entry) => entry.id === this.activeId()));
    let next = start;
    for (let tries = 0; tries < items.length; tries++) {
      next = (next + step + items.length) % items.length;
      if (!items[next].locked) break;
    }
    this.activeId.set(items[next].id);
    this.host.nativeElement.querySelectorAll<HTMLElement>('[role=option]')[next]?.focus();
  }
}
