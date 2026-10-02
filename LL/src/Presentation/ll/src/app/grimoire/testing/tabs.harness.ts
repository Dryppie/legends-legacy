import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgTabsHarnessFilters extends BaseHarnessFilters {
  /** The tablist's accessible name. */
  label?: string | RegExp;
}

/** The keys a player uses on Tabs. */
export type LgTabsKey =
  | 'ArrowLeft'
  | 'ArrowRight'
  | 'ArrowUp'
  | 'ArrowDown'
  | 'Home'
  | 'End';

const KEYS: Record<LgTabsKey, TestKey> = {
  ArrowLeft: TestKey.LEFT_ARROW,
  ArrowRight: TestKey.RIGHT_ARROW,
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
};

/** `lg-tabs`: the tabs and their panels. Tabs are found by their words (without the count). */
export class LgTabsHarness extends ComponentHarness {
  static hostSelector = 'lg-tabs';

  static with(
    options: LgTabsHarnessFilters = {},
  ): HarnessPredicate<LgTabsHarness> {
    return new HarnessPredicate(LgTabsHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly tablist = this.locatorFor('[role=tablist]');
  private readonly tabs = this.locatorForAll('[role=tab]');
  private readonly panels = this.locatorForAll('[role=tabpanel]');

  async getLabel(): Promise<string | null> {
    return (await this.tablist()).getAttribute('aria-label');
  }

  /** Every tab's words, in order. */
  async getTabLabels(): Promise<string[]> {
    const tabs = await this.tabs();
    return parallel(() =>
      tabs.map((tab) => tab.text({ exclude: '.lg-tab__count' })),
    );
  }

  /** A click on the tab: the press focuses it first, as a pointer press does in the browser. */
  async click(label: string): Promise<void> {
    const tab = await this.tab(label);
    if (!(await tab.isFocused())) await tab.focus();
    await tab.click();
  }

  /** A key pressed while focus is on a tab. */
  async pressKey(key: LgTabsKey): Promise<void> {
    const tabs = await this.tabs();
    const focused = await parallel(() => tabs.map((tab) => tab.isFocused()));
    const tab = tabs[focused.indexOf(true)];
    if (!tab) throw Error('No tab has focus.');
    await tab.sendKeys(KEYS[key]);
  }

  /** The tab that has focus, or null. */
  async focusedLabel(): Promise<string | null> {
    return this.labelWhere((tab) => tab.isFocused());
  }

  /** The selected tab, or null. */
  async selectedLabel(): Promise<string | null> {
    return this.labelWhere(
      async (tab) => (await tab.getAttribute('aria-selected')) === 'true',
    );
  }

  /** The tabs in the Tab order: the strip is one tab stop, so there should be exactly one. */
  async tabStopLabels(): Promise<string[]> {
    const [tabs, labels] = await parallel(() => [
      this.tabs(),
      this.getTabLabels(),
    ]);
    const stops = await parallel(() =>
      tabs.map(async (tab) => (await tab.getAttribute('tabindex')) === '0'),
    );
    return labels.filter((_, i) => stops[i]);
  }

  /** The panel that shows, as the words of the tab that labels it and its text; null without one. */
  async getShownPanel(): Promise<{ tab: string | null; text: string } | null> {
    const panels = await this.panels();
    const hidden = await parallel(() =>
      panels.map((panel) => panel.getAttribute('hidden')),
    );
    const panel = panels[hidden.indexOf(null)];
    if (!panel) return null;
    const [labelledBy, text] = await parallel(() => [
      panel.getAttribute('aria-labelledby'),
      panel.text(),
    ]);
    const tabs = await this.tabs();
    const ids = await parallel(() => tabs.map((tab) => tab.getAttribute('id')));
    const labels = await this.getTabLabels();
    const i = ids.indexOf(labelledBy);
    return { tab: i < 0 ? null : labels[i], text };
  }

  private async tab(label: string): Promise<TestElement> {
    const [tabs, labels] = await parallel(() => [
      this.tabs(),
      this.getTabLabels(),
    ]);
    const i = labels.indexOf(label);
    if (i < 0) {
      throw Error(
        `No tab labelled "${label}"; the tabs are ${labels.join(', ')}.`,
      );
    }
    return tabs[i];
  }

  private async labelWhere(
    test: (tab: TestElement) => Promise<boolean>,
  ): Promise<string | null> {
    const [tabs, labels] = await parallel(() => [
      this.tabs(),
      this.getTabLabels(),
    ]);
    const hits = await parallel(() => tabs.map(test));
    const i = hits.indexOf(true);
    return i < 0 ? null : labels[i];
  }
}

/** `nav[lgTabNav]`: route tabs. Links are found by their words (without the count). */
export class LgTabNavHarness extends ComponentHarness {
  static hostSelector = 'nav.lg-tabs__list';

  static with(
    options: LgTabsHarnessFilters = {},
  ): HarnessPredicate<LgTabNavHarness> {
    return new HarnessPredicate(LgTabNavHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly links = this.locatorForAll('a.lg-tab');

  async getLabel(): Promise<string | null> {
    return (await this.host()).getAttribute('aria-label');
  }

  /** Every link's words, in order. */
  async getLinkLabels(): Promise<string[]> {
    const links = await this.links();
    return parallel(() =>
      links.map((link) => link.text({ exclude: '.lg-tab__count' })),
    );
  }

  /** The current route's link (`aria-current="page"`), or null. */
  async currentLabel(): Promise<string | null> {
    const [links, labels] = await parallel(() => [
      this.links(),
      this.getLinkLabels(),
    ]);
    const current = await parallel(() =>
      links.map((link) => link.getAttribute('aria-current')),
    );
    const i = current.indexOf('page');
    return i < 0 ? null : labels[i];
  }
}
