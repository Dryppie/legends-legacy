import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  LocatorFactory,
  TestElement,
} from '@angular/cdk/testing';

export interface LgReasonTipHarnessFilters extends BaseHarnessFilters {
  /** Only the tip while it is shown (true) or hidden (false). */
  shown?: boolean;
  /** The reason it gives, as text or a pattern. */
  reason?: string | RegExp;
}

/** A control the reason tip belongs to: anything the pointer can move onto and off. */
export interface LgReasonTipOwner {
  hover(): Promise<void>;
  mouseAway(): Promise<void>;
}

/**
 * The reason tip (Standards · States · The reason tip): the one float on the page in which a blocked control says why.
 * It is drawn on the body, so load it from `TestbedHarnessEnvironment.documentRootLoader(fixture)`. It exists once a
 * control has first shown it; `getHarnessOrNull(LgReasonTipHarness.with({ shown: true }))` is null while no tip shows.
 */
export class LgReasonTipHarness extends ComponentHarness {
  static hostSelector = '.lg-why';

  static with(
    options: LgReasonTipHarnessFilters = {},
  ): HarnessPredicate<LgReasonTipHarness> {
    return new HarnessPredicate(LgReasonTipHarness, options)
      .addOption(
        'shown',
        options.shown,
        async (h, shown) => (await h.isShown()) === shown,
      )
      .addOption('reason', options.reason, (h, reason) =>
        HarnessPredicate.stringMatches(h.getReason(), reason),
      );
  }

  private readonly word = this.locatorForOptional('.lg-why__word');
  private readonly title = this.locatorForOptional('.lg-why__title');
  private readonly reasonLines = this.locatorForAll('.lg-why__reason');

  async isShown(): Promise<boolean> {
    return (await this.host()).hasClass('is-shown');
  }

  /** The state's word above the reason ("Locked"), or null when the state has none. */
  async getWord(): Promise<string | null> {
    return (await this.word())?.text() ?? null;
  }

  /** The reason, one line per line ("Unlocks at level 20"). */
  async getReason(): Promise<string> {
    const lines = await this.reasonLines();
    return (await Promise.all(lines.map((l) => l.text()))).join('\n');
  }

  /** The control's name, when the tip names it (the compact rail), or null. */
  async getTitle(): Promise<string | null> {
    return (await this.title())?.text() ?? null;
  }

  /** Whether the reason is a shortfall, set in warning. */
  async isWarning(): Promise<boolean> {
    return (await this.host()).hasClass('lg-why--warning');
  }

  /**
   * Whether a press pinned the tip to `control`. Pinned is not in the markup, so this checks what it means: the pointer
   * leaves the control (a hover tip closes after its short grace, a pinned one stays), then comes back over it, which
   * shows a hover tip again. Pass the harness of the control that opened the tip.
   */
  async isPinned(control: LgReasonTipOwner): Promise<boolean> {
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
 * Closes the reason tip if a test left it open, as a press elsewhere on the page does. The tip is one element for the
 * whole page and it hears Escape before any layer, so a tip left open changes the next test: call this in `afterEach`.
 */
export function lgCloseReasonTip(): void {
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
