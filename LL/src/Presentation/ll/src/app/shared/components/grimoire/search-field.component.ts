import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { lgUniqueId } from './grimoire-core';

/**
 * Pill search input with a suggestions list (ARIA combobox).
 * `<lg-search-field [(value)]="query" [suggestions]="names" (pick)="view($event)" />`
 */
@Component({
  selector: 'lg-search-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-search' },
  template: `
    <div class="lg-search__field">
      <svg class="lg-search__glass" viewBox="0 0 24 24" width="16" height="16" fill="none"
        stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true">
        <circle cx="10.5" cy="10.5" r="6" />
        <path d="M15 15l5 5" />
      </svg>
      <input
        type="text"
        class="lg-input lg-search__input"
        autocomplete="off"
        role="combobox"
        aria-autocomplete="list"
        [id]="id"
        [attr.aria-controls]="id + '-list'"
        [attr.aria-expanded]="panelOpen()"
        [attr.aria-activedescendant]="active() >= 0 ? id + '-opt-' + active() : null"
        [attr.aria-label]="label() ?? placeholder()"
        [attr.placeholder]="placeholder()"
        [attr.maxlength]="maxLength()"
        [value]="value()"
        (input)="onInput($any($event.target).value)"
        (focus)="open.set(true)"
        (blur)="close()"
        (keydown)="onKeydown($event)"
      />
    </div>
    @if (panelOpen()) {
      <div class="lg-search__panel" role="listbox" [id]="id + '-list'">
        @if (loading()) {
          <div class="lg-search__note">{{ loadingText() }}</div>
        }
        @for (option of suggestions(); track option; let i = $index) {
          <div
            class="lg-search__option"
            role="option"
            [id]="id + '-opt-' + i"
            [class.is-active]="i === active()"
            [attr.aria-selected]="i === active()"
            (mouseenter)="active.set(i)"
            (mousedown)="$event.preventDefault(); choose(option)"
          >
            {{ option }}
          </div>
        }
        @if (!loading() && !suggestions().length) {
          <div class="lg-search__note">{{ emptyText() }}</div>
        }
      </div>
    }
  `,
})
export class LgSearchFieldComponent {
  readonly value = model('');
  readonly suggestions = input<readonly string[]>([]);
  readonly loading = input(false);
  /** A search has run for the current value: show `emptyText` when nothing matched. */
  readonly searched = input(false);
  readonly placeholder = input('Search…');
  readonly label = input<string>();
  readonly maxLength = input(80);
  readonly emptyText = input('No matching players');
  readonly loadingText = input('Finding players…');
  /** A suggestion was chosen. */
  readonly pick = output<string>();
  /** Enter with no suggestion highlighted. */
  readonly submitted = output<string>();

  protected readonly id = lgUniqueId('lg-search');
  protected readonly open = signal(false);
  protected readonly active = signal(-1);
  protected readonly panelOpen = computed(
    () =>
      this.open() &&
      (this.loading() ||
        this.suggestions().length > 0 ||
        (this.searched() && this.value().length > 0)),
  );

  protected onInput(text: string): void {
    this.value.set(text);
    this.open.set(true);
    this.active.set(-1);
  }

  protected close(): void {
    this.open.set(false);
    this.active.set(-1);
  }

  protected choose(option: string): void {
    this.value.set(option);
    this.close();
    this.pick.emit(option);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const options = this.suggestions();
    if (event.key === 'ArrowDown' && options.length) {
      event.preventDefault();
      this.open.set(true);
      this.active.set((this.active() + 1) % options.length);
    } else if (event.key === 'ArrowUp' && options.length) {
      event.preventDefault();
      this.active.set((this.active() - 1 + options.length) % options.length);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const highlighted = options[this.active()];
      if (this.panelOpen() && highlighted !== undefined) {
        this.choose(highlighted);
      } else {
        this.close();
        this.submitted.emit(this.value());
      }
    } else if (event.key === 'Escape') {
      this.close();
    }
  }
}
