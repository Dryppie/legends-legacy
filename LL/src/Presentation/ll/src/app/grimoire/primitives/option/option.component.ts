import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  inject,
  input,
  signal,
} from '@angular/core';
import { Highlightable } from '@angular/cdk/a11y';
import { lgUniqueId } from '../../core/grimoire-core';

/** What an option reports to the list it sits in: a SearchField's suggestions (later a Select's). */
export abstract class LgOptionParent {
  /** The pointer is over the option: highlight it. */
  abstract highlight(option: LgOptionComponent): void;
  /** The option was pressed: choose it. */
  abstract choose(option: LgOptionComponent): void;
}

/**
 * One option of a list that keeps focus in its field (an ARIA combobox): the field moves a highlight through the
 * options (`aria-activedescendant`), and the highlighted one is `aria-selected`. Its words are its content; `value` is
 * what choosing it gives. `<lg-option value="Maren">Maren</lg-option>`
 */
@Component({
  selector: 'lg-option',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'option',
    class: 'lg-option',
    '[id]': 'id',
    '[class.is-active]': 'active()',
    '[attr.aria-selected]': 'active()',
    '[attr.aria-disabled]': "isDisabled() ? 'true' : null",
    '(mouseenter)': 'parent?.highlight(this)',
    // A press keeps focus in the field, so the list stays open until the choice is made.
    '(mousedown)': '$event.preventDefault()',
    '(click)': 'isDisabled() || parent?.choose(this)',
  },
  template: '<ng-content />',
  styleUrl: './option.component.css',
})
export class LgOptionComponent implements Highlightable {
  /** What choosing it gives the field: the player's name. */
  readonly value = input.required<string>();
  /** Shown, but skipped by the keys and not chosen. */
  readonly isDisabled = input(false, { alias: 'disabled', transform: booleanAttribute });

  protected readonly parent = inject(LgOptionParent, { optional: true });
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  readonly id = lgUniqueId('lg-option');
  protected readonly active = signal(false);

  /** Highlightable: the key manager skips a disabled option. */
  get disabled(): boolean {
    return this.isDisabled();
  }

  /** Highlightable: the highlight moved here. */
  setActiveStyles(): void {
    this.active.set(true);
    this.el.scrollIntoView?.({ block: 'nearest' });
  }

  /** Highlightable: the highlight moved on. */
  setInactiveStyles(): void {
    this.active.set(false);
  }

  /** Its words, for typeahead in lists that have it. */
  getLabel(): string {
    return (this.el.textContent ?? '').trim();
  }
}
