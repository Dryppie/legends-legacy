import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
} from '@angular/cdk/testing';

export interface LgPopoverTriggerHarnessFilters extends BaseHarnessFilters {
  /** The trigger's words, as text or a pattern. */
  text?: string | RegExp;
}

/**
 * An element that opens a Popover (`[lgPopoverTrigger]`), and the popover it opens on the CDK overlay (read through
 * the document root).
 */
export class LgPopoverTriggerHarness extends ComponentHarness {
  static hostSelector = '[aria-haspopup="dialog"][aria-expanded]';

  static with(
    options: LgPopoverTriggerHarnessFilters = {},
  ): HarnessPredicate<LgPopoverTriggerHarness> {
    return new HarnessPredicate(LgPopoverTriggerHarness, options).addOption(
      'text',
      options.text,
      async (h, text) =>
        HarnessPredicate.stringMatches(
          (await (await h.host()).text()).trim(),
          text,
        ),
    );
  }

  async isOpen(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-expanded')) === 'true';
  }

  /** A press on the trigger (it takes focus, as a click does). */
  async press(): Promise<void> {
    const host = await this.host();
    await host.focus();
    await host.click();
  }

  async isFocused(): Promise<boolean> {
    return (await this.host()).isFocused();
  }

  /** The open popover's name, or null while it is closed. */
  async getPopoverLabel(): Promise<string | null> {
    return (await (await this.popover())?.getAttribute('aria-label')) ?? null;
  }

  async getPopoverText(): Promise<string | null> {
    return (await (await this.popover())?.text())?.trim() ?? null;
  }

  /** Whether focus is in the open popover. */
  async hasFocusInside(): Promise<boolean> {
    const popover = await this.popover();
    return !!popover && (await popover.matchesSelector(':focus-within'));
  }

  /** Escape, from where focus is. */
  async pressEscape(): Promise<void> {
    const popover = await this.popover();
    const focused = popover
      ? await this.documentRootLocatorFactory().locatorForOptional(
          `[id="${await this.popoverId()}"] :focus, [id="${await this.popoverId()}"]:focus`,
        )()
      : null;
    await (focused ?? (await this.host())).sendKeys(TestKey.ESCAPE);
  }

  private async popoverId(): Promise<string | null> {
    return (await this.host()).getAttribute('aria-controls');
  }

  private async popover() {
    const id = await this.popoverId();
    return id
      ? this.documentRootLocatorFactory().locatorForOptional(`[id="${id}"]`)()
      : null;
  }
}
