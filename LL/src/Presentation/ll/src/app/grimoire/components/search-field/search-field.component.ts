import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  booleanAttribute,
  computed,
  contentChildren,
  effect,
  forwardRef,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import { ActiveDescendantKeyManager } from '@angular/cdk/a11y';
import {
  CdkConnectedOverlay,
  CdkOverlayOrigin,
  ConnectedPosition,
  Overlay,
} from '@angular/cdk/overlay';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgAnnouncer } from '../../core/grimoire-announcer';
import { LgOptionComponent, LgOptionParent } from '../../primitives/option/option.component';

/** Below the field, or above it when there is no room; 6px away, as the old panel sat. */
const POSITIONS: ConnectedPosition[] = [
  { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 6 },
  { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -6 },
];

/**
 * Search with suggestions (an ARIA combobox). The suggestions open beneath the field on the CDK overlay, so no region
 * clips them, and focus stays in the field while Up and Down move the highlight (the CDK's
 * ActiveDescendantKeyManager).
 *
 *   <lg-search-field label="Search character by name" [(value)]="query" [suggestions]="names()"
 *     (pick)="view($event)" (submitted)="search($event)" />
 *
 * Suggestions from a service are `suggestions` (strings); richer ones are projected `lg-option`s
 * (`<lg-option value="Maren">Maren <span>Lv 42</span></lg-option>`). Screen readers hear how many suggestions there
 * are, or that a search found nothing, once, through the announcer.
 */
@Component({
  selector: 'lg-search-field',
  imports: [CdkConnectedOverlay, CdkOverlayOrigin, LgOptionComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgOptionParent, useExisting: forwardRef(() => LgSearchFieldComponent) }],
  host: { class: 'lg-search' },
  template: `
    <div class="lg-search__field" cdkOverlayOrigin #origin="cdkOverlayOrigin">
      <svg class="lg-search__glass" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true">
        <circle cx="10.5" cy="10.5" r="6" />
        <path d="M15 15l5 5" />
      </svg>
      <input
        #input
        [id]="id"
        type="text"
        class="lg-input lg-search__input"
        autocomplete="off"
        role="combobox"
        aria-autocomplete="list"
        [attr.aria-controls]="panelOpen() ? listId : null"
        [attr.aria-expanded]="panelOpen()"
        [attr.aria-activedescendant]="panelOpen() ? (active()?.id ?? null) : null"
        [attr.aria-label]="label() || placeholder() || null"
        [attr.placeholder]="placeholder() ?? null"
        [attr.maxlength]="maxLength() || 80"
        [value]="value() || ''"
        [attr.value]="value() || ''"
        (input)="onInput($any($event.target).value)"
        (focus)="openPanel()"
        (blur)="closePanel()"
        (keydown)="onKeydown($event)"
      />
    </div>
    <ng-template
      cdkConnectedOverlay
      [cdkConnectedOverlayOrigin]="origin"
      [cdkConnectedOverlayOpen]="panelOpen()"
      [cdkConnectedOverlayPositions]="positions"
      [cdkConnectedOverlayWidth]="width()"
      [cdkConnectedOverlayScrollStrategy]="scroll"
      (detach)="closePanel()"
    >
      <div
        [id]="listId"
        class="lg-search__panel"
        role="listbox"
        [attr.aria-label]="label() || placeholder() || null"
        (mousedown)="$event.preventDefault()"
      >
        @if (loading()) {
          <div class="lg-search__note">{{ loadingText() || 'Finding players…' }}</div>
        }
        <ng-content />
        @for (suggestion of suggestions(); track suggestion) {
          <lg-option [value]="suggestion">{{ suggestion }}</lg-option>
        }
        @if (!loading() && !count()) {
          <div class="lg-search__note">{{ emptyText() || 'No matching players' }}</div>
        }
      </div>
    </ng-template>
  `,
  styleUrl: './search-field.component.css',
})
export class LgSearchFieldComponent extends LgOptionParent {
  readonly value = model('');
  /** Suggestions from a service, as words. Richer ones are projected `lg-option`s. */
  readonly suggestions = input<readonly string[]>([]);
  readonly loading = input(false, { transform: booleanAttribute });
  /** A search has run for the current value: show `emptyText` when nothing matched. */
  readonly searched = input(false, { transform: booleanAttribute });
  readonly placeholder = input<string>();
  readonly label = input<string>();
  readonly maxLength = input(80);
  readonly emptyText = input('No matching players');
  readonly loadingText = input('Finding players…');
  /** A suggestion was chosen (`select` is a DOM event name, so this is `pick`). */
  readonly pick = output<string>();
  /** Enter with no suggestion highlighted (`submit` is a DOM event name, so this is `submitted`). */
  readonly submitted = output<string>();

