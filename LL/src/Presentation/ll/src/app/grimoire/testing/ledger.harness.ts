import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgLedgerHarnessFilters extends BaseHarnessFilters {
  /** The Ledger's title ("Offense"). */
  title?: string | RegExp;
}

/** The keys a player uses on a Ledger. */
export type LgLedgerKey = 'ArrowUp' | 'ArrowDown' | 'Home' | 'End' | 'Escape';

const KEYS: Record<LgLedgerKey, TestKey> = {
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Escape: TestKey.ESCAPE,
};

/** `lg-ledger`: the labelled value list. Rows are found by their label ("Power"). */
export class LgLedgerHarness extends ComponentHarness {
  static hostSelector = 'lg-ledger';

  static with(
    options: LgLedgerHarnessFilters = {},
  ): HarnessPredicate<LgLedgerHarness> {
    return new HarnessPredicate(LgLedgerHarness, options).addOption(
      'title',
      options.title,
      (harness, title) =>
        HarnessPredicate.stringMatches(harness.getTitle(), title),
    );
  }

  private readonly title = this.locatorFor('.lg-ledger__title');
  private readonly rows = this.locatorForAll('.lg-ledger__row');
  private readonly labels = this.locatorForAll(
    '.lg-ledger__row .lg-ledger__labeltext',
  );

  async getTitle(): Promise<string> {
    return (await this.title()).text();
  }

  /** Every row's label, in order. */
  async getRowLabels(): Promise<string[]> {
    const labels = await this.labels();
    return parallel(() => labels.map((label) => label.text()));
  }

  /** A click on the row. A row that explains itself takes focus, as a pointer press does in the browser. */
  async click(label: string): Promise<void> {
    const row = await this.row(label);
    const focusable = (await row.getAttribute('tabindex')) !== null;
    if (focusable && !(await row.isFocused())) await row.focus();
    await row.click();
  }

  /** A key pressed while focus is on a row. */
  async pressKey(key: LgLedgerKey): Promise<void> {
    const rows = await this.rows();
    const focused = await parallel(() => rows.map((row) => row.isFocused()));
    const row = rows[focused.indexOf(true)];
    if (!row) throw Error('No row has focus.');
    await row.sendKeys(KEYS[key]);
  }

  /** The row that has focus, or null. */
  async focusedLabel(): Promise<string | null> {
    return (await this.labelsWhere((row) => row.isFocused()))[0] ?? null;
  }

  /** The rows in the Tab order: the rows that explain themselves are one tab stop, so there should be one. */
  async tabStopLabels(): Promise<string[]> {
    return this.labelsWhere(
      async (row) => (await row.getAttribute('tabindex')) === '0',
    );
  }

  /** The rows whose explanation a click pinned open. */
  async pinnedLabels(): Promise<string[]> {
    return this.labelsWhere((row) => row.hasClass('is-pinned'));
  }

  /** The rows whose explanation Escape put away while the row keeps focus or hover. */
  async dismissedLabels(): Promise<string[]> {
    return this.labelsWhere((row) => row.hasClass('is-dismissed'));
  }

  private async row(label: string): Promise<TestElement> {
    const [rows, labels] = await parallel(() => [
      this.rows(),
      this.getRowLabels(),
    ]);
    const i = labels.indexOf(label);
    if (i < 0) {
      throw Error(
        `No row labelled "${label}"; the Ledger has ${labels.join(', ')}.`,
      );
    }
    return rows[i];
  }

  private async labelsWhere(
    test: (row: TestElement) => Promise<boolean>,
  ): Promise<string[]> {
    const [rows, labels] = await parallel(() => [
      this.rows(),
      this.getRowLabels(),
    ]);
    const hits = await parallel(() => rows.map(test));
    return labels.filter((_, i) => hits[i]);
  }
}
