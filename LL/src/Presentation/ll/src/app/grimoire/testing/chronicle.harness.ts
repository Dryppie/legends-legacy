import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestElement,
  TestKey,
} from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';

export interface LgChronicleHarnessFilters extends BaseHarnessFilters {
  /** The region's name ("Chronicle"). */
  label?: string | RegExp;
}

/** The Chronicle (lg-chronicle): channels, the log, the collapsed ticker, and the floating drawer's grip and tall toggle. */
export class LgChronicleHarness extends ComponentHarness {
  static hostSelector = 'lg-chronicle';

  static with(
    options: LgChronicleHarnessFilters = {},
  ): HarnessPredicate<LgChronicleHarness> {
    return new HarnessPredicate(LgChronicleHarness, options).addOption(
      'label',
      options.label,
      (harness, label) =>
        HarnessPredicate.stringMatches(harness.getLabel(), label),
    );
  }

  private readonly tabs = this.locatorForAll('[role=tab]');
  private readonly log = this.locatorForOptional('.lg-chronicle__log');
  private readonly ticker = this.locatorForOptional('.lg-chronicle__ticker');
  private readonly collapseToggle = this.locatorForOptional(
    '.lg-chronicle__collapse',
  );
  private readonly jumpButton = this.locatorForOptional('.lg-chronicle__jump');
  private readonly grip = this.locatorForOptional('.lg-chronicle__grip');
  private readonly tallToggle = this.locatorForOptional('.lg-chronicle__tall');
  private readonly authorButtons = this.locatorForAll(
    '.lg-chronicle__log button.lg-chronicle__author',
  );

  async getLabel(): Promise<string | null> {
    return (await this.host()).getAttribute('aria-label');
  }

  /* ---------- Channels ---------- */

  /** The channel tabs' labels, without their unread counts. */
  async getChannels(): Promise<string[]> {
    return Promise.all(
      (await this.tabs()).map((tab) => this.channelLabel(tab)),
    );
  }

  /** A pointer press on a channel tab: focus moves to it, then it is clicked. */
  async pressChannel(label: string): Promise<void> {
    await press(await this.tabByLabel(label));
  }

  /** A key pressed on the channel tab that has focus (or on the tab stop, if none has). */
  async pressChannelKey(key: TestKey | string): Promise<void> {
    const tabs = await this.tabs();
    const focused = await firstMatching(tabs, (tab) => tab.isFocused());
    const target =
      focused ??
      (await firstMatching(
        tabs,
        async (tab) => (await tab.getAttribute('tabindex')) === '0',
      ));
    if (!target) throw Error('The Chronicle shows no channel tabs.');
    await target.sendKeys(key);
  }

  /** The channel whose tab is selected (aria-selected), or null when the channels are not shown. */
  async getSelectedChannel(): Promise<string | null> {
    const tab = await firstMatching(
      await this.tabs(),
      async (t) => (await t.getAttribute('aria-selected')) === 'true',
    );
    return tab ? this.channelLabel(tab) : null;
  }

  /** The channel tabs in the Tab order (tabindex="0"): roving focus keeps exactly one. */
  async getChannelTabStops(): Promise<string[]> {
    const stops: string[] = [];
    for (const tab of await this.tabs()) {
      if ((await tab.getAttribute('tabindex')) === '0')
        stops.push(await this.channelLabel(tab));
    }
    return stops;
  }

  /** The channel whose tab has focus, or null. */
  async getFocusedChannel(): Promise<string | null> {
    const tab = await firstMatching(await this.tabs(), (t) => t.isFocused());
    return tab ? this.channelLabel(tab) : null;
  }

  /* ---------- Open, collapsed ---------- */

  /** Whether the Chronicle is open: its channels and log show. Collapsed, it is a one-line ticker. */
  async isOpen(): Promise<boolean> {
    return !!(await this.log());
  }

  async hasCollapseToggle(): Promise<boolean> {
    return !!(await this.collapseToggle());
  }

  /** Presses the collapse and expand button. */
  async pressCollapseToggle(): Promise<void> {
    const toggle = await this.collapseToggle();
    if (!toggle) throw Error('The Chronicle has no collapse toggle.');
    await press(toggle);
  }

  /** The collapse and expand button's name ("Collapse chat"), or null without one. */
  async getCollapseToggleLabel(): Promise<string | null> {
    const toggle = await this.collapseToggle();
    return toggle ? toggle.getAttribute('aria-label') : null;
  }

  async isCollapseToggleExpanded(): Promise<boolean> {
    const toggle = await this.collapseToggle();
    return !!toggle && (await toggle.getAttribute('aria-expanded')) === 'true';
  }

  async isCollapseToggleFocused(): Promise<boolean> {
    const toggle = await this.collapseToggle();
    return !!toggle && (await toggle.isFocused());
  }

  /** The collapsed ticker's name ("Open chat, 2 unread"), or null while open. */
  async getTickerLabel(): Promise<string | null> {
    const ticker = await this.ticker();
    return ticker ? ticker.getAttribute('aria-label') : null;
  }

  /** Presses the collapsed ticker, which opens the Chronicle. */
  async pressTicker(): Promise<void> {
    const ticker = await this.ticker();
    if (!ticker) throw Error('The Chronicle is not collapsed.');
    await press(ticker);
  }

  /* ---------- The log ---------- */

  /** The text of each line in the log, in order. */
  async getLines(): Promise<string[]> {
    const lines = await this.locatorForAll('.lg-chronicle__log > li')();
    return Promise.all(lines.map((line) => line.text()));
  }

