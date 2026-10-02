import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgTabStripHarnessFilters extends BaseHarnessFilters {
  /** The tablist's accessible name. */
  label?: string | RegExp;
}

/** The keys a player uses on a TabStrip. */
export type LgTabStripKey = 'ArrowLeft' | 'ArrowRight';

const KEYS: Record<LgTabStripKey, TestKey> = {
  ArrowLeft: TestKey.LEFT_ARROW,
  ArrowRight: TestKey.RIGHT_ARROW,
};

/** `lg-tab-strip`: the tabs. Tabs are found by their label (without the count). */
export class LgTabStripHarness extends ComponentHarness {
  static hostSelector = 'lg-tab-strip';

  static with(
    options: LgTabStripHarnessFilters = {},
  ): HarnessPredicate<LgTabStripHarness> {
    return new HarnessPredicate(LgTabStripHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly tablist = this.locatorFor('[role=tablist]');
  private readonly tabs = this.locatorForAll('[role=tab]');

  async getLabel(): Promise<string | null> {
    return (await this.tablist()).getAttribute('aria-label');
  }

  /** Every tab's label, in order. */
  async getTabLabels(): Promise<string[]> {
    const tabs = await this.tabs();
    return parallel(() =>
      tabs.map((tab) => tab.text({ exclude: '.lg-tabs__count' })),
    );
  }

  /** A click on the tab: the press focuses it first, as a pointer press does in the browser. */
  async click(label: string): Promise<void> {
    const tab = await this.tab(label);
    if (!(await tab.isFocused())) await tab.focus();
    await tab.click();
  }

  /** A key pressed while focus is on a tab. */
  async pressKey(key: LgTabStripKey): Promise<void> {
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

  private async tab(label: string): Promise<TestElement> {
    const [tabs, labels] = await parallel(() => [
      this.tabs(),
      this.getTabLabels(),
    ]);
    const i = labels.indexOf(label);
    if (i < 0) {
      throw Error(
        `No tab labelled "${label}"; the strip has ${labels.join(', ')}.`,
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
