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
 * A checkbox with its words: `<lg-checkbox [(checked)]="showNobility">Show my Nobility</lg-checkbox>`. A native
 * checkbox inside, so it is a real form control: `formControlName` and `ngModel` take a boolean. `indeterminate`
 * shows a mixed state (some of a group). Not a circle and not a switch: a checkbox picks, a Switch turns a setting on.
 */
@Component({
  selector: 'lg-checkbox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => LgCheckboxComponent), multi: true }],
  host: {
    class: 'lg-checkbox',
    '[class.is-checked]': 'checked()',
    '[class.is-disabled]': 'isDisabled()',
  },
  template: `
    <label class="lg-checkbox__label">
      <span class="lg-checkbox__box"
        ><input
          type="checkbox"
          class="lg-checkbox__input"
          [checked]="checked()"
          [indeterminate]="indeterminate()"
          [disabled]="isDisabled()"
          (change)="onChange($event)"
          (blur)="onTouched()" /><span class="lg-checkbox__mark" aria-hidden="true"></span
      ></span>
      <span class="lg-checkbox__text"><ng-content /></span>
    </label>
  `,
  styleUrl: './checkbox.component.css',
})
export class LgCheckboxComponent implements ControlValueAccessor {
  readonly checked = model(false);
  /** Some of a group, not all: a dash. A press checks it. */
  readonly indeterminate = model(false);
  readonly disabled = input(false, { transform: booleanAttribute });

  private readonly formDisabled = signal(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  private changed: (value: boolean) => void = () => {};
  protected onTouched: () => void = () => {};

  protected onChange(event: Event): void {
    const value = (event.target as HTMLInputElement).checked;
    this.indeterminate.set(false);
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
