import {
  BaseHarnessFilters,
  ComponentHarness,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { LgDensity } from '../core/grimoire-core';

export interface LgPanelHarnessFilters extends BaseHarnessFilters {
  /** The Panel's title. */
  title?: string | RegExp;
}

/** `lg-panel`, the content box. Panels are found by their title. */
export class LgPanelHarness extends ComponentHarness {
  static hostSelector = 'lg-panel';

  static with(
    options: LgPanelHarnessFilters = {},
  ): HarnessPredicate<LgPanelHarness> {
    return new HarnessPredicate(LgPanelHarness, options).addOption(
      'title',
      options.title,
      (harness, title) =>
        HarnessPredicate.stringMatches(harness.getTitle(), title),
    );
  }

  private readonly header = this.locatorForOptional(':scope > lg-panel-header');
  private readonly title = this.locatorForOptional(
    ':scope > lg-panel-header > lg-panel-title',
  );
  private readonly body = this.locatorFor(':scope > .lg-panel__body');

  /** The title, or null for a Panel without one. */
  async getTitle(): Promise<string | null> {
    const title = await this.title();
    return title ? (await title.text()).trim() : null;
  }

  /** What the header holds after the title: a count, a Tag, a link. Empty without a header. */
  async getHeaderExtras(): Promise<string> {
    const [header, title] = await Promise.all([this.header(), this.getTitle()]);
    if (!header) return '';
    const text = (await header.text()).trim();
    return (title && text.startsWith(title) ? text.slice(title.length) : text)
      .replace(/\s+/g, ' ')
      .trim();
  }

  /** The body's text. */
  async getBodyText(): Promise<string> {
    return (await (await this.body()).text()).replace(/\s+/g, ' ').trim();
  }

  /** The accessible name: the title, when the Panel is a region named by it. */
  async getLabel(): Promise<string | null> {
    const host = await this.host();
    const id = await host.getAttribute('aria-labelledby');
    if (!id || (await host.getAttribute('role')) !== 'region') return null;
    const title = await this.title();
    return title && (await title.getAttribute('id')) === id
      ? (await title.text()).trim()
      : null;
  }

  /** Whether the body drops its padding for a List or table. */
  async isFlush(): Promise<boolean> {
    return (await this.host()).hasClass('lg-panel--flush');
  }

  /** The density set on the Panel itself, or null when it follows the region's. */
  async getDensity(): Promise<LgDensity | null> {
    return (await (
      await this.host()
    ).getAttribute('data-density')) as LgDensity | null;
  }
}
