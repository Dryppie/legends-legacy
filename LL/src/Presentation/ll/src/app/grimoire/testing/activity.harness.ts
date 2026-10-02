import { ComponentHarness } from '@angular/cdk/testing';

/** A Grimoire Activity (`[lgActivity]`), the current action at the head of the rail: a button, a link or a box. */
export class LgActivityHarness extends ComponentHarness {
  static hostSelector = '[lgActivity]';

  // The block is its host.
  private readonly control = () => this.host();
  private readonly label = this.locatorFor('.lg-activity__label');
  private readonly remaining = this.locatorForOptional('.lg-activity__time');

  /** The action ("Engaged in Combat", "Idle"). */
  async getLabel(): Promise<string> {
    return (await this.label()).text();
  }

  /** The time left as printed ("00:12"), or null. */
  async getRemaining(): Promise<string | null> {
    return (await this.remaining())?.text() ?? null;
  }

  /** A click or tap: goes to the action. */
  async press(): Promise<void> {
    return (await this.control()).click();
  }
}
