import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { lgDescriptionOf } from './tip.harness';

export interface LgButtonHarnessFilters extends BaseHarnessFilters {
  /** The label the player sees, as text or a pattern. */
  label?: string | RegExp;
}

/** A Grimoire Button (`<button lgButton>`, `<a lgButton>`). */
export class LgButtonHarness extends ComponentHarness {
  static hostSelector = '.lg-btn';

  static with(
    options: LgButtonHarnessFilters = {},
  ): HarnessPredicate<LgButtonHarness> {
    return new HarnessPredicate(LgButtonHarness, options).addOption(
      'label',
      options.label,
      (h, label) => HarnessPredicate.stringMatches(h.getLabel(), label),
    );
  }

  // While pending, the label and the pending label share one cell and the hidden one is aria-hidden.
  private readonly label = this.locatorFor(
    '.lg-btn__label:not([aria-hidden="true"])',
  );

  /** The label the player sees and hears: the pending label ("Saving…") while it is pending. */
  async getLabel(): Promise<string> {
    return (await this.label()).text();
  }

  /** A click or tap. */
  async press(): Promise<void> {
    return (await this.host()).click();
  }

  async hover(): Promise<void> {
    return (await this.host()).hover();
  }

  async mouseAway(): Promise<void> {
    return (await this.host()).mouseAway();
  }

  async focus(): Promise<void> {
    return (await this.host()).focus();
  }

  async blur(): Promise<void> {
    return (await this.host()).blur();
  }

  async isFocused(): Promise<boolean> {
    return (await this.host()).isFocused();
  }

  /** Blocked (unavailable, locked, restricted, insufficient, on cooldown): it stays focusable, aria-disabled, and says why. */
  async isBlocked(): Promise<boolean> {
    const host = await this.host();
    return (
      (await host.getAttribute('aria-disabled')) === 'true' &&
      (await host.getAttribute('aria-busy')) !== 'true'
    );
  }

  /** Waiting on the server: busy, and a press does nothing. */
  async isPending(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-busy')) === 'true';
  }

  /** Plain disabled (rare): out of the Tab order. */
  async isDisabled(): Promise<boolean> {
    return (
      (await (
        await this.host()
      ).getProperty<boolean | undefined>('disabled')) === true
    );
  }

  /** What screen readers hear after its name: a blocked Button's reason ("Locked. Unlocks at level 20"), or null. */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.host(),
      this.documentRootLocatorFactory(),
    );
  }
}
