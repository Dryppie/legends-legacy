import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  forwardRef,
  input,
  model,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * A setting turned on or off at once, with its words: `<lg-switch formControlName="newLook">New look</lg-switch>`.
 * A `button` with `role="switch"`, so the words and the switch are one control, named by the words. `[(checked)]`, or
 * a boolean through `formControlName` and `ngModel`. For a choice that waits for a Save, use a Checkbox.
 */
@Component({
  selector: 'lg-switch',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => LgSwitchComponent), multi: true }],
  host: {
    class: 'lg-switch',
    '[class.is-checked]': 'checked()',
    '[class.is-disabled]': 'isDisabled()',
  },
  template: `
    <button
      type="button"
      role="switch"
      class="lg-switch__control"
      [attr.aria-checked]="checked()"
      [disabled]="isDisabled()"
      (click)="toggle()"
      (blur)="onTouched()"
    >
      <span class="lg-switch__track" aria-hidden="true"><span class="lg-switch__thumb"></span></span>
      <span class="lg-switch__label"><ng-content /></span>
    </button>
  `,
  styleUrl: './switch.component.css',
})
export class LgSwitchComponent implements ControlValueAccessor {
  readonly checked = model(false);
  readonly disabled = input(false, { transform: booleanAttribute });

  private readonly formDisabled = signal(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  private changed: (value: boolean) => void = () => {};
  protected onTouched: () => void = () => {};

  protected toggle(): void {
    const value = !this.checked();
    this.checked.set(value);
    this.changed(value);
  }

  writeValue(value: unknown): void {
    this.checked.set(!!value);
  }
  registerOnChange(fn: (value: boolean) => void): void {
    this.changed = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.formDisabled.set(disabled);
  }
}
