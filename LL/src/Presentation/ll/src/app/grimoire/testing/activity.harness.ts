import { ComponentHarness } from '@angular/cdk/testing';

/** A Grimoire Activity (`lg-activity`), the current action at the head of the rail; with `interactive` it is a button. */
export class LgActivityHarness extends ComponentHarness {
  static hostSelector = 'lg-activity';

  // The block: a button when interactive, else a plain box.
  private readonly control = this.locatorFor('.lg-activity');
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
