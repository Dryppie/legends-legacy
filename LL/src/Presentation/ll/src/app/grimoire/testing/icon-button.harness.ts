import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { lgDescriptionOf } from './tip.harness';

export interface LgIconButtonHarnessFilters extends BaseHarnessFilters {
  /** Its name, the action in words ("Close"), as text or a pattern. */
  label?: string | RegExp;
}

/** An Icon button (`<button lgIconButton>`, `<a lgIconButton>`): a common action shown by its icon alone. */
export class LgIconButtonHarness extends ComponentHarness {
  static hostSelector = '.lg-iconbtn';

  static with(
    options: LgIconButtonHarnessFilters = {},
  ): HarnessPredicate<LgIconButtonHarness> {
    return new HarnessPredicate(LgIconButtonHarness, options).addOption(
      'label',
      options.label,
      (h, label) => HarnessPredicate.stringMatches(h.getLabel(), label),
    );
  }

  /** Its accessible name, which its tooltip shows too. */
  async getLabel(): Promise<string> {
    return (await (await this.host()).getAttribute('aria-label')) ?? '';
  }

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

  async isFocused(): Promise<boolean> {
    return (await this.host()).isFocused();
  }

  /** A toggle: true or false; null when it is a plain action. */
  async isPressed(): Promise<boolean | null> {
    const pressed = await (await this.host()).getAttribute('aria-pressed');
    return pressed == null ? null : pressed === 'true';
  }

  /** Blocked: focusable, aria-disabled, and it says why. */
  async isBlocked(): Promise<boolean> {
    return (await (await this.host()).getAttribute('aria-disabled')) === 'true';
  }

  /** Plain disabled: out of the Tab order. */
  async isDisabled(): Promise<boolean> {
    return (
      (await (
        await this.host()
      ).getProperty<boolean | undefined>('disabled')) === true
    );
  }

  /** What screen readers hear after its name: a blocked one's reason, or null. */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.host(),
      this.documentRootLocatorFactory(),
    );
  }
}
