import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
} from '@angular/cdk/testing';

export interface LgObjectiveHarnessFilters extends BaseHarnessFilters {
  /** The quest's title: its heading. */
  heading?: string | RegExp;
}

/** The pinned quest (lg-objective): its summary, and the tracker it opens in a Popover when it has one. */
export class LgObjectiveHarness extends ComponentHarness {
  static hostSelector = 'lg-objective';

  static with(
    options: LgObjectiveHarnessFilters = {},
  ): HarnessPredicate<LgObjectiveHarness> {
    return new HarnessPredicate(LgObjectiveHarness, options).addOption(
      'heading',
      options.heading,
      (harness, heading) =>
        HarnessPredicate.stringMatches(harness.getHeading(), heading),
    );
  }

  private readonly summary = this.locatorFor('.lg-objective__summary');

  /** The tracker while it is open: a Popover on the CDK overlay, named by the summary's aria-controls. */
  private async panel() {
    const id = await (await this.summary()).getAttribute('aria-controls');
    return id
      ? this.documentRootLocatorFactory().locatorForOptional(`[id="${id}"]`)()
      : null;
  }

  async getHeading(): Promise<string> {
    return (await this.locatorFor('.lg-objective__title')()).text();
  }

  /** The current objective's text and its count ("Defeat wolves", "3 / 5"). */
  async getObjective(): Promise<string | null> {
    const text = await this.locatorForOptional('.lg-objective__text')();
    return text ? text.text() : null;
  }

  async getCount(): Promise<string | null> {
    const count = await this.locatorForOptional('.lg-objective__count')();
    return count ? count.text() : null;
  }

  /** Whether the summary is a button that opens a tracker. */
  async isDisclosure(): Promise<boolean> {
    return (await this.summary()).matchesSelector('button');
  }

  /** A pointer press on the summary: focus moves to it, then it is clicked (the tracker then takes focus). */
  async press(): Promise<void> {
    const summary = await this.summary();
    await summary.focus();
    await summary.click();
  }

  /** A key pressed on the summary (it takes focus first). */
  async pressKey(key: TestKey | string): Promise<void> {
    await (await this.summary()).sendKeys(key);
  }

  /** Whether the tracker is showing. */
  async isOpen(): Promise<boolean> {
    return !!(await this.panel());
  }

  /** The summary's aria-expanded. */
  async isExpanded(): Promise<boolean> {
    return (
      (await (await this.summary()).getAttribute('aria-expanded')) === 'true'
    );
  }

  async isFocused(): Promise<boolean> {
    return (await this.summary()).isFocused();
  }

  /** The tracker's accessible name (a non-modal dialog), or null while it is closed. */
  async getPanelLabel(): Promise<string | null> {
    const panel = await this.panel();
    return panel ? panel.getAttribute('aria-label') : null;
  }
}
