import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
  parallel,
} from '@angular/cdk/testing';

export interface LgSegmentedHarnessFilters extends BaseHarnessFilters {
  /** Its label. */
  label?: string | RegExp;
}

/** The keys a player uses on a Segmented. */
export type LgSegmentedKey =
  | 'ArrowLeft'
  | 'ArrowRight'
  | 'ArrowUp'
  | 'ArrowDown'
  | 'Home'
  | 'End'
  | 'Space';

const KEYS: Record<LgSegmentedKey, TestKey | string> = {
  ArrowLeft: TestKey.LEFT_ARROW,
  ArrowRight: TestKey.RIGHT_ARROW,
  ArrowUp: TestKey.UP_ARROW,
  ArrowDown: TestKey.DOWN_ARROW,
  Home: TestKey.HOME,
  End: TestKey.END,
  Space: ' ',
};

/** A Segmented (`lg-segmented`): a radio group of joined segments, found by their words. */
export class LgSegmentedHarness extends ComponentHarness {
  static hostSelector = 'lg-segmented';

  static with(
    options: LgSegmentedHarnessFilters = {},
  ): HarnessPredicate<LgSegmentedHarness> {
    return new HarnessPredicate(LgSegmentedHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly segments = this.locatorForAll('lg-segment');

  async getLabel(): Promise<string> {
    return (
      await (await this.locatorFor('.lg-segmented__label')()).text()
    ).trim();
  }
  async getSegments(): Promise<string[]> {
    const segments = await this.segments();
    return parallel(() => segments.map(async (s) => (await s.text()).trim()));
  }
  /** The chosen segment's words, or null. */
  async getSelected(): Promise<string | null> {
    return (
      (
        await this.wordsWhere(
          async (s) => (await s.getAttribute('aria-checked')) === 'true',
        )
      )[0] ?? null
    );
  }
  /** The segment in the Tab order. */
  async getTabStop(): Promise<string | null> {
    return (
      (
        await this.wordsWhere(
          async (s) => (await s.getAttribute('tabindex')) === '0',
        )
      )[0] ?? null
    );
  }
  async getFocused(): Promise<string | null> {
    return (await this.wordsWhere((s) => s.isFocused()))[0] ?? null;
  }
  /** A press on the segment with these words: focus moves to it, then it is clicked. */
  async select(label: string): Promise<void> {
    const segment = await this.segment(label);
    await segment.focus();
    await segment.click();
  }
  /** A key pressed on the focused segment. */
  async pressKey(key: LgSegmentedKey): Promise<void> {
    const segments = await this.segments();
    const focused = await parallel(() => segments.map((s) => s.isFocused()));
    const segment = segments[focused.indexOf(true)];
    if (!segment) throw Error('No segment has focus.');
    await segment.sendKeys(KEYS[key]);
  }

  private async segment(label: string): Promise<TestElement> {
    const [segments, words] = await parallel(() => [
      this.segments(),
      this.getSegments(),
    ]);
    const i = words.indexOf(label);
    if (i < 0)
      throw Error(`No segment "${label}"; it has ${words.join(', ')}.`);
    return segments[i];
  }
  private async wordsWhere(
    test: (s: TestElement) => Promise<boolean>,
  ): Promise<string[]> {
    const [segments, words] = await parallel(() => [
      this.segments(),
      this.getSegments(),
    ]);
    const hits = await parallel(() => segments.map(test));
    return words.filter((_, i) => hits[i]);
  }
}
