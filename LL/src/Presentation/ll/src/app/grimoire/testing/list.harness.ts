import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';
import { lgDescriptionOf } from './tip.harness';

export interface LgListHarnessFilters extends BaseHarnessFilters {
  /** The list's accessible name. */
  label?: string | RegExp;
}

/** The keys a player uses on a List. Letters move to the next row whose name starts with them (`type`). */
export type LgListKey =
  | 'ArrowUp'
  | 'ArrowDown'
  | 'ArrowLeft'
  | 'ArrowRight'
  | 'Home'
  | 'End'
  | 'Enter'
  | ' ';

const KEYS: Record<LgListKey, TestKey | string> = {
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  ArrowLeft: TestKey.LEFT_ARROW,
  ArrowRight: TestKey.RIGHT_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Enter: TestKey.ENTER,
  ' ': ' ',
};

/** A place in the list that takes focus: a row (the option, or its action) or the control in its trailing region. */
export interface LgListSpot {
  row: string;
  on: 'row' | 'trailing';
}

/**
 * One `li[lgListRow]`. Its focus target is the row itself in a selectable List (an option), else its action
 * (`button[lgListRowAction]`), if it has one.
 */
export class LgListRowHarness extends ComponentHarness {
  static hostSelector = '.lg-listrow';

  private readonly nameEl = this.locatorFor('.lg-listrow__name');
  private readonly metaEl = this.locatorForOptional('.lg-listrow__meta');
  private readonly action = this.locatorForOptional('.lg-listrow__action');
  private readonly trailingControl = this.locatorForOptional(
    'lg-list-row-trailing button, lg-list-row-trailing a',
  );

  async getName(): Promise<string> {
    return (await this.nameEl()).text();
  }

  async getMeta(): Promise<string | null> {
    return (await this.metaEl())?.text() ?? null;
  }

  /** Whether the row is an option (in a selectable List). */
  async isOption(): Promise<boolean> {
    return (await (await this.host()).getAttribute('role')) === 'option';
  }

  async hasAction(): Promise<boolean> {
    return !!(await this.action());
  }

  /** Selected: `aria-selected` on an option, `aria-pressed` on an action, else the selected look. */
  async isSelected(): Promise<boolean> {
    const host = await this.host();
    if (await this.isOption()) {
      return (await host.getAttribute('aria-selected')) === 'true';
    }
    const pressed = (await this.action())
      ? await (await this.action())!.getAttribute('aria-pressed')
      : null;
    return pressed != null ? pressed === 'true' : host.hasClass('is-selected');
  }

  /** Blocked (locked, unavailable, restricted): unavailable, but still focusable. */
  async isBlocked(): Promise<boolean> {
    const target = await this.target();
    return !!target && (await target.getAttribute('aria-disabled')) === 'true';
  }

  /** Whether Tab lands on the row. */
  async isTabStop(): Promise<boolean> {
    const target = await this.target();
    return !!target && (await target.getAttribute('tabindex')) === '0';
  }

  /** Whether Tab lands on the trailing region's control. */
  async isTrailingTabStop(): Promise<boolean> {
    const control = await this.trailingControl();
    return !!control && (await control.getProperty<number>('tabIndex')) >= 0;
  }

  /** Where focus is in the row, or null. */
  async getFocus(): Promise<'row' | 'trailing' | null> {
    const [target, control] = await parallel(() => [
      this.target(),
      this.trailingControl(),
    ]);
    if (target && (await target.isFocused())) return 'row';
    if (control && (await control.isFocused())) return 'trailing';
    return null;
  }

  /** What screen readers hear as the row's description: a blocked row's reason, or null. */
  async getDescription(): Promise<string | null> {
    const target = await this.target();
    return target
      ? lgDescriptionOf(target, this.documentRootLocatorFactory())
      : null;
  }

  /** A click on the row: the press focuses it first, as a pointer press does in the browser. */
  async click(): Promise<void> {
    const target = await this.requireTarget();
    if (!(await target.isFocused())) await target.focus();
    await target.click();
  }

  async hover(): Promise<void> {
    await (await this.requireTarget()).hover();
  }

  async mouseAway(): Promise<void> {
    await (await this.requireTarget()).mouseAway();
  }

  /** A key pressed where focus is in the row. Enter and Space also press a focused button or link, as the browser does. */
  async press(key: LgListKey): Promise<void> {
    const el = await this.focusedElement();
    if (!el) throw Error(`Nothing in row "${await this.getName()}" has focus.`);
    await el.sendKeys(KEYS[key]);
    if (
      (key === 'Enter' || key === ' ') &&
      (await el.matchesSelector('button, a[href]'))
    ) {
      await el.click();
    }
  }

  async type(letters: string): Promise<void> {
    const el = await this.focusedElement();
    if (!el) throw Error(`Nothing in row "${await this.getName()}" has focus.`);
    await el.sendKeys(letters);
  }

