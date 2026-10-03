import {
  ChangeDetectionStrategy,
  Component,
  Signal,
  booleanAttribute,
  computed,
  forwardRef,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { lgUniqueId } from '../../core/grimoire-core';

/** What a radio group tells its radios: its name, the chosen value, and how to choose. */
export abstract class LgRadioGroupRef {
  abstract readonly name: string;
  abstract readonly value: Signal<unknown>;
  abstract choose(value: unknown): void;
  abstract touched(): void;
}

/** One choice of a radio group: a native radio with its words. `<lg-radio value="docked">Docked</lg-radio>` */
@Component({
  selector: 'lg-radio',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-radio',
    '[class.is-checked]': 'checked()',
    '[class.is-disabled]': 'disabled()',
  },
  template: `
    <label class="lg-radio__label">
      <span class="lg-radio__box"
        ><input
          type="radio"
          class="lg-radio__input"
          [attr.name]="group?.name"
          [checked]="checked()"
          [disabled]="disabled()"
          (change)="group?.choose(value())"
          (blur)="group?.touched()" /><span class="lg-radio__mark" aria-hidden="true"></span
      ></span>
      <span class="lg-radio__text"><ng-content /></span>
    </label>
  `,
  styleUrl: './radio.component.css',
})
export class LgRadioComponent {
  /** What choosing it gives the group. */
  readonly value = input.required<unknown>();
  readonly disabled = input(false, { transform: booleanAttribute });
  protected readonly group = inject(LgRadioGroupRef, { optional: true });
  protected readonly checked = computed(() => this.group?.value() === this.value());
}

/**
 * One choice among a few, all in view: a `fieldset` named by its `label` (the legend), holding `lg-radio`s. Native
 * radios, so the arrow keys move the choice and Tab leaves the group. `[(value)]`, or `formControlName` and
 * `ngModel`. Two to five choices; more, or long ones, go in a Select. For a setting with two to four short words,
 * a Segmented takes less room.
 */
@Component({
  selector: 'lg-radio-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => LgRadioGroupComponent), multi: true },
    { provide: LgRadioGroupRef, useExisting: forwardRef(() => LgRadioGroupComponent) },
  ],
  host: { class: 'lg-radio-group' },
  template: `
    <fieldset class="lg-radio-group__set" [disabled]="isDisabled()">
      <legend class="lg-radio-group__legend">{{ label() }}</legend>
      <div [class]="orientation() === 'horizontal' ? 'lg-radio-group__options is-row' : 'lg-radio-group__options'">
        <ng-content />
      </div>
    </fieldset>
  `,
  styleUrl: './radio-group.component.css',
})
export class LgRadioGroupComponent implements ControlValueAccessor, LgRadioGroupRef {
  readonly label = input.required<string>();
  readonly value = model<unknown>(null);
  /** vertical, one under the other (the default); horizontal, in a row. */
  readonly orientation = input<'vertical' | 'horizontal'>('vertical');
  readonly disabled = input(false, { transform: booleanAttribute });

  readonly name = lgUniqueId('lgradio');
  private readonly formDisabled = signal(false);
  protected readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  private changed: (value: unknown) => void = () => {};
  private onTouched: () => void = () => {};

  choose(value: unknown): void {
    this.value.set(value);
    this.changed(value);
  }
  touched(): void {
    this.onTouched();
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

export const LG_RADIO_GROUP = [LgRadioGroupComponent, LgRadioComponent] as const;
