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

/** What an option reports to the list it sits in: a SearchField's suggestions, a Select's options. */
export abstract class LgOptionParent {
  /** The pointer is over the option: highlight it. */
  abstract highlight(option: LgOptionComponent): void;
  /** The option was pressed: choose it. */
  abstract choose(option: LgOptionComponent): void;
  /** Whether the option is the list's current choice (a Select's value). Read in a template, so a signal keeps it current. */
  isChosen(option: LgOptionComponent): boolean {
    void option;
    return false;
  }
}

/**
 * One option of a list that keeps focus in its field (an ARIA combobox): the field moves a highlight through the
 * options (`aria-activedescendant`), and the highlighted one is `aria-selected`. Its words are its content; `value` is
 * what choosing it gives. `<lg-option value="Maren">Maren</lg-option>`. In a Select, the current choice is also marked
 * chosen: a 2px `arcana-glow` bar at its start and weight 600 (Selected, Standards · States).
 */
@Component({
  selector: 'lg-option',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'option',
    class: 'lg-option',
    '[id]': 'id',
    '[class.is-active]': 'active()',
    '[class.is-chosen]': '!!parent?.isChosen(this)',
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
