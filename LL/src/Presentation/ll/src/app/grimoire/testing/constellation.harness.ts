import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgConstellationHarnessFilters extends BaseHarnessFilters {
  /** The group's accessible name ("Attributes" unless `label` is set). */
  label?: string | RegExp;
}

/** The keys a player uses on a Constellation. */
export type LgConstellationKey =
  | 'ArrowLeft'
  | 'ArrowRight'
  | 'ArrowUp'
  | 'ArrowDown'
  | 'Home'
  | 'End'
  | 'Enter';

const KEYS: Record<LgConstellationKey, TestKey> = {
  ArrowLeft: TestKey.LEFT_ARROW,
  ArrowRight: TestKey.RIGHT_ARROW,
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Enter: TestKey.ENTER,
};

/** `lg-constellation`: the stat star chart. Its Sigils are found by their label ("Strength"). */
export class LgConstellationHarness extends ComponentHarness {
  static hostSelector = 'lg-constellation';

  static with(
    options: LgConstellationHarnessFilters = {},
  ): HarnessPredicate<LgConstellationHarness> {
    return new HarnessPredicate(LgConstellationHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly group = this.locatorFor('.lg-constellation');
  private readonly sigils = this.locatorForAll(
    '.lg-constellation__node .lg-sigil',
  );
  private readonly labels = this.locatorForAll(
    '.lg-constellation__node .lg-sigil__label',
  );

  async getLabel(): Promise<string | null> {
    return (await this.group()).getAttribute('aria-label');
  }

  /** Every Sigil's label, in order. */
  async getSigilLabels(): Promise<string[]> {
    const labels = await this.labels();
    return parallel(() => labels.map((label) => label.text()));
  }

  /** A click on the Sigil: the press focuses it first, as a pointer press does in the browser. */
  async click(label: string): Promise<void> {
    const sigil = await this.sigil(label);
    if (!(await sigil.isFocused())) await sigil.focus();
    await sigil.click();
  }

  /** The pointer moves onto the Sigil. */
  async hover(label: string): Promise<void> {
    await (await this.sigil(label)).hover();
  }

  /** The pointer moves off the Sigil. */
  async mouseAway(label: string): Promise<void> {
    await (await this.sigil(label)).mouseAway();
  }

  /** A key pressed while focus is on a Sigil. Enter also presses it, as the browser does for a button. */
  async pressKey(key: LgConstellationKey): Promise<void> {
    const sigils = await this.sigils();
    const focused = await parallel(() =>
      sigils.map((sigil) => sigil.isFocused()),
    );
    const sigil = sigils[focused.indexOf(true)];
    if (!sigil) throw Error('No Sigil has focus.');
    await sigil.sendKeys(KEYS[key]);
    if (key === 'Enter') await sigil.click();
  }

  /** The Sigil that has focus, or null. */
  async focusedLabel(): Promise<string | null> {
    return (await this.labelsWhere((sigil) => sigil.isFocused()))[0] ?? null;
  }

  /** The selected (pressed) Sigils. */
  async selectedLabels(): Promise<string[]> {
    return this.labelsWhere(
      async (sigil) => (await sigil.getAttribute('aria-pressed')) === 'true',
    );
  }

  /** The Sigils in the Tab order: the chart is one tab stop, so there should be exactly one. */
  async tabStopLabels(): Promise<string[]> {
    return this.labelsWhere(
      async (sigil) => (await sigil.getAttribute('tabindex')) === '0',
    );
  }

  /** Locked: unavailable, but still focusable. */
  async isLocked(label: string): Promise<boolean> {
    const sigil = await this.sigil(label);
    return (await sigil.getAttribute('aria-disabled')) === 'true';
  }

  private async sigil(label: string): Promise<TestElement> {
    const [sigils, labels] = await parallel(() => [
      this.sigils(),
      this.getSigilLabels(),
    ]);
    const i = labels.indexOf(label);
    if (i < 0) {
      throw Error(
        `No Sigil labelled "${label}"; the chart has ${labels.join(', ')}.`,
      );
    }
    return sigils[i];
  }

  private async labelsWhere(
    test: (sigil: TestElement) => Promise<boolean>,
  ): Promise<string[]> {
    const [sigils, labels] = await parallel(() => [
      this.sigils(),
      this.getSigilLabels(),
    ]);
    const hits = await parallel(() => sigils.map(test));
    return labels.filter((_, i) => hits[i]);
  }
}
