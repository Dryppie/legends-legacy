import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Injector,
  afterNextRender,
  booleanAttribute,
  computed,
  contentChildren,
  forwardRef,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { ActiveDescendantKeyManager } from '@angular/cdk/a11y';
import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition, Overlay } from '@angular/cdk/overlay';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgIconComponent } from '../icon/icon.component';
import { LgFieldRef } from '../field/field.component';
import { LgOptionComponent, LgOptionParent } from '../option/option.component';

const POSITIONS: ConnectedPosition[] = [
  { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 4 },
  { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -4 },
];

/**
 * One choice from a list, in a field: `<lg-select formControlName="rarity" placeholder="Any rarity"><lg-option
 * value="Epic">Epic</lg-option>…</lg-select>`. A button that shows the choice and opens its options on the CDK overlay
 * beneath it (an ARIA select-only combobox): arrows move the highlight, Enter or Space chooses, Escape closes, typing
 * finds an option by its words, Tab chooses the highlighted one and moves on. Focus stays on the button throughout.
 *
 * The options are `lg-option`s, the same the SearchField shows. The value is a string: `[(value)]`, or through
 * `formControlName` and `ngModel`. Inside an `lg-field` the Field's label names it and its hint or error describes it;
 * outside one, give it a `label`.
 */
@Component({
  selector: 'lg-select',
  imports: [CdkConnectedOverlay, CdkOverlayOrigin, LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: LgOptionParent, useExisting: forwardRef(() => LgSelectComponent) },
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => LgSelectComponent), multi: true },
  ],
  host: {
    class: 'lg-select',
    '[class.is-open]': 'open()',
    '[class.is-disabled]': 'isDisabled()',
  },
  template: `
    <button
      type="button"
      class="lg-select__trigger"
      cdkOverlayOrigin
      role="combobox"
      aria-haspopup="listbox"
      [id]="triggerId"
      [attr.aria-expanded]="open()"
      [attr.aria-controls]="open() ? listId : null"
      [attr.aria-activedescendant]="open() ? (active()?.id ?? null) : null"
      [attr.aria-labelledby]="labelledBy()"
      [attr.aria-label]="labelledBy() ? null : label() || null"
      [attr.aria-describedby]="field?.describedBy() ?? null"
      [attr.aria-invalid]="field?.invalid() ? 'true' : null"
      [attr.aria-required]="field?.required() ? 'true' : null"
      [disabled]="isDisabled()"
      (click)="toggle()"
      (keydown)="onKeydown($event)"
      (blur)="onBlur()"
    >
      <span class="lg-select__value" [class.is-placeholder]="!chosenLabel()">{{ chosenLabel() || placeholder() }}</span>
      <lg-icon class="lg-select__chevron" name="expand" [size]="16" />
    </button>
    <ng-template
      cdkConnectedOverlay
      [cdkConnectedOverlayOrigin]="origin()"
      [cdkConnectedOverlayOpen]="open()"
      [cdkConnectedOverlayPositions]="positions"
      [cdkConnectedOverlayMinWidth]="width()"
      [cdkConnectedOverlayScrollStrategy]="scroll"
      cdkConnectedOverlayPanelClass="lg-root"
      (detach)="close()"
    >
      <div
        [id]="listId"
        class="lg-select__panel"
        role="listbox"
        [attr.aria-labelledby]="labelledBy()"
        [attr.aria-label]="labelledBy() ? null : label() || null"
        (mousedown)="$event.preventDefault()"
      >
        <ng-content />
      </div>
    </ng-template>
  `,
  styleUrl: './select.component.css',
})
export class LgSelectComponent extends LgOptionParent implements ControlValueAccessor {
  /** The chosen option's value, or null. */
  readonly value = model<string | null>(null);
  /** Shown while nothing is chosen: "Any rarity". Not a label. */
  readonly placeholder = input('');
  /** Its name outside a Field. Inside one, the Field's label names it. */
  readonly label = input<string>();
  readonly disabled = input(false, { transform: booleanAttribute });
  /** A choice was made by the player (not when the value is set from outside). */
  readonly picked = output<string>();

