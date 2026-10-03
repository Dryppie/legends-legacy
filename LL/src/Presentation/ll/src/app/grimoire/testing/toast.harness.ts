import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
  TestKey,
} from '@angular/cdk/testing';

export interface LgToastHarnessFilters extends BaseHarnessFilters {
  /** What it says happened, as text or a pattern. */
  heading?: string | RegExp;
}

/**
 * A toast (`lg-toast`). The toaster's live in a stack on the CDK overlay, so load them from
 * `TestbedHarnessEnvironment.documentRootLoader(fixture)`; one leaving is still there until it has faded.
 */
export class LgToastHarness extends ComponentHarness {
  static hostSelector = 'lg-toast';

  static with(
    options: LgToastHarnessFilters = {},
  ): HarnessPredicate<LgToastHarness> {
    return new HarnessPredicate(LgToastHarness, options).addOption(
      'heading',
      options.heading,
      (h, heading) => HarnessPredicate.stringMatches(h.getHeading(), heading),
    );
  }

  private readonly heading = this.locatorFor('.lg-toast__heading');
  private readonly text = this.locatorForOptional('.lg-toast__text');
  private readonly action = this.locatorForOptional('.lg-toast__action');
  private readonly dismissButton = this.locatorFor('.lg-toast__dismiss');

  async getHeading(): Promise<string> {
    return (await (await this.heading()).text()).trim();
  }

  async getText(): Promise<string | null> {
    return (await (await this.text())?.text())?.trim() ?? null;
  }

  /** 'info', 'success', 'warning' or 'danger'. */
  async getTone(): Promise<string> {
    const cls = (await (await this.host()).getAttribute('class')) ?? '';
    return /lg-toast--(\w+)/.exec(cls)?.[1] ?? 'info';
  }

  async getActionLabel(): Promise<string | null> {
    return (await (await this.action())?.text())?.trim() ?? null;
  }

  async pressAction(): Promise<void> {
    const action = await this.action();
    if (!action) throw Error('This toast has no action.');
    await action.click();
  }

  async dismiss(): Promise<void> {
    await (await this.dismissButton()).click();
  }

  /** Fading out: dismissed, or its time ran out. */
  async isLeaving(): Promise<boolean> {
    return (await this.host()).hasClass('is-leaving');
  }

  async hover(): Promise<void> {
    await (await this.host()).hover();
  }

  async mouseAway(): Promise<void> {
    await (await this.host()).mouseAway();
  }

  /** Focuses its Dismiss button, as Tab does. */
  async focusDismiss(): Promise<void> {
    await (await this.dismissButton()).focus();
  }

  /** Escape while focus is in it. */
  async pressEscape(): Promise<void> {
    await (await this.dismissButton()).sendKeys(TestKey.ESCAPE);
  }
}
