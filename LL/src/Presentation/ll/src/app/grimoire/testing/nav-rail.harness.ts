import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgNavRailHarnessFilters extends BaseHarnessFilters {
  /** The navigation's accessible name ("Game" unless `label` is set). */
  label?: string | RegExp;
}

/** The keys a player uses on a NavRail item. */
export type LgNavRailKey = 'Enter' | 'Escape';

const KEYS: Record<LgNavRailKey, TestKey> = {
  Enter: TestKey.ENTER,
  Escape: TestKey.ESCAPE,
};

/** `lg-nav-rail`: the main navigation, its `a[lgNavItem]` destinations found by their title ("Inventory"). */
export class LgNavRailHarness extends ComponentHarness {
  static hostSelector = 'lg-nav-rail';

  static with(
    options: LgNavRailHarnessFilters = {},
  ): HarnessPredicate<LgNavRailHarness> {
    return new HarnessPredicate(LgNavRailHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly items = this.locatorForAll('a.lg-rail__item');
  private readonly titles = this.locatorForAll(
    'a.lg-rail__item .lg-rail__title',
  );

  async getLabel(): Promise<string | null> {
    return (await this.host()).getAttribute('aria-label');
  }

  /** Each section's label, in order. */
  async getSectionLabels(): Promise<string[]> {
    const labels = await this.locatorForAll('.lg-rail__group')();
    return parallel(() => labels.map((label) => label.text()));
  }

  /** Every item's title, in order, across the sections. */
  async getItemTitles(): Promise<string[]> {
    const titles = await this.titles();
    return parallel(() => titles.map((title) => title.text()));
  }

  /** A click on the item: the press focuses it first, as a pointer press does in the browser. */
  async click(title: string): Promise<void> {
    const item = await this.item(title);
    if (!(await item.isFocused())) await item.focus();
    await item.click();
  }

  /** Focus moves to the item, as Tab does. */
  async focusItem(title: string): Promise<void> {
    await (await this.item(title)).focus();
  }

  /** The pointer moves onto the item. */
  async hover(title: string): Promise<void> {
    await (await this.item(title)).hover();
  }

  /** The pointer moves off the item. */
  async mouseAway(title: string): Promise<void> {
    await (await this.item(title)).mouseAway();
  }

  /** A key pressed while focus is on an item. Enter on a link with an href also clicks it, as the browser does. */
  async pressKey(key: LgNavRailKey): Promise<void> {
    const items = await this.items();
    const focused = await parallel(() => items.map((item) => item.isFocused()));
    const item = items[focused.indexOf(true)];
    if (!item) throw Error('No item has focus.');
    await item.sendKeys(KEYS[key]);
    if (key === 'Enter' && (await item.getAttribute('href')) !== null)
      await item.click();
  }

  /** The item that has focus, or null. */
  async focusedTitle(): Promise<string | null> {
    return (await this.titlesWhere((item) => item.isFocused()))[0] ?? null;
  }

  /** The current page's item (`aria-current`), or null. */
  async currentTitle(): Promise<string | null> {
    const [current] = await this.titlesWhere(
      async (item) => (await item.getAttribute('aria-current')) === 'page',
    );
    return current ?? null;
  }

  /** Locked: unavailable, but still in the Tab order. */
  async isLocked(title: string): Promise<boolean> {
    const item = await this.item(title);
    return (await item.getAttribute('aria-disabled')) === 'true';
  }

  private async item(title: string): Promise<TestElement> {
    const [items, titles] = await parallel(() => [
      this.items(),
      this.getItemTitles(),
    ]);
    const i = titles.indexOf(title);
    if (i < 0) {
      throw Error(
        `No item titled "${title}"; the rail has ${titles.join(', ')}.`,
      );
    }
    return items[i];
  }

  private async titlesWhere(
    test: (item: TestElement) => Promise<boolean>,
  ): Promise<string[]> {
    const [items, titles] = await parallel(() => [
      this.items(),
      this.getItemTitles(),
    ]);
    const hits = await parallel(() => items.map(test));
    return titles.filter((_, i) => hits[i]);
  }
}