  protected readonly field = inject(LgFieldRef, { optional: true });
  private readonly injector = inject(Injector);
  protected readonly triggerId = this.field?.controlId ?? lgUniqueId('lgsel');
  protected readonly listId = lgUniqueId('lgsel') + '-list';
  protected readonly positions = POSITIONS;
  protected readonly scroll = inject(Overlay).scrollStrategies.reposition();
  protected readonly origin = viewChild.required(CdkOverlayOrigin);
  private readonly options = contentChildren(LgOptionComponent, { descendants: true });
  private readonly keys = new ActiveDescendantKeyManager(this.options, this.injector)
    .withVerticalOrientation()
    .withHorizontalOrientation(null)
    .withHomeAndEnd()
    .withTypeAhead(300);

  protected readonly open = signal(false);
  protected readonly width = signal(0);
  protected readonly active = signal<LgOptionComponent | null>(null);
  private readonly formDisabled = signal(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  protected readonly labelledBy = computed(() => this.field?.labelId ?? null);
  private readonly chosen = computed(() => {
    const v = this.value();
    return v == null ? null : (this.options().find((o) => o.value() === v) ?? null);
  });
  protected readonly chosenLabel = computed(() => this.chosen()?.getLabel() ?? '');

  private changed: (value: string | null) => void = () => {};
  private touched: () => void = () => {};

  constructor() {
    super();
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
    if (option.disabled) return;
    const value = option.value();
    this.close();
    if (value !== this.value()) {
      this.value.set(value);
      this.changed(value);
    }
    this.picked.emit(value);
  }

  /** LgOptionParent: the option is the current choice. */
  override isChosen(option: LgOptionComponent): boolean {
    return this.value() != null && option.value() === this.value();
  }

  protected toggle(): void {
    if (this.open()) this.close();
    else this.openPanel();
  }

  protected onBlur(): void {
    this.close();
    this.touched();
  }

  protected onKeydown(event: KeyboardEvent): void {
    const key = event.key;
    if (!this.open()) {
      if (key === 'ArrowDown' || key === 'ArrowUp' || key === 'Enter' || key === ' ') {
        event.preventDefault();
        this.openPanel(key === 'ArrowUp' ? 'last' : undefined);
      } else if (key === 'Home' || key === 'End') {
        event.preventDefault();
        this.openPanel(key === 'Home' ? 'first' : 'last');
      } else if (key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
        // Typing opens the list on the option whose words start with what was typed.
        this.openPanel();
        this.keys.onKeydown(event);
      }
      return;
    }
    if (key === 'Enter' || (key === ' ' && !this.keys.isTyping())) {
      event.preventDefault();
      const option = this.keys.activeItem;
      if (option) this.choose(option);
      else this.close();
    } else if (key === 'Escape') {
      // The list is the topmost layer: Escape closes it and goes no further (Foundations · Accessibility).
      event.preventDefault();
      event.stopPropagation();
      this.close();
    } else if (key === 'Tab') {
      const option = this.keys.activeItem;
      if (option) this.choose(option);
      else this.close();
    } else {
      this.keys.onKeydown(event);
    }
  }

  private openPanel(start?: 'first' | 'last'): void {
    if (this.isDisabled()) return;
    // The list is at least as wide as the field.
    this.width.set(this.origin().elementRef.nativeElement.getBoundingClientRect().width);
    this.open.set(true);
    const chosen = this.chosen();
    if (start === 'last') this.keys.setLastItemActive();
    else if (start === 'first' || !chosen || chosen.disabled) this.keys.setFirstItemActive();
    else this.keys.setActiveItem(chosen);
    // Once the list is drawn, bring the highlighted option into view.
    afterNextRender(() => this.keys.activeItem?.setActiveStyles(), { injector: this.injector });
  }

  protected close(): void {
    if (!this.open()) return;
    this.open.set(false);
    this.keys.setActiveItem(-1);
  }

  writeValue(value: unknown): void {
    this.value.set(value == null || value === '' ? null : String(value));
  }
  registerOnChange(fn: (value: string | null) => void): void {
    this.changed = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.touched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.formDisabled.set(disabled);
  }
}
