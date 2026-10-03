import {
  ChangeDetectionStrategy,
  Component,
  Signal,
  booleanAttribute,
  computed,
  contentChild,
  effect,
  forwardRef,
  input,
  signal,
} from '@angular/core';
import { NgControl, ValidationErrors, Validators } from '@angular/forms';
import { lgUniqueId } from '../../core/grimoire-core';

/** What a Field tells the control inside it: its id, what describes it, and whether it shows an error. */
export abstract class LgFieldRef {
  abstract readonly controlId: string;
  abstract readonly labelId: string;
  /** The hint's and the error's ids, as `aria-describedby`; null when there are neither. */
  abstract readonly describedBy: Signal<string | null>;
  /** The error shows: the control is `aria-invalid`. */
  abstract readonly invalid: Signal<boolean>;
  abstract readonly required: Signal<boolean>;
}

/** The words for a validator's error, when the Field is given none of its own. */
const DEFAULT_MESSAGES: Record<string, (error: unknown) => string> = {
  required: () => 'Fill this in.',
  minlength: (e) => `At least ${(e as { requiredLength: number }).requiredLength} characters.`,
  maxlength: (e) => `At most ${(e as { requiredLength: number }).requiredLength} characters.`,
  min: (e) => `At least ${(e as { min: number }).min}.`,
  max: (e) => `At most ${(e as { max: number }).max}.`,
  email: () => 'Enter an email address.',
  pattern: () => 'Not in the expected form.',
};

/** The first of the control's errors, in words: the Field's own message for it, else the default. */
export function lgFieldMessage(
  errors: ValidationErrors | null,
  messages: Readonly<Record<string, string>> = {},
): string | null {
  if (!errors) return null;
  for (const key of Object.keys(errors)) {
    if (messages[key]) return messages[key];
    const make = DEFAULT_MESSAGES[key];
    if (make) return make(errors[key]);
    if (typeof errors[key] === 'string') return errors[key] as string;
  }
  return 'Check this.';
}

/**
 * A labelled form field: its label above the control, a hint under it, and an error in place of the hint once the
 * player has touched the control and it is invalid. `<lg-field label="Character name" hint="3 to 16 letters"><input
 * lgInput formControlName="name" /></lg-field>`. It wires the control's id, `aria-describedby` and `aria-invalid`,
 * and reads the control's errors from its `NgControl` (Reactive Forms or `ngModel`); `error` sets the words yourself,
 * and `messages` names a validator's error (`{ required: 'Name your character.' }`).
 */
@Component({
  selector: 'lg-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LgFieldRef, useExisting: forwardRef(() => LgFieldComponent) }],
  host: { class: 'lg-field', '[class.is-invalid]': 'invalid()' },
  template: `
    <label class="lg-field__label" [id]="labelId" [attr.for]="controlId"
      >{{ label() }}@if (required()) {<span class="lg-field__required" aria-hidden="true"> *</span>}</label
    >
    <ng-content />
    <div class="lg-field__foot">
      @if (hint() && !shownError()) {
        <p class="lg-field__hint" [id]="hintId">{{ hint() }}</p>
      }
      <!-- Always there, so screen readers hear the error when it arrives. -->
      <p class="lg-field__error" [id]="errorId" aria-live="polite"
        >@if (shownError(); as words) {<span class="lg-field__glyph" aria-hidden="true">✕</span>{{ words }}}</p
      >
    </div>
  `,
  styleUrl: './field.component.css',
})
export class LgFieldComponent implements LgFieldRef {
  readonly label = input.required<string>();
  /** One line under the control: what to enter ("3 to 16 letters"). */
  readonly hint = input<string>();
  /** The error, in words, whatever the control's own state. */
  readonly error = input<string | null>();
  /** A validator's error in words, by its key. */
  readonly messages = input<Readonly<Record<string, string>>>({});
  /** Marks the label (" *") and the control (`aria-required`). With Reactive Forms, `Validators.required` does it. */
  readonly isRequired = input(false, { alias: 'required', transform: booleanAttribute });

  private readonly base = lgUniqueId('lgfield');
  readonly controlId = this.base + '-control';
  readonly labelId = this.base + '-label';
  protected readonly hintId = this.base + '-hint';
  protected readonly errorId = this.base + '-error';

  private readonly ngControl = contentChild(NgControl, { descendants: true });
  /** Bumped on each of the control's events (value, status, touched), so the error follows them. */
  private readonly seen = signal(0);

  protected readonly shownError = computed(() => {
    this.seen();
    const own = this.error();
    if (own) return own;
    const control = this.ngControl()?.control;
    if (!control || !control.invalid || !(control.touched || control.dirty)) return null;
    return lgFieldMessage(control.errors, this.messages());
  });
  readonly invalid = computed(() => !!this.shownError());
  readonly required = computed(() => {
    this.seen();
    const control = this.ngControl()?.control;
    return this.isRequired() || !!control?.hasValidator(Validators.required);
  });
  readonly describedBy = computed(() => (this.shownError() ? this.errorId : this.hint() ? this.hintId : null));

  constructor() {
    effect((onCleanup) => {
      const control = this.ngControl()?.control;
      if (!control) return;
      const sub = control.events.subscribe(() => this.seen.update((n) => n + 1));
      onCleanup(() => sub.unsubscribe());
    });
  }
}
