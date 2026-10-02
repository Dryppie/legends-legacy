import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgEntryListHarnessFilters extends BaseHarnessFilters {
  /** The list's accessible name ("Entries" unless `label` is set). */
  label?: string | RegExp;
}

/** The keys a player uses on an EntryList. */
export type LgEntryListKey =
  | 'ArrowUp'
  | 'ArrowDown'
  | 'Home'
  | 'End'
  | 'Enter'
  | ' ';

const KEYS: Record<LgEntryListKey, TestKey | string> = {
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Enter: TestKey.ENTER,
  ' ': ' ',
};

/** `lg-entry-list`: the browsable name list. Entries are found by their name. */
export class LgEntryListHarness extends ComponentHarness {
  static hostSelector = 'lg-entry-list';

  static with(
    options: LgEntryListHarnessFilters = {},
  ): HarnessPredicate<LgEntryListHarness> {
    return new HarnessPredicate(LgEntryListHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly list = this.locatorFor('[role=listbox]');
  private readonly entries = this.locatorForAll('[role=option]');
  private readonly names = this.locatorForAll(
    '[role=option] .lg-entrylist__name',
  );

  async getLabel(): Promise<string | null> {
    return (await this.list()).getAttribute('aria-label');
  }

  /** Every entry's name, in order. */
  async getNames(): Promise<string[]> {
    const names = await this.names();
    return parallel(() => names.map((name) => name.text()));
  }

  /** A click on the entry: the press focuses it first, as a pointer press does in the browser. */
  async click(name: string): Promise<void> {
    const entry = await this.entry(name);
    if (!(await entry.isFocused())) await entry.focus();
    await entry.click();
  }

  /** The pointer moves onto the entry. */
  async hover(name: string): Promise<void> {
    await (await this.entry(name)).hover();
  }

  /** The pointer moves off the entry. */
  async mouseAway(name: string): Promise<void> {
    await (await this.entry(name)).mouseAway();
  }

  /** A key pressed while focus is on an entry. */
  async pressKey(key: LgEntryListKey): Promise<void> {
    const entry = await this.focusedEntry();
    if (!entry) throw Error('No entry has focus.');
    await entry.sendKeys(KEYS[key]);
  }

  /** The entry that has focus, or null. */
  async focusedName(): Promise<string | null> {
    return this.nameWhere((entry) => entry.isFocused());
  }

  /** The selected entry, or null. */
  async selectedName(): Promise<string | null> {
    return this.nameWhere(
      async (entry) => (await entry.getAttribute('aria-selected')) === 'true',
    );
  }

  /** The entries in the Tab order: the list is one tab stop, so there should be exactly one. */
  async tabStopNames(): Promise<string[]> {
    const [entries, names] = await parallel(() => [
      this.entries(),
      this.getNames(),
    ]);
    const stops = await parallel(() =>
      entries.map(
        async (entry) => (await entry.getAttribute('tabindex')) === '0',
      ),
    );
    return names.filter((_, i) => stops[i]);
  }

  /** Locked: unavailable, but still focusable. */
  async isLocked(name: string): Promise<boolean> {
    const entry = await this.entry(name);
    return (await entry.getAttribute('aria-disabled')) === 'true';
  }

  private async entry(name: string): Promise<TestElement> {
    const [entries, names] = await parallel(() => [
      this.entries(),
      this.getNames(),
    ]);
    const i = names.indexOf(name);
    if (i < 0) {
      throw Error(
        `No entry named "${name}"; the list has ${names.join(', ')}.`,
      );
    }
    return entries[i];
  }

  private async focusedEntry(): Promise<TestElement | null> {
    const entries = await this.entries();
    const focused = await parallel(() =>
      entries.map((entry) => entry.isFocused()),
    );
    return entries[focused.indexOf(true)] ?? null;
  }

  private async nameWhere(
    test: (entry: TestElement) => Promise<boolean>,
  ): Promise<string | null> {
    const [entries, names] = await parallel(() => [
      this.entries(),
      this.getNames(),
    ]);
    const hits = await parallel(() => entries.map(test));
    const i = hits.indexOf(true);
    return i < 0 ? null : names[i];
  }
}
