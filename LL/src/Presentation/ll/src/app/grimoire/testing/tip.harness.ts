import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  LocatorFactory,
  TestElement,
} from '@angular/cdk/testing';

export interface LgTipHarnessFilters extends BaseHarnessFilters {
  /** Only the tip while it is shown (true) or hidden (false). */
  shown?: boolean;
  /** What it says (a reason, a tooltip's text), as text or a pattern. */
  reason?: string | RegExp;
}

/** An element the tip belongs to: anything the pointer can move onto and off. */
export interface LgTipOwner {
  hover(): Promise<void>;
  mouseAway(): Promise<void>;
}

/**
 * The tip: the one float on the page in which a tooltip, a Ledger row's explanation or a blocked control's reason
 * (Standards · States · The reason tip) shows. It lives on the CDK overlay, so load it from
 * `TestbedHarnessEnvironment.documentRootLoader(fixture)`. It exists once something has first shown it;
 * `getHarnessOrNull(LgTipHarness.with({ shown: true }))` is null while no tip shows.
 */
export class LgTipHarness extends ComponentHarness {
  static hostSelector = '.lg-tip';

  static with(
    options: LgTipHarnessFilters = {},
  ): HarnessPredicate<LgTipHarness> {
    return new HarnessPredicate(LgTipHarness, options)
      .addOption(
        'shown',
        options.shown,
        async (h, shown) => (await h.isShown()) === shown,
      )
      .addOption('reason', options.reason, (h, reason) =>
        HarnessPredicate.stringMatches(h.getReason(), reason),
      );
  }

  private readonly word = this.locatorForOptional('.lg-tip__word');
  private readonly title = this.locatorForOptional('.lg-tip__title');
  private readonly meta = this.locatorForOptional('.lg-tip__meta');
  private readonly lines = this.locatorForAll('.lg-tip__line');

  async isShown(): Promise<boolean> {
    return (await this.host()).hasClass('is-shown');
  }

  /** The state's word above the reason ("Locked"), or null when the state has none. */
  async getWord(): Promise<string | null> {
    return (await this.word())?.text() ?? null;
  }

  /** What it says, one line per line: a reason ("Unlocks at level 20"), a tooltip's text, an explanation. */
  async getReason(): Promise<string> {
    const lines = await this.lines();
    return (await Promise.all(lines.map((l) => l.text()))).join('\n');
  }

  /** Its heading: the control's name (the compact rail) or what a row explains; null when it has none. */
  async getTitle(): Promise<string | null> {
    return (await this.title())?.text() ?? null;
  }

  /** Its footnote, or null. */
  async getMeta(): Promise<string | null> {
    return (await this.meta())?.text() ?? null;
  }

  /** Whether the reason is a shortfall, set in warning. */
  async isWarning(): Promise<boolean> {
    return (await this.host()).hasClass('lg-tip--warning');
  }

  /**
   * Whether a press pinned the tip to `control`. Pinned is not in the markup, so this checks what it means: the pointer
   * leaves the control (a hover tip closes after its short grace, a pinned one stays), then comes back over it, which
   * shows a hover tip again. Pass the harness of the control that opened the tip.
   */
  async isPinned(control: LgTipOwner): Promise<boolean> {
    if (!(await this.isShown())) return false;
    await control.mouseAway();
    const pinned = await this.isShown();
    await control.hover();
    return pinned;
  }

  /** Closes the tip as Escape does (the tip hears Escape before anything else on the page). Does nothing when hidden. */
  async close(): Promise<void> {
    if (!(await this.isShown())) return;
    await (await this.host()).dispatchEvent('keydown', { key: 'Escape' });
  }
}

/**
 * Closes the tip if a test left it open, as a press elsewhere on the page does. The tip is one element for the page and
 * it hears Escape before anything else, so a tip left open changes what Escape does next: call this in `afterEach`.
 */
export function lgCloseTip(): void {
  document.body.dispatchEvent(
    new PointerEvent('pointerdown', { bubbles: true }),
  );
}

/**
 * What screen readers hear as a control's description: the text of the elements its `aria-describedby` names,
 * without their aria-hidden parts (a named element counts even when it is aria-hidden itself). Null without one.
 */
export async function lgDescriptionOf(
  control: TestElement,
  documentRoot: LocatorFactory,
): Promise<string | null> {
  const ids =
    (await control.getAttribute('aria-describedby'))
      ?.split(/\s+/)
      .filter(Boolean) ?? [];
  if (!ids.length) return null;
  const texts = await Promise.all(
    ids.map(async (id) => {
      const el = await documentRoot.locatorForOptional(`[id="${id}"]`)();
      return el ? el.text({ exclude: '[aria-hidden="true"]' }) : '';
    }),
  );
  return texts.join(' ').replace(/\s+/g, ' ').trim();
}
