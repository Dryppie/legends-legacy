import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  model,
  signal,
} from '@angular/core';
import { LgDensity, lgCx, lgUniqueId } from './grimoire-core';
import { LgTagComponent, LgTagTone } from './tag.component';
import { LgWhyDirective, LgWhyOptions, lgWhySpoken } from './grimoire-a11y';
import { lgBlockedReason, lgReadyWords } from './grimoire-states';

export interface LgEntry {
  id: string;
  name: string;
  tag?: string;
  tagTone?: LgTagTone;
  locked?: boolean;
  /** Locked: how it unlocks. */
  reason?: string;
  /** The attention diamond at the row's end: true, or the words ("1 point to spend"). Not on a locked entry. */
  ready?: boolean | string;
}

/**
 * The browsable name list (creatures, members, prophecies), with faded ends. Give it a height; it scrolls. Selection
 * follows focus. A locked entry stays in reach of the keyboard: Up and Down land on it and its unlock condition opens
 * beside it, but it never becomes selected; Enter or a click shows the condition. One Tag per row: Locked before the
 * entry's own tag.
 */
@Component({
  selector: 'lg-entry-list',
  imports: [LgTagComponent, LgWhyDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ul
      [class]="classes()"
      role="listbox"
      [attr.aria-label]="label() || 'Entries'"
      [attr.data-density]="density() ?? null"
      (keydown)="onKeydown($event)"
    >
      @for (entry of rows(); track entry.item.id; let i = $index) {
        <li
          role="option"
          [attr.aria-selected]="entry.active"
          [attr.data-index]="i"
          [attr.tabindex]="i === tabAt() ? 0 : -1"
          [class]="entry.classes"
          [lgWhy]="entry.why"
          [lgWhyId]="entry.whyId"
          (focus)="focused.set(i)"
          (click)="entry.item.locked ? null : choose(entry.item.id)"
        >
          <span class="lg-entrylist__name">{{ entry.item.name }}</span>
          @if (entry.item.locked) {
            <lg-tag state="locked" ariaHidden />
          } @else if (entry.item.tag) {
            <lg-tag [tone]="entry.item.tagTone || 'new'">{{ entry.item.tag }}</lg-tag>
          }
          <!-- The attention diamond at the row's end; a locked entry isn't waiting for the player, so it takes none. -->
          @if (entry.item.ready && !entry.item.locked) {
            <span class="lg-attention lg-entrylist__attention" aria-hidden="true"></span>
            <span class="lg-sr">, {{ readyWords(entry.item.ready) }}</span>
          }
          @if (entry.why; as w) {
            <span class="lg-sr lg-why__desc" [id]="entry.whyId" aria-hidden="true">{{ spoken(w) }}</span>
          }
        </li>
      }
    </ul>
  `,
})
export class LgEntryListComponent {
  readonly items = input.required<readonly LgEntry[]>();
  /** The selected entry; two-way bindable (React's activeId and onSelect). */
  readonly activeId = model<string>();
  readonly fade = input(true, { transform: booleanAttribute });
  readonly density = input<LgDensity>();
  readonly label = input('Entries');

  protected readonly focused = signal<number | null>(null);
  protected readonly spoken = lgWhySpoken;
  protected readonly readyWords = lgReadyWords;
  private readonly base = lgUniqueId('lge');

  protected readonly classes = computed(() => lgCx('lg-entrylist', this.fade() !== false && 'lg-entrylist--fade'));
  private readonly activeIndex = computed(() => this.items().findIndex((x) => x.id === this.activeId()));
  protected readonly tabAt = computed(() => {
    const f = this.focused();
    return f != null && f < this.items().length ? f : Math.max(0, this.activeIndex());
  });
  protected readonly rows = computed(() =>
    this.items().map((item, i) => {
      const active = item.id === this.activeId() && !item.locked;
      const why: LgWhyOptions | null = item.locked
        ? {
            reason: lgBlockedReason('locked', item, `EntryList entry "${item.name}"`).reason,
            word: 'Locked',
            place: 'end',
          }
        : null;
      return {
        item,
        active,
        why,
        whyId: `${this.base}-${i}`,
        classes: lgCx('lg-entrylist__item', active && 'is-active', item.locked && 'is-locked'),
      };
    }),
  );

  protected choose(id: string): void {
    this.activeId.set(id);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const items = this.items();
    const n = items.length;
    const row = (event.target as Element).closest?.('[role=option]') as HTMLElement | null;
    const i = row ? +row.getAttribute('data-index')! : Math.max(0, this.activeIndex());
    let j: number | null = null;
    if (event.key === 'ArrowDown') j = (i + 1) % n;
    else if (event.key === 'ArrowUp') j = (i - 1 + n) % n;
    else if (event.key === 'Home') j = 0;
    else if (event.key === 'End') j = n - 1;
    if (j != null && n) {
      event.preventDefault();
      this.focused.set(j);
      (event.currentTarget as HTMLElement).querySelector<HTMLElement>(`[data-index="${j}"]`)?.focus();
      if (!items[j].locked) this.choose(items[j].id);
      return;
    }
    if ((event.key === 'Enter' || event.key === ' ') && row) {
      event.preventDefault();
      if (items[i].locked) row.click();
      else this.choose(items[i].id);
    }
  }
}