  /** The authors shown as buttons (with `authorActions`), in order. */
  async getAuthorButtons(): Promise<string[]> {
    return Promise.all((await this.authorButtons()).map((b) => b.text()));
  }

  /** A pointer press on an author's name. With several lines by the same author, the first. */
  async pressAuthor(name: string): Promise<void> {
    const button = await firstMatching(
      await this.authorButtons(),
      async (b) => (await b.text()) === name,
    );
    if (!button) throw Error(`No author button "${name}".`);
    await press(button);
  }

  async isAuthorFocused(name: string): Promise<boolean> {
    const button = await firstMatching(
      await this.authorButtons(),
      async (b) => (await b.text()) === name,
    );
    return !!button && (await button.isFocused());
  }

  /** The log's aria-live: 'polite' when it is itself the live region, 'off' otherwise. */
  async getLogLiveMode(): Promise<string | null> {
    return (await this.requiredLog()).getAttribute('aria-live');
  }

  /** Whether the log shows its newest line (scrolled to its foot). */
  async isLogAtFoot(): Promise<boolean> {
    const log = await this.requiredLog();
    const [height, top, client] = await Promise.all([
      log.getProperty<number>('scrollHeight'),
      log.getProperty<number>('scrollTop'),
      log.getProperty<number>('clientHeight'),
    ]);
    return height - top - client < 2;
  }

  async getLogScrollTop(): Promise<number> {
    return (await this.requiredLog()).getProperty<number>('scrollTop');
  }

  /** Whether the log overflows, so that there is somewhere to scroll. */
  async canLogScroll(): Promise<boolean> {
    const log = await this.requiredLog();
    return (
      (await log.getProperty<number>('scrollHeight')) >
      (await log.getProperty<number>('clientHeight'))
    );
  }

  /**
   * The player scrolls the log back to its first line. TestElement has no way to scroll, so this reaches the element
   * (TestBed only) and sends the scroll event at once rather than on the next frame.
   */
  async scrollLogToTop(): Promise<void> {
    const log = TestbedHarnessEnvironment.getNativeElement(
      await this.requiredLog(),
    );
    log.scrollTop = 0;
    log.dispatchEvent(new Event('scroll'));
    await this.forceStabilize();
  }

  async isLogFocused(): Promise<boolean> {
    return (await this.requiredLog()).isFocused();
  }

  /** The "3 new lines" control's text, or null while nothing waits. */
  async getNewLinesLabel(): Promise<string | null> {
    const jump = await this.jumpButton();
    return jump ? jump.text() : null;
  }

  /** The "3 new lines" control's accessible name ("3 new lines, jump to latest"), or null. */
  async getNewLinesName(): Promise<string | null> {
    const jump = await this.jumpButton();
    return jump ? jump.getAttribute('aria-label') : null;
  }

  /** Presses "N new lines": the log jumps to the latest. */
  async jumpToLatest(): Promise<void> {
    const jump = await this.jumpButton();
    if (!jump) throw Error('No new lines are waiting.');
    await press(jump);
  }

  /* ---------- The floating drawer ---------- */

  async hasGrip(): Promise<boolean> {
    return !!(await this.grip());
  }

  /** A pointer press on the drag grip (no drag): focus moves to it. */
  async pressGrip(): Promise<void> {
    await press(await this.requiredGrip());
  }

  /** A key pressed on the drag grip: the arrow keys nudge the drawer. */
  async pressGripKey(key: TestKey | string): Promise<void> {
    await (await this.requiredGrip()).sendKeys(key);
  }

  async isGripFocused(): Promise<boolean> {
    const grip = await this.grip();
    return !!grip && (await grip.isFocused());
  }

  async hasTallToggle(): Promise<boolean> {
    return !!(await this.tallToggle());
  }

  async pressTallToggle(): Promise<void> {
    const tall = await this.tallToggle();
    if (!tall)
      throw Error(
        'The Chronicle has no tall toggle (it is not a floating drawer).',
      );
    await press(tall);
  }

  /** Whether the drawer has been made taller (the tall toggle's aria-pressed). */
  async isTall(): Promise<boolean> {
    const tall = await this.tallToggle();
    return !!tall && (await tall.getAttribute('aria-pressed')) === 'true';
  }

  async getTallToggleLabel(): Promise<string | null> {
    const tall = await this.tallToggle();
    return tall ? tall.getAttribute('aria-label') : null;
  }

  async isTallToggleFocused(): Promise<boolean> {
    const tall = await this.tallToggle();
    return !!tall && (await tall.isFocused());
  }

  private async requiredLog(): Promise<TestElement> {
    const log = await this.log();
    if (!log) throw Error('The Chronicle is collapsed: it shows no log.');
    return log;
  }

  private async requiredGrip(): Promise<TestElement> {
    const grip = await this.grip();
    if (!grip)
      throw Error(
        'The Chronicle has no drag grip (it is not a floating drawer in a shell).',
      );
    return grip;
  }

  private async channelLabel(tab: TestElement): Promise<string> {
    return tab.text({ exclude: '.lg-chronicle__unread' });
  }

  private async tabByLabel(label: string): Promise<TestElement> {
    const tab = await firstMatching(
      await this.tabs(),
      async (t) => (await this.channelLabel(t)) === label,
    );
    if (!tab) throw Error(`No channel "${label}".`);
    return tab;
  }
}

/** A pointer press: focus moves to the element, then it is clicked. */
async function press(element: TestElement): Promise<void> {
  await element.focus();
  await element.click();
}

async function firstMatching(
  elements: TestElement[],
  test: (element: TestElement) => Promise<boolean>,
): Promise<TestElement | null> {
  for (const element of elements) {
    if (await test(element)) return element;
  }
  return null;
}
