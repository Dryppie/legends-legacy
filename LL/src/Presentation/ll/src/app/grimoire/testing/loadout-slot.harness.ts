import { ComponentHarness } from '@angular/cdk/testing';
import { lgDescriptionOf } from './tip.harness';

/** A Grimoire LoadoutSlot (`lg-loadout-slot`), one Essence loadout slot; with `interactive` it is a button. */
export class LgLoadoutSlotHarness extends ComponentHarness {
  static hostSelector = 'lg-loadout-slot';

  // The slot: a button when interactive, else a plain box.
  private readonly control = this.locatorFor('.lg-loadout');
  private readonly name = this.locatorFor('.lg-loadout__name');
  private readonly tag = this.locatorForOptional('.lg-loadout__head lg-tag');

  /** The name line: the Essence, "Empty", or a locked slot's unlock condition. */
  async getName(): Promise<string> {
    return (await this.name()).text();
  }

  /** The state Tag's word ("Attuned", "Locked"), or null. */
  async getTag(): Promise<string | null> {
    return (await this.tag())?.text() ?? null;
  }

  /**
   * Its accessible name: its aria-label, or else the text it holds without the aria-hidden parts ("Slot 3", "Locked",
   * "Unlocks at level 20"). Words from neighbouring elements are not spaced apart, so match the parts.
   */
  async getAccessibleName(): Promise<string> {
    const control = await this.control();
    return (
      (await control.getAttribute('aria-label')) ??
      control.text({ exclude: '[aria-hidden="true"]' })
    );
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

  /** Locked: it stays focusable and aria-disabled, and a press announces its condition instead of acting. */
  async isBlocked(): Promise<boolean> {
    return (
      (await (await this.control()).getAttribute('aria-disabled')) === 'true'
    );
  }

  /** A description beyond its name, or null (a locked slot's condition is already in its name). */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.control(),
      this.documentRootLocatorFactory(),
    );
  }
}
