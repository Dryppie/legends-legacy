import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { lgDescriptionOf } from './tip.harness';

export interface LgSigilHarnessFilters extends BaseHarnessFilters {
  /** The stat's name beside the hex ("Int"), as text or a pattern. */
  label?: string | RegExp;
}

/** A Grimoire Sigil (`lg-sigil`), the hex stat badge; with `interactive` it is a toggle button. */
export class LgSigilHarness extends ComponentHarness {
  static hostSelector = 'lg-sigil';

  static with(
    options: LgSigilHarnessFilters = {},
  ): HarnessPredicate<LgSigilHarness> {
    return new HarnessPredicate(LgSigilHarness, options).addOption(
      'label',
      options.label,
      (h, label) => HarnessPredicate.stringMatches(h.getLabel(), label),
    );
  }

  // The badge: a button when interactive, else a plain box.
  private readonly control = this.locatorFor('.lg-sigil');
  private readonly value = this.locatorFor('.lg-sigil__value');
  private readonly label = this.locatorForOptional('.lg-sigil__label');

  async getValue(): Promise<string> {
    return (await this.value()).text();
  }

  /** The stat's name beside the hex, or null when it shows none. */
  async getLabel(): Promise<string | null> {
    return (await this.label())?.text() ?? null;
  }

  /** Its accessible name: "Int 9", with ", can be raised" when ready. */
  async getAccessibleName(): Promise<string | null> {
    return (await this.control()).getAttribute('aria-label');
  }

  /** A click or tap. */
  async press(): Promise<void> {
    return (await this.control()).click();
  }

  async hover(): Promise<void> {
    return (await this.control()).hover();
  }

  async mouseAway(): Promise<void> {
    return (await this.control()).mouseAway();
  }

  async focus(): Promise<void> {
    return (await this.control()).focus();
  }

  async isFocused(): Promise<boolean> {
    return (await this.control()).isFocused();
  }

  /** Locked: it stays focusable, aria-disabled, and gives its unlock condition. */
  async isBlocked(): Promise<boolean> {
    return (
      (await (await this.control()).getAttribute('aria-disabled')) === 'true'
    );
  }

  /** Whether the toggle is pressed (selected); null when it is not a toggle (not interactive, or locked). */
  async isPressed(): Promise<boolean | null> {
    const pressed = await (await this.control()).getAttribute('aria-pressed');
    return pressed == null ? null : pressed === 'true';
  }

  /** What screen readers hear after its name: a locked Sigil's condition ("Locked. Unlocks at level 30"), or null. */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.control(),
      this.documentRootLocatorFactory(),
    );
  }
}
