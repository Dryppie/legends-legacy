import { ChangeDetectionStrategy, Component, ElementRef, inject } from '@angular/core';
import { LgFieldRef } from '../field/field.component';

/**
 * A text input or textarea in Grimoire's well: `<input lgInput />`, `<textarea lgInput></textarea>`. The host is the
 * native element, so `formControlName`, `ngModel`, `[value]` and `(input)` work on it as on any input. Inside an
 * `lg-field` it takes the Field's id, description and error state.
 */
@Component({
  selector: 'input[lgInput], textarea[lgInput]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-input',
    '[class.lg-input--area]': 'isArea',
    '[attr.id]': 'id()',
    '[attr.aria-describedby]': 'field?.describedBy() ?? null',
    '[attr.aria-invalid]': "field?.invalid() ? 'true' : null",
    '[attr.aria-required]': "field?.required() ? 'true' : null",
  },
  template: '',
  styleUrl: './input.component.css',
})
export class LgInputComponent {
  protected readonly field = inject(LgFieldRef, { optional: true });
  private readonly el = inject<ElementRef<HTMLInputElement | HTMLTextAreaElement>>(ElementRef).nativeElement;
  protected readonly isArea = this.el.tagName === 'TEXTAREA';
  /** An id of your own wins; inside a Field the input takes the Field's, so its label names it. */
  private readonly ownId = this.el.getAttribute('id');
  protected id(): string | null {
    return this.ownId || this.field?.controlId || null;
  }
}