  private async target(): Promise<TestElement | null> {
    return (await this.isOption()) ? this.host() : this.action();
  }

  private async requireTarget(): Promise<TestElement> {
    const target = await this.target();
    if (!target) {
      throw Error(
        `Row "${await this.getName()}" takes no focus: it has no action and the List is not selectable.`,
      );
    }
    return target;
  }

  private async focusedElement(): Promise<TestElement | null> {
    const [target, control] = await parallel(() => [
      this.target(),
      this.trailingControl(),
    ]);
    if (target && (await target.isFocused())) return target;
    if (control && (await control.isFocused())) return control;
    return null;
  }
}

/** `lg-list` and its `li[lgListRow]` rows. Rows are found by their name. */
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

  private readonly list = this.locatorFor('ul');
  private readonly rowHarnesses = this.locatorForAll(LgListRowHarness);

  async getLabel(): Promise<string | null> {
    return (await this.list()).getAttribute('aria-label');
  }

  /** `list`, or `listbox` when the List is selectable. */
  async getRole(): Promise<string | null> {
    return (await this.list()).getAttribute('role');
  }

  /** Every row, in order. */
  async getRows(): Promise<LgListRowHarness[]> {
    return this.rowHarnesses();
  }

  /** Every row's name, in order. */
  async getRowNames(): Promise<string[]> {
    const rows = await this.rowHarnesses();
    return parallel(() => rows.map((row) => row.getName()));
  }

  /** The row named `name`. */
  async getRow(name: string): Promise<LgListRowHarness> {
    const [rows, names] = await Promise.all([
      this.rowHarnesses(),
      this.getRowNames(),
    ]);
    const i = names.indexOf(name);
    if (i < 0) {
      throw Error(`No row named "${name}"; the list has ${names.join(', ')}.`);
    }
    return rows[i];
  }

  async click(name: string): Promise<void> {
    await (await this.getRow(name)).click();
  }

  async hover(name: string): Promise<void> {
    await (await this.getRow(name)).hover();
  }

  async mouseAway(name: string): Promise<void> {
    await (await this.getRow(name)).mouseAway();
  }

  /** A key pressed where focus is in the list. */
  async pressKey(key: LgListKey): Promise<void> {
    await (await this.focusedRow()).press(key);
  }

  /** Letters typed where focus is in the list: typeahead by name. */
  async type(letters: string): Promise<void> {
    await (await this.focusedRow()).type(letters);
  }

  /** Where focus is in the list, or null. */
  async getFocus(): Promise<LgListSpot | null> {
    const rows = await this.rowHarnesses();
    const [names, spots] = await Promise.all([
      parallel(() => rows.map((row) => row.getName())),
      parallel(() => rows.map((row) => row.getFocus())),
    ]);
    const i = spots.findIndex((spot) => spot != null);
    return i < 0 ? null : { row: names[i], on: spots[i]! };
  }

  /** The focused row's name, or null. */
  async focusedName(): Promise<string | null> {
    return (await this.getFocus())?.row ?? null;
  }

  /** Where Tab enters the list: one row, and never the trailing control of a row with an action. */
  async getTabStops(): Promise<LgListSpot[]> {
    const rows = await this.rowHarnesses();
    const [names, rowStops, trailStops] = await Promise.all([
      parallel(() => rows.map((row) => row.getName())),
      parallel(() => rows.map((row) => row.isTabStop())),
      parallel(() => rows.map((row) => row.isTrailingTabStop())),
    ]);
    const spots: LgListSpot[] = [];
    names.forEach((row, i) => {
      if (rowStops[i]) spots.push({ row, on: 'row' });
      if (trailStops[i]) spots.push({ row, on: 'trailing' });
    });
    return spots;
  }

  /** The selected rows' names. */
  async selectedNames(): Promise<string[]> {
    return this.namesWhere((row) => row.isSelected());
  }

  /** The blocked rows' names. */
  async blockedNames(): Promise<string[]> {
    return this.namesWhere((row) => row.isBlocked());
  }

  private async focusedRow(): Promise<LgListRowHarness> {
    const rows = await this.rowHarnesses();
    const spots = await parallel(() => rows.map((row) => row.getFocus()));
    const row = rows[spots.findIndex((spot) => spot != null)];
    if (!row) throw Error('Nothing in the list has focus.');
    return row;
  }

  private async namesWhere(
    test: (row: LgListRowHarness) => Promise<boolean>,
  ): Promise<string[]> {
    const rows = await this.rowHarnesses();
    const [names, hits] = await Promise.all([
      parallel(() => rows.map((row) => row.getName())),
      parallel(() => rows.map(test)),
    ]);
    return names.filter((_, i) => hits[i]);
  }
}
