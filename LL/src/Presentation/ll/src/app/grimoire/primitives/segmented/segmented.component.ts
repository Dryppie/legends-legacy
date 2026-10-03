import {
  AfterContentInit,
  OnInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  Signal,
  booleanAttribute,
  computed,
  contentChildren,
  forwardRef,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FocusKeyManager, FocusableOption } from '@angular/cdk/a11y';
import { lgUniqueId } from '../../core/grimoire-core';

/** What a Segmented tells its segments: the chosen value, which segment takes Tab, and how to choose. */
export abstract class LgSegmentedRef {
  abstract readonly value: Signal<unknown>;
  abstract isTabStop(segment: LgSegmentComponent): boolean;
  /** The segment just before it is chosen: its rule gives way to that one's ring. */
  abstract isAfterChosen(segment: LgSegmentComponent): boolean;
  abstract choose(segment: LgSegmentComponent): void;
}

/** One segment: its words, and the `value` choosing it gives. `<lg-segment value="large">Large</lg-segment>` */
@Component({
  selector: 'lg-segment',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-segment',
    role: 'radio',
    '[attr.aria-checked]': 'checked()',
    '[class.is-after-chosen]': 'segmented?.isAfterChosen(this)',
    '[attr.aria-disabled]': "isDisabled() ? 'true' : null",
    '[attr.tabindex]': 'segmented?.isTabStop(this) ? 0 : -1',
    '(click)': 'press()',
    '(keydown.space)': '$event.preventDefault(); press()',
    '(keydown.enter)': '$event.preventDefault(); press()',
  },
  template: '<ng-content />',
  styleUrl: './segment.component.css',
})
export class LgSegmentComponent implements FocusableOption, OnInit {
  readonly value = input.required<unknown>();
  readonly isDisabled = input(false, { alias: 'disabled', transform: booleanAttribute });
  /** Read by the key manager, which passes over a disabled segment. */
  get disabled(): boolean {
    return this.isDisabled();
  }

  protected readonly segmented = inject(LgSegmentedRef, { optional: true });
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  /** Set once its inputs are: a sibling reads `checked()` while the segments render (NG0950). */
  private readonly settled = signal(false);
  readonly checked = computed(() => this.settled() && this.segmented?.value() === this.value());

  ngOnInit(): void {
    this.settled.set(true);
  }

  focus(): void {
    this.el.focus();
  }
  getLabel(): string {
    return this.el.textContent?.trim() ?? '';
  }
  protected press(): void {
    if (!this.isDisabled()) this.segmented?.choose(this);
  }
}

/**
 * Two to four short choices side by side, one always chosen: a radio group drawn as joined segments.
 * `<lg-segmented label="Text size" formControlName="textSize"><lg-segment value="default">Default</lg-segment>…`
 * The arrow keys move the choice and Tab leaves it. `[(value)]`, or `formControlName` and `ngModel`. Its label shows
 * above it like a Field's; `labelHidden` keeps it for screen readers only, where the setting's name is beside it.
 */
@Component({
  selector: 'lg-segmented',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => LgSegmentedComponent), multi: true },
    { provide: LgSegmentedRef, useExisting: forwardRef(() => LgSegmentedComponent) },
  ],
  host: {
    class: 'lg-segmented',
    '[class.is-disabled]': 'isDisabled()',
    '(focusout)': 'onFocusOut($event)',
  },
  template: `
    <span class="lg-segmented__label" [class.lg-sr]="labelHidden()" [id]="labelId">{{ label() }}</span>
    <div
      class="lg-segmented__track"
      role="radiogroup"
      [attr.aria-labelledby]="labelId"
      [attr.aria-disabled]="isDisabled() ? 'true' : null"
      (keydown)="onKeydown($event)"
    >
      <ng-content />
    </div>
  `,
  styleUrl: './segmented.component.css',
})
export class LgSegmentedComponent implements ControlValueAccessor, LgSegmentedRef, AfterContentInit {
  readonly label = input.required<string>();
  readonly value = model<unknown>(null);
  readonly labelHidden = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });

  protected readonly labelId = lgUniqueId('lgseg') + '-label';
  private readonly segments = contentChildren(LgSegmentComponent);
  private readonly injector = inject(Injector);
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private keys: FocusKeyManager<LgSegmentComponent> | null = null;
  private readonly formDisabled = signal(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  private changed: (value: unknown) => void = () => {};
  private onTouched: () => void = () => {};

  ngAfterContentInit(): void {
    this.keys = new FocusKeyManager(this.segments, this.injector)
      .withHorizontalOrientation('ltr')
      .withVerticalOrientation(true)
      .withWrap()
      .withHomeAndEnd();
  }

  isTabStop(segment: LgSegmentComponent): boolean {
    const all = this.segments();
    const chosen = all.find((s) => s.checked() && !s.disabled);
    return chosen ? chosen === segment : all.find((s) => !s.disabled) === segment;
  }

  isAfterChosen(segment: LgSegmentComponent): boolean {
    const all = this.segments();
    const i = all.indexOf(segment);
    return i > 0 && all[i - 1].checked();
  }

  choose(segment: LgSegmentComponent): void {
    if (this.isDisabled() || segment.disabled) return;
    const value = segment.value();
    this.keys?.updateActiveItem(segment);
    if (value === this.value()) return;
    this.value.set(value);
    this.changed(value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (!this.keys || this.isDisabled()) return;
    const current = this.segments().find((s) => this.isTabStop(s));
    if (current && this.keys.activeItem !== current) this.keys.updateActiveItem(current);
    const before = this.keys.activeItem;
    this.keys.onKeydown(event);
    const after = this.keys.activeItem;
    // A radio group: moving is choosing.
    if (after && after !== before) this.choose(after);
  }

  protected onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (!next || !this.el.contains(next)) this.onTouched();
  }

  writeValue(value: unknown): void {
    this.value.set(value);
  }
  registerOnChange(fn: (value: unknown) => void): void {
    this.changed = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.formDisabled.set(disabled);
  }
}

export const LG_SEGMENTED = [LgSegmentedComponent, LgSegmentComponent] as const;
