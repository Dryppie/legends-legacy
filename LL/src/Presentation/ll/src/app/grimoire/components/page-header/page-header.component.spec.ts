import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LG_SHELL, LgShellApi } from '../../core/grimoire-core';
import { LG_PAGE_HEADER } from './page-header.component';

@Component({
  imports: [...LG_PAGE_HEADER],
  template: `
    <div class="lg-region" style="width: 60rem">
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        heading="Overview"
        summary="Stats, combat rating, and Essence loadout"
      >
        <lg-page-header-actions>
          <button type="button">Search</button>
          <button type="button">Refresh</button>
        </lg-page-header-actions>
      </lg-page-header>
    </div>
  `,
})
class PageHeaderHost {}

describe('LgPageHeaderComponent', () => {
  function setup(inShell = false) {
    if (inShell) {
      TestBed.configureTestingModule({
        providers: [{ provide: LG_SHELL, useValue: {} as LgShellApi }],
      });
    }
    const fixture = TestBed.createComponent(PageHeaderHost);
    fixture.detectChanges();
    const header = (
      fixture.nativeElement as HTMLElement
    ).querySelector<HTMLElement>('lg-page-header')!;
    const q = <T extends Element = HTMLElement>(sel: string) =>
      header.querySelector<T>(sel)!;
    return { header, q };
  }

  it('is the box itself: a flex row closed by a hairline, with no native tooltip', () => {
    const { header } = setup();
    const style = getComputedStyle(header);
    expect(header.classList).toContain('lg-pagehead');
    expect(style.display).toBe('flex');
    expect(style.borderBottomStyle).toBe('solid');
    expect(header.hasAttribute('title')).toBeFalse();
  });

  it('sets the heading as the screen title, under its eyebrow and over its summary', () => {
    const { q } = setup();
    const h1 = q('h1');
    expect(h1.textContent?.trim()).toBe('Overview');
    expect(h1.classList).toContain('lg-heading--screen');
    expect(getComputedStyle(h1).fontSize).toBe('36px');
    expect(q('.lg-pagehead__eyebrow').textContent?.trim()).toBe('Character');
    expect(q('.lg-pagehead__summary').textContent?.trim()).toBe(
      'Stats, combat rating, and Essence loadout',
    );
  });

  it('draws the section icon in a diamond, hidden from screen readers', () => {
    const { q } = setup();
    const icon = q('.lg-pagehead__icon');
    expect(icon.getAttribute('aria-hidden')).toBe('true');
    expect(icon.querySelector('polygon')).not.toBeNull();
    const glyph = q<SVGElement>('.lg-pagehead__glyph svg.lg-icon');
    expect(getComputedStyle(glyph).width).toBe('24px');
    expect(getComputedStyle(icon).width).toBe('52px');
  });

  it('puts the actions region last, at the end of the row', () => {
    const { header } = setup();
    const actions = header.lastElementChild as HTMLElement;
    expect(actions.tagName.toLowerCase()).toBe('lg-page-header-actions');
    expect(actions.classList).toContain('lg-pagehead__actions');
    expect(getComputedStyle(actions).display).toBe('flex');
    expect(getComputedStyle(actions).flexWrap).toBe('nowrap');
    expect(actions.querySelectorAll('button').length).toBe(2);
  });

  it('outside a GameShell shows its eyebrow and summary', () => {
    const { header, q } = setup();
    expect(header.classList).not.toContain('lg-pagehead--dense');
    expect(getComputedStyle(q('.lg-pagehead__eyebrow')).display).not.toBe(
      'none',
    );
    expect(getComputedStyle(q('.lg-pagehead__summary')).display).not.toBe(
      'none',
    );
  });

  it('inside a GameShell is one dense row (D-120): no eyebrow or summary, a smaller mark and title', () => {
    const { header, q } = setup(true);
    expect(header.classList).toContain('lg-pagehead--dense');
    expect(getComputedStyle(q('.lg-pagehead__eyebrow')).display).toBe('none');
    expect(getComputedStyle(q('.lg-pagehead__summary')).display).toBe('none');
    expect(getComputedStyle(q('.lg-pagehead__icon')).width).toBe('36px');
    expect(getComputedStyle(q('.lg-pagehead__glyph svg.lg-icon')).width).toBe(
      '18px',
    );
    expect(getComputedStyle(q('h1')).fontSize).toBe('24px');
    expect(getComputedStyle(header).paddingBottom).toBe('12px');
  });
});
