import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LG_SHELL, LgShellApi } from '../../core/grimoire-core';
import { LgPageComponent } from './page.component';

@Component({
  imports: [LgPageComponent],
  template: `
    <div style="position: relative; width: 60rem; height: 20rem">
      <lg-page
        label="Leaderboard"
        role="region"
        [flow]="flow()"
        [maxWidth]="maxWidth()"
      >
        <p>Standings</p>
        <div class="lg-aside">
          <div>Main</div>
          <div>Side</div>
        </div>
      </lg-page>
    </div>
  `,
})
class PageHost {
  readonly flow = signal(false);
  readonly maxWidth = signal<string | undefined>(undefined);
}

describe('LgPageComponent', () => {
  function setup(inShell = false) {
    if (inShell) {
      TestBed.configureTestingModule({
        providers: [{ provide: LG_SHELL, useValue: {} as LgShellApi }],
      });
    }
    const fixture = TestBed.createComponent(PageHost);
    fixture.detectChanges();
    const page = (
      fixture.nativeElement as HTMLElement
    ).querySelector<HTMLElement>('lg-page')!;
    const inner = page.querySelector<HTMLElement>('.lg-page__inner')!;
    return { fixture, page, inner };
  }

  it('is the box itself: the scrolling region, named by its label', () => {
    const { page, inner } = setup();
    const style = getComputedStyle(page);
    expect(page.classList).toContain('lg-page');
    expect(style.display).toBe('block');
    expect(style.position).toBe('absolute');
    expect(style.overflowY).toBe('auto');
    expect(style.containerName).toBe('lg-region');
    expect(page.getAttribute('aria-label')).toBe('Leaderboard');
    expect(page.getAttribute('role')).toBe('region');
    expect(inner.textContent).toContain('Standings');
  });

  it('centres its content at page-max, or at maxWidth', () => {
    const { fixture, inner } = setup();
    expect(getComputedStyle(inner).maxWidth).toBe('1280px');
    fixture.componentInstance.maxWidth.set('36rem');
    fixture.detectChanges();
    expect(inner.style.maxWidth).toBe('36rem');
  });

  it('flow puts it in the normal flow, with no room for a TopBar and no gutters of its own', () => {
    const { fixture, page } = setup();
    fixture.componentInstance.flow.set(true);
    fixture.detectChanges();
    const style = getComputedStyle(page);
    expect(page.classList).toContain('lg-page--flow');
    expect(style.position).toBe('relative');
    expect(style.paddingTop).toBe('0px');
    expect(style.paddingLeft).toBe('0px');
  });

  it('outside a GameShell is not dense: track layouts keep their own gaps', () => {
    const { page } = setup();
    expect(page.classList).not.toContain('lg-page--dense');
    const aside = page.querySelector<HTMLElement>('.lg-aside')!;
    expect(getComputedStyle(aside).columnGap).toBe('32px');
  });

  it('inside a GameShell is dense (D-120): stack-lg apart, filling the stage to the gutter', () => {
    const { page, inner } = setup(true);
    expect(page.classList).toContain('lg-page--dense');
    const style = getComputedStyle(inner);
    expect(style.rowGap).toBe('16px');
    expect(style.maxWidth).toBe('none');
    const aside = page.querySelector<HTMLElement>('.lg-aside')!;
    expect(getComputedStyle(aside).columnGap).toBe('16px');
    expect(getComputedStyle(aside).rowGap).toBe('16px');
  });
});
