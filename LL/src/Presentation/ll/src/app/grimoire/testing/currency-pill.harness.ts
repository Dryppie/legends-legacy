import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';

export interface LgCurrencyPillHarnessFilters extends BaseHarnessFilters {
  /** The currency ("Cinders"), as text or a pattern. */
  name?: string | RegExp;
}

/** A Grimoire CurrencyPill (`lg-currency-pill`); a short pill is a button that toggles 12.5k and 12,480. */
export class LgCurrencyPillHarness extends ComponentHarness {
  static hostSelector = 'lg-currency-pill';

  static with(
    options: LgCurrencyPillHarnessFilters = {},
  ): HarnessPredicate<LgCurrencyPillHarness> {
    return new HarnessPredicate(LgCurrencyPillHarness, options).addOption(
      'name',
      options.name,
      (h, name) => HarnessPredicate.stringMatches(h.getName(), name),
    );
  }

  // The pill: a button when short or interactive, else a plain box.
  private readonly control = this.locatorFor('.lg-currency');
  private readonly amount = this.locatorFor('.lg-currency__amount');
  private readonly name = this.locatorFor('.lg-currency__name');

  /** The amount as shown: "12.5k" or "12,480". */
  async getShownAmount(): Promise<string> {
    return (await this.amount()).text();
  }

  async getName(): Promise<string> {
    return (await this.name()).text();
  }

  /** What screen readers hear: the full amount and name ("12,480 Cinders"), whatever is shown. */
  async getAccessibleName(): Promise<string> {
    return (await this.control()).text({ exclude: '[aria-hidden="true"]' });
  }

  /** The tooltip: the full amount and name, or the `title` given. */
  async getTooltip(): Promise<string | null> {
    return (await this.control()).getAttribute('title');
  }

  /** The width, in characters, kept for the amount, so the TopBar does not reflow as it changes. */
  async getReservedWidth(): Promise<number> {
    const width = (
      await (await this.amount()).getProperty<CSSStyleDeclaration>('style')
    ).minWidth;
    return parseFloat(width) || 0;
  }

  /** A click, tap or key press: toggles a short pill's format, or emits `activate`. */
  async press(): Promise<void> {
    return (await this.control()).click();
  }
}
