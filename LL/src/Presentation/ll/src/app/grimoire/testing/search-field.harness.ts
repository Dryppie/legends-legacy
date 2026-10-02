import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgSearchFieldHarnessFilters extends BaseHarnessFilters {
  /** The field's accessible name: its `label`, else its placeholder. */
  label?: string | RegExp;
}

/** The keys a player uses in a SearchField, besides typing. */
export type LgSearchFieldKey = 'ArrowUp' | 'ArrowDown' | 'Enter' | 'Escape';

const KEYS: Record<LgSearchFieldKey, TestKey> = {
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Enter: TestKey.ENTER,
  Escape: TestKey.ESCAPE,
};

/** `lg-search-field`: search with suggestions, an ARIA combobox. */
export class LgSearchFieldHarness extends ComponentHarness {
  static hostSelector = 'lg-search-field';

  static with(
    options: LgSearchFieldHarnessFilters = {},
  ): HarnessPredicate<LgSearchFieldHarness> {
    return new HarnessPredicate(LgSearchFieldHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly input = this.locatorFor('input[role=combobox]');
  private readonly options = this.locatorForAll('[role=listbox] [role=option]');
  private readonly note = this.locatorForOptional(
    '[role=listbox] .lg-search__note',
  );

  async getLabel(): Promise<string | null> {
    return (await this.input()).getAttribute('aria-label');
  }

  /** A click in the field, which focuses it. */
  async click(): Promise<void> {
    const input = await this.input();
    if (!(await input.isFocused())) await input.focus();
    await input.click();
  }

  /** Types at the end of what the field holds. */
  async type(text: string): Promise<void> {
    await (await this.input()).sendKeys(text);
  }

  async pressKey(key: LgSearchFieldKey): Promise<void> {
    await (await this.input()).sendKeys(KEYS[key]);
  }

  /** What the field holds. */
  async getValue(): Promise<string> {
    return (await this.input()).getProperty<string>('value');
  }

  async isFocused(): Promise<boolean> {
    return (await this.input()).isFocused();
  }

  /** Whether the suggestions list is open (`aria-expanded`). */
  async isOpen(): Promise<boolean> {
    return (
      (await (await this.input()).getAttribute('aria-expanded')) === 'true'
    );
  }

  /** The suggestions on offer, in order. */
  async getSuggestions(): Promise<string[]> {
    const options = await this.options();
    return parallel(() => options.map((option) => option.text()));
  }

  /** The suggestion the arrow keys highlighted (the combobox's active descendant), or null. */
  async highlightedSuggestion(): Promise<string | null> {
    const [input, options] = await parallel(() => [
      this.input(),
      this.options(),
    ]);
    const active = await input.getAttribute('aria-activedescendant');
    if (!active) return null;
    const ids = await parallel(() =>
      options.map((option) => option.getAttribute('id')),
    );
    const i = ids.indexOf(active);
    return i < 0 ? null : options[i].text();
  }

  /** The list's note in place of suggestions ("No matching players", "Finding players…"), or null. */
  async getNote(): Promise<string | null> {
    const note = await this.note();
    return note ? note.text() : null;
  }
}
