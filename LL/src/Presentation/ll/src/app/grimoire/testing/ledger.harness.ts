import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
  parallel,
} from '@angular/cdk/testing';
import { LgTipHarness, lgDescriptionOf } from './tip.harness';

export interface LgLedgerHarnessFilters extends BaseHarnessFilters {
  /** The Ledger's heading ("Offense"). */
  heading?: string | RegExp;
}

/** The keys a player uses on a Ledger. Letters move to the next row whose label starts with them (`type`). */
export type LgLedgerKey = 'ArrowUp' | 'ArrowDown' | 'Home' | 'End' | 'Escape';

const KEYS: Record<LgLedgerKey, TestKey> = {
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Escape: TestKey.ESCAPE,
};

/** A row's explanation, as the tip shows it. */
export interface LgLedgerExplanation {
  title: string | null;
  text: string;
  meta: string | null;
}

/** One `div[lgLedgerRow]`. */
export class LgLedgerRowHarness extends ComponentHarness {
  static hostSelector = '.lg-ledger__row';

  private readonly label = this.locatorFor('.lg-ledger__labeltext');
  private readonly value = this.locatorFor('.lg-ledger__value');
  private readonly sub = this.locatorForOptional('.lg-ledger__sub');

  async getLabel(): Promise<string> {
    return (await this.label()).text();
  }

  /** The value as it reads: "1,284", "84 HP/5s" (with its non-breaking space), "—". */
  async getValue(): Promise<string> {
    return (await this.value()).text();
  }

  /** The second line: the change, the sub text, or both ("▲ +12 · now 142"); null without one. */
  async getSub(): Promise<string | null> {
    const sub = await this.sub();
    return sub ? (await sub.text()).replace(/\s+/g, ' ').trim() : null;
  }

  /** Whether the row explains itself, and so takes part in the Ledger's tab stop. */
  async explains(): Promise<boolean> {
    return (await (await this.host()).getAttribute('tabindex')) !== null;
  }

  async isTabStop(): Promise<boolean> {
    return (await (await this.host()).getAttribute('tabindex')) === '0';
  }

  async isFocused(): Promise<boolean> {
    return (await this.host()).isFocused();
  }

  async isMuted(): Promise<boolean> {
    return (await this.host()).hasClass('is-muted');
  }

  /** Whether its explanation shows: on hover, on keyboard focus, or pinned. */
  async isExplained(): Promise<boolean> {
    return (await this.host()).hasClass('is-explained');
  }

  /** Whether a press pinned its explanation open. */
  async isPinned(): Promise<boolean> {
    return (await this.host()).hasClass('is-pinned');
  }

  /** What screen readers hear as its description: its explanation, or null. */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.host(),
      this.documentRootLocatorFactory(),
    );
  }

  /** A click, which focuses a row that explains itself first, as a pointer press does in the browser. */
  async click(): Promise<void> {
    const host = await this.host();
    if ((await this.explains()) && !(await host.isFocused()))
      await host.focus();
    await host.click();
  }

  async hover(): Promise<void> {
    await (await this.host()).hover();
  }

  async mouseAway(): Promise<void> {
    await (await this.host()).mouseAway();
  }

  async focus(): Promise<void> {
    await (await this.host()).focus();
  }

  async press(key: LgLedgerKey): Promise<void> {
    await (await this.host()).sendKeys(KEYS[key]);
  }

  async type(letters: string): Promise<void> {
    await (await this.host()).sendKeys(letters);
  }
}

/** `lg-ledger`: the labelled value list. Its rows are found by their label ("Power"). */
export class LgLedgerHarness extends ComponentHarness {
  static hostSelector = 'lg-ledger';

  static with(
    options: LgLedgerHarnessFilters = {},
  ): HarnessPredicate<LgLedgerHarness> {
    return new HarnessPredicate(LgLedgerHarness, options).addOption(
      'heading',
      options.heading,
      (harness, heading) =>
        HarnessPredicate.stringMatches(harness.getHeading(), heading),
    );
  }

  private readonly heading = this.locatorFor('.lg-ledger__title');
  private readonly rowHarnesses = this.locatorForAll(LgLedgerRowHarness);
  private readonly tip = this.documentRootLocatorFactory().locatorForOptional(
    LgTipHarness.with({ shown: true }),
  );

  async getHeading(): Promise<string> {
    return (await this.heading()).text();
  }

  /** 2 when the rows are set two-up, otherwise 1. */
  async getColumns(): Promise<number> {
    return (await (await this.host()).getAttribute('data-columns')) === '2'
      ? 2
      : 1;
  }

  /** Every row, in order. */
  async getRows(): Promise<LgLedgerRowHarness[]> {
    return this.rowHarnesses();
  }

  /** The row labelled `label`. */
  async getRow(label: string): Promise<LgLedgerRowHarness> {
    const [rows, labels] = await Promise.all([
      this.rowHarnesses(),
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

  /** Every row's label, in order. */
  async getRowLabels(): Promise<string[]> {
    const rows = await this.rowHarnesses();
    return parallel(() => rows.map((row) => row.getLabel()));
  }

  /** A click on a row. */
  async click(label: string): Promise<void> {
    await (await this.getRow(label)).click();
  }

  /** A key pressed while focus is on one of the rows. */
  async pressKey(key: LgLedgerKey): Promise<void> {
    await (await this.focusedRow()).press(key);
  }

  /** Letters typed while focus is on one of the rows: typeahead by label. */
  async type(letters: string): Promise<void> {
    await (await this.focusedRow()).type(letters);
  }

  /** The row that has focus, or null. */
  async focusedLabel(): Promise<string | null> {
    return (await this.labelsWhere((row) => row.isFocused()))[0] ?? null;
  }

  /** The rows in the Tab order: the rows that explain themselves are one tab stop, so there should be one. */
  async tabStopLabels(): Promise<string[]> {
    return this.labelsWhere((row) => row.isTabStop());
  }

  /** The rows whose explanation shows: one at most, since the page has one tip. */
  async explainedLabels(): Promise<string[]> {
    return this.labelsWhere((row) => row.isExplained());
  }

  /** The rows whose explanation a press pinned open. */
  async pinnedLabels(): Promise<string[]> {
    return this.labelsWhere((row) => row.isPinned());
  }

  /** The explanation showing for one of this Ledger's rows, as the tip shows it; null when none shows. */
  async getExplanation(): Promise<LgLedgerExplanation | null> {
    if (!(await this.explainedLabels()).length) return null;
    const tip = await this.tip();
    if (!tip) return null;
    const [title, text, meta] = await parallel(() => [
      tip.getTitle(),
      tip.getReason(),
      tip.getMeta(),
    ]);
    return { title, text, meta };
  }

  private async focusedRow(): Promise<LgLedgerRowHarness> {
    const rows = await this.rowHarnesses();
    const focused = await parallel(() => rows.map((row) => row.isFocused()));
    const row = rows[focused.indexOf(true)];
    if (!row) throw Error('No row has focus.');
    return row;
  }

  private async labelsWhere(
    test: (row: LgLedgerRowHarness) => Promise<boolean>,
  ): Promise<string[]> {
    const rows = await this.rowHarnesses();
    const [labels, hits] = await Promise.all([
      parallel(() => rows.map((row) => row.getLabel())),
      parallel(() => rows.map(test)),
    ]);
    return labels.filter((_, i) => hits[i]);
  }
}
