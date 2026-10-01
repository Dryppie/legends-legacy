import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  effect,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { lgUniqueId } from './grimoire-core';
import { lgAnnounce } from './grimoire-a11y';

/**
 * Search with suggestions (an ARIA combobox).
 * `<lg-search-field [(value)]="query" [suggestions]="names" (pick)="view($event)" (submitted)="search($event)" />`
 * Screen readers hear how many suggestions there are, or that a search found nothing, once, through the announcer.
 */
@Component({
  selector: 'lg-search-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div class="lg-search">
      <div class="lg-search__field">
        <svg class="lg-search__glass" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true">
          <circle cx="10.5" cy="10.5" r="6" />
          <path d="M15 15l5 5" />
        </svg>
        <input
          [id]="id"
          type="text"
          class="lg-input lg-search__input"
          autocomplete="off"
          role="combobox"
          aria-autocomplete="list"
          [attr.aria-controls]="id + '-list'"
          [attr.aria-expanded]="panelOpen()"
          [attr.aria-activedescendant]="active() >= 0 ? id + '-opt-' + active() : null"
          [attr.aria-label]="label() || placeholder() || null"
          [attr.placeholder]="placeholder() ?? null"
          [attr.maxlength]="maxLength() || 80"
          [value]="value() || ''"
          [attr.value]="value() || ''"
          (input)="onInput($any($event.target).value)"
          (focus)="open.set(true)"
          (blur)="closeSoon()"
          (keydown)="onKeydown($event)"
        />
      </div>
      @if (panelOpen()) {
        <div [id]="id + '-list'" class="lg-search__panel" role="listbox">
          @if (loading()) {
            <div class="lg-search__note">{{ loadingText() || 'Finding players…' }}</div>
          }
          @for (option of suggestions(); track option; let i = $index) {
            <div
              [id]="id + '-opt-' + i"
              role="option"
              [attr.aria-selected]="i === active()"
              class="lg-search__option"
              [class.is-active]="i === active()"
              (mouseenter)="active.set(i)"
              (mousedown)="$event.preventDefault(); choose(option)"
            >
              {{ option }}
            </div>
          }
          @if (!loading() && !suggestions().length) {
            <div class="lg-search__note">{{ emptyText() || 'No matching players' }}</div>
          }
        </div>
      }
    </div>
  `,
})
export class LgSearchFieldComponent {
  readonly value = model('');
  readonly suggestions = input<readonly string[]>([]);
  readonly loading = input(false, { transform: booleanAttribute });
  /** A search has run for the current value: show `emptyText` when nothing matched. */
  readonly searched = input(false, { transform: booleanAttribute });
  readonly placeholder = input<string>();
  readonly label = input<string>();
  readonly maxLength = input(80);
  readonly emptyText = input('No matching players');
  readonly loadingText = input('Finding players…');
  /** A suggestion was chosen (React's onSelect). */
  readonly pick = output<string>();
  /** Enter with no suggestion highlighted (React's onSubmit). */
  readonly submitted = output<string>();

  protected readonly id = lgUniqueId('lgs');
  protected readonly open = signal(false);
  protected readonly active = signal(-1);
  protected readonly panelOpen = computed(
    () =>
      this.open() &&
      (this.loading() || this.suggestions().length > 0 || (this.searched() && (this.value() || '').length > 0)),
  );
  private readonly said = computed(() => {
    if (!this.panelOpen() || this.loading()) return '';
    const n = this.suggestions().length;
    if (n) return n + (n === 1 ? ' suggestion' : ' suggestions');
    return this.searched() ? this.emptyText() || 'No matching players' : '';
  });

  constructor() {
    effect(() => {
      const text = this.said();
      if (text) lgAnnounce(text, { key: 'search-' + this.id });
    });
  }

  protected onInput(text: string): void {
    this.open.set(true);
    this.active.set(-1);
    this.value.set(text);
  }

  protected closeSoon(): void {
    setTimeout(() => this.open.set(false), 120);
  }

  protected choose(option: string): void {
    this.open.set(false);
    this.active.set(-1);
    this.value.set(option);
    this.pick.emit(option);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const list = this.suggestions();
    if (event.key === 'ArrowDown' && list.length) {
      event.preventDefault();
      this.open.set(true);
      this.active.set((this.active() + 1) % list.length);
    } else if (event.key === 'ArrowUp' && list.length) {
      event.preventDefault();
      this.active.set((this.active() - 1 + list.length) % list.length);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.active() >= 0 && list[this.active()] !== undefined) this.choose(list[this.active()]);
      else {
        this.open.set(false);
        this.submitted.emit(this.value());
      }
    } else if (event.key === 'Escape' && this.open()) {
      event.stopPropagation();
      this.open.set(false);
    }
  }
}
