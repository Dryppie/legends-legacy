import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { lgDescriptionOf } from './tip.harness';

export interface LgItemSlotHarnessFilters extends BaseHarnessFilters {
  /** The name under the frame, as text or a pattern. */
  name?: string | RegExp;
}

/** A Grimoire ItemSlot (`[lgItemSlot]`): a toggle button, a link or a box. */
export class LgItemSlotHarness extends ComponentHarness {
  static hostSelector = '[lgItemSlot]';

  static with(
    options: LgItemSlotHarnessFilters = {},
  ): HarnessPredicate<LgItemSlotHarness> {
    return new HarnessPredicate(LgItemSlotHarness, options).addOption(
      'name',
      options.name,
      (h, name) => HarnessPredicate.stringMatches(h.getName(), name),
    );
  }

  // The slot is its host: a button, a link or a box.
  private readonly control = () => this.host();
  private readonly name = this.locatorForOptional('.lg-slot__name');
  private readonly printedReason = this.locatorForOptional('.lg-slot__reason');
  private readonly printedWord = this.locatorForOptional(
    '.lg-slot__reason .lg-slot__word',
  );

  /** The name printed under the frame, or null without a caption. */
  async getName(): Promise<string | null> {
    return (await this.name())?.text() ?? null;
  }

  /** A blocked slot's reason as printed under its name ("Clear floor 10"), or null when nothing is printed. */
  async getPrintedReason(): Promise<string | null> {
    const el = await this.printedReason();
    return el ? el.text({ exclude: '.lg-sr, .lg-slot__word' }) : null;
  }

  /** The state's word printed above the reason ("Locked"), or null. */
  async getPrintedWord(): Promise<string | null> {
    return (await this.printedWord())?.text() ?? null;
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

  /** Blocked (locked, unavailable, restricted, insufficient, on cooldown): it stays focusable, aria-disabled, and says why. */
  async isBlocked(): Promise<boolean> {
    return (
      (await (await this.control()).getAttribute('aria-disabled')) === 'true'
    );
  }

  /** Whether the toggle is pressed (selected); null when it is not a toggle (not a button, or blocked). */
  async isPressed(): Promise<boolean | null> {
    const pressed = await (await this.control()).getAttribute('aria-pressed');
    return pressed == null ? null : pressed === 'true';
  }

  /** What screen readers hear after its name: a blocked slot's reason ("Locked. Clear floor 10"), or null. */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.control(),
      this.documentRootLocatorFactory(),
    );
  }
}
