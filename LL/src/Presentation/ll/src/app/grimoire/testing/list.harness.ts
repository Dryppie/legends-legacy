import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgListHarnessFilters extends BaseHarnessFilters {
  /** The list's accessible name. */
  label?: string | RegExp;
}

/** The keys a player uses on a List. */
export type LgListKey =
  | 'ArrowUp'
  | 'ArrowDown'
  | 'ArrowLeft'
  | 'ArrowRight'
  | 'Home'
  | 'End'
  | 'Enter';

const KEYS: Record<LgListKey, TestKey> = {
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  ArrowLeft: TestKey.LEFT_ARROW,
  ArrowRight: TestKey.RIGHT_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Enter: TestKey.ENTER,
};

/** A place in the list that takes focus: a row's name (its button, when interactive) or its trailing action. */
export interface LgListSpot {
  row: string;
  on: 'name' | 'action';
}

interface Row {
  title: string;
  name: TestElement | null;
  hit: TestElement | null;
  action: TestElement | null;
}

/** `lg-list` and its `li[lgListRow]` rows. Rows are found by their title. */
export class LgListHarness extends ComponentHarness {
  static hostSelector = 'lg-list';

  static with(
    options: LgListHarnessFilters = {},
  ): HarnessPredicate<LgListHarness> {
    return new HarnessPredicate(LgListHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly list = this.locatorFor('[role=list]');
  // In DOM order, so each row's parts follow the row itself.
  private readonly parts = this.locatorForAll(
    '.lg-listrow',
    '.lg-listrow__hit',
    '.lg-listrow__name',
    '.lg-listrow__trail button',
    '.lg-listrow__trail a',
  );

  async getLabel(): Promise<string | null> {
    return (await this.list()).getAttribute('aria-label');
  }

  /** Every row's title, in order. */
  async getRowTitles(): Promise<string[]> {
    return (await this.rows()).map((row) => row.title);
  }

  /** A click on the row's name: the press focuses it first, as a pointer press does in the browser. */
  async click(title: string): Promise<void> {
    const hit = (await this.row(title)).hit;
    if (!hit) throw Error(`Row "${title}" is not interactive.`);
    if (!(await hit.isFocused())) await hit.focus();
    await hit.click();
  }

  /** A key pressed where focus is. Enter also presses the focused button, as the browser does. */
  async pressKey(key: LgListKey): Promise<void> {
    const focused = await this.focusedElement();
    if (!focused) throw Error('Nothing in the list has focus.');
    await focused.sendKeys(KEYS[key]);
    if (key === 'Enter' && (await focused.matchesSelector('button'))) {
      await focused.click();
    }
  }

  /** Where focus is in the list, or null. */
  async getFocus(): Promise<LgListSpot | null> {
    return (await this.spotsWhere((el) => el.isFocused()))[0] ?? null;
  }

  /** Where Tab enters the list: one row's name, and never a trailing action. */
  async getTabStops(): Promise<LgListSpot[]> {
    return this.spotsWhere(
      async (el) => (await el.getProperty<number>('tabIndex')) >= 0,
    );
  }

  /** The rows marked selected (`aria-pressed` on an interactive row's name). */
  async selectedTitles(): Promise<string[]> {
    const rows = await this.rows();
    const pressed = await parallel(() =>
      rows.map((row) => row.hit?.getAttribute('aria-pressed') ?? null),
    );
    return rows.filter((_, i) => pressed[i] === 'true').map((row) => row.title);
  }

  private async rows(): Promise<Row[]> {
    const parts = await this.parts();
    const kinds = await parallel(() =>
      parts.map(async (part) => {
        const [row, hit, name] = await parallel(() => [
          part.hasClass('lg-listrow'),
          part.hasClass('lg-listrow__hit'),
          part.hasClass('lg-listrow__name'),
        ]);
        return row ? 'row' : hit ? 'hit' : name ? 'name' : 'action';
      }),
    );
    const rows: Row[] = [];
    kinds.forEach((kind, i) => {
      if (kind === 'row') {
        rows.push({ title: '', name: null, hit: null, action: null });
      } else if (rows.length) {
        rows[rows.length - 1][kind] = parts[i];
      }
    });
    const titles = await parallel(() =>
      rows.map((row) => row.name?.text() ?? ''),
    );
    rows.forEach((row, i) => (row.title = titles[i]));
    return rows;
  }

  private async row(title: string): Promise<Row> {
    const rows = await this.rows();
    const row = rows.find((r) => r.title === title);
    if (!row) {
      const titles = rows.map((r) => r.title).join(', ');
      throw Error(`No row titled "${title}"; the list has ${titles}.`);
    }
    return row;
  }

  private async focusedElement(): Promise<TestElement | null> {
    const els = (await this.rows())
      .flatMap((row) => [row.hit, row.action])
      .filter((el): el is TestElement => !!el);
    const focused = await parallel(() => els.map((el) => el.isFocused()));
    return els[focused.indexOf(true)] ?? null;
  }

  private async spotsWhere(
    test: (el: TestElement) => Promise<boolean>,
  ): Promise<LgListSpot[]> {
    const spots: { spot: LgListSpot; el: TestElement }[] = [];
    for (const row of await this.rows()) {
      if (row.hit) {
        spots.push({ spot: { row: row.title, on: 'name' }, el: row.hit });
      }
      if (row.action) {
        spots.push({ spot: { row: row.title, on: 'action' }, el: row.action });
      }
    }
    const hits = await parallel(() => spots.map(({ el }) => test(el)));
    return spots.filter((_, i) => hits[i]).map(({ spot }) => spot);
  }
}
