import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';

export interface LgTableHarnessFilters extends BaseHarnessFilters {
  /** Its caption, as text or a pattern. */
  caption?: string | RegExp;
}

/** A Table (`table[lgTable]`): its caption, its column heads and its rows' cells, as the player reads them. */
export class LgTableHarness extends ComponentHarness {
  static hostSelector = 'table.lg-table';

  static with(
    options: LgTableHarnessFilters = {},
  ): HarnessPredicate<LgTableHarness> {
    return new HarnessPredicate(LgTableHarness, options).addOption(
      'caption',
      options.caption,
      (h, caption) => HarnessPredicate.stringMatches(h.getCaption(), caption),
    );
  }

  async getCaption(): Promise<string> {
    const caption = await this.locatorForOptional('caption')();
    return (await caption?.text())?.trim() ?? '';
  }

  /** The column heads' words. */
  async getHeaders(): Promise<string[]> {
    const heads = await this.locatorForAll('thead th')();
    return Promise.all(heads.map(async (h) => (await h.text()).trim()));
  }

  /** Each body row's cells' words. */
  async getRows(): Promise<string[][]> {
    const rows = await this.locatorForAll('tbody tr')();
    const cells = await this.locatorForAll('tbody tr > *')();
    const counts = await Promise.all(
      rows.map(async (r) => Number(await r.getProperty('childElementCount'))),
    );
    const texts = await Promise.all(
      cells.map(async (c) => (await c.text()).trim()),
    );
    let at = 0;
    return counts.map((n) => texts.slice(at, (at += n)));
  }

  /** 'compact', 'standard' or 'comfortable' when it is pinned to one; null when it follows its region. */
  async getDensity(): Promise<string | null> {
    return (await this.host()).getAttribute('data-density');
  }

  async isZebra(): Promise<boolean> {
    return (await this.host()).hasClass('lg-table--zebra');
  }
}