  protected readonly id = lgUniqueId('lgs');
  protected readonly listId = this.id + '-list';
  protected readonly positions = POSITIONS;
  protected readonly scroll = inject(Overlay).scrollStrategies.reposition();
  private readonly origin = viewChild.required(CdkOverlayOrigin);
  private readonly projected = contentChildren(LgOptionComponent);
  private readonly rendered = viewChildren(LgOptionComponent);
  /** Every option, in the order the list shows them: the projected ones, then the suggestions. */
  private readonly options = computed(() => [...this.projected(), ...this.rendered()]);
  private readonly keys = new ActiveDescendantKeyManager(this.options, inject(Injector))
    .withVerticalOrientation()
    .withHorizontalOrientation(null)
    .withWrap();

  protected readonly open = signal(false);
  protected readonly width = signal(0);
  /** The highlighted option, or null. */
  protected readonly active = signal<LgOptionComponent | null>(null);
  /** How many options there are; the suggestions are counted from the input, so the list can open on them. */
  protected readonly count = computed(() => this.projected().length + this.suggestions().length);
  protected readonly panelOpen = computed(
    () => this.open() && (this.loading() || this.count() > 0 || (this.searched() && (this.value() || '').length > 0)),
  );
  private readonly said = computed(() => {
    if (!this.panelOpen() || this.loading()) return '';
    const n = this.count();
    if (n) return n + (n === 1 ? ' suggestion' : ' suggestions');
    return this.searched() ? this.emptyText() || 'No matching players' : '';
  });

  constructor() {
    super();
    const announcer = inject(LgAnnouncer);
    effect(() => {
      const text = this.said();
      if (text) announcer.announce(text, { key: 'search-' + this.id });
    });
    const changes = this.keys.change.subscribe(() => this.active.set(this.keys.activeItem));
    inject(DestroyRef).onDestroy(() => {
      changes.unsubscribe();
      this.keys.destroy();
    });
  }

  /** LgOptionParent: the pointer is over an option. */
  highlight(option: LgOptionComponent): void {
    if (!option.disabled) this.keys.setActiveItem(option);
  }

  /** LgOptionParent: an option was pressed. */
  choose(option: LgOptionComponent): void {
    const value = option.value();
    this.closePanel();
    this.value.set(value);
    this.pick.emit(value);
  }

  protected openPanel(): void {
    // The list is as wide as the field.
    this.width.set(this.origin().elementRef.nativeElement.getBoundingClientRect().width);
    this.open.set(true);
  }

  protected closePanel(): void {
    this.open.set(false);
    this.keys.setActiveItem(-1);
  }

  protected onInput(text: string): void {
    this.openPanel();
    this.keys.setActiveItem(-1);
    this.value.set(text);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      if (!this.count()) return;
      event.preventDefault();
      if (!this.open()) this.openPanel();
      this.keys.onKeydown(event);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const option = this.keys.activeItem;
      if (this.panelOpen() && option) this.choose(option);
      else {
        this.closePanel();
        this.submitted.emit(this.value());
      }
    } else if (event.key === 'Escape' && this.open()) {
      // The list is the topmost layer: Escape closes it and goes no further (Foundations · Accessibility).
      event.stopPropagation();
      this.closePanel();
    }
  }
}
