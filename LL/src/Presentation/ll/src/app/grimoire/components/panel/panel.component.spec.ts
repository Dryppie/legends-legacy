import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgDensity } from '../../core/grimoire-core';
import { LgPanelHarness } from '../../testing/panel.harness';
import { LG_PANEL } from './panel.component';

@Component({
  imports: [...LG_PANEL],
  template: `
    <div style="--lg-inset: 16px; --lg-inset-inner: 12px">
      <lg-panel id="loot" [flush]="flush()" [density]="density()">
        <lg-panel-header [align]="align()">
          <lg-panel-title>Pending loot</lg-panel-title>
          <span class="count">4 items</span>
        </lg-panel-header>
        <p>Retreat to secure your pending loot.</p>
        <lg-panel id="inner">
          <lg-panel-header>
            <lg-panel-title>Inner</lg-panel-title>
          </lg-panel-header>
        </lg-panel>
      </lg-panel>
      <lg-panel id="bare"><p>Only a body.</p></lg-panel>
    </div>
  `,
})
class PanelHost {
  readonly flush = signal(false);
  readonly density = signal<LgDensity | undefined>(undefined);
  readonly align = signal<'start' | 'end'>('start');
}

describe('LgPanelComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(PanelHost);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const loader = TestbedHarnessEnvironment.loader(fixture);
    return {
      fixture,
      loader,
      panel: el.querySelector<HTMLElement>('#loot')!,
      inner: el.querySelector<HTMLElement>('#inner')!,
      bare: el.querySelector<HTMLElement>('#bare')!,
    };
  }

  it('is the box itself: the host takes the class and a block box, and no native tooltip', () => {
    const { panel } = setup();
    expect(panel.classList).toContain('lg-panel');
    expect(getComputedStyle(panel).display).toBe('block');
    expect(panel.hasAttribute('title')).toBeFalse();
  });

  it('is a region named by its title', async () => {
    const { panel, loader } = setup();
    const title = panel.querySelector('lg-panel-title')!;
    expect(panel.getAttribute('role')).toBe('region');
    expect(title.id).toMatch(/^lg-panel-title-\d+$/);
    expect(panel.getAttribute('aria-labelledby')).toBe(title.id);
    const harness = await loader.getHarness(
      LgPanelHarness.with({ title: 'Pending loot' }),
    );
    expect(await harness.getLabel()).toBe('Pending loot');
  });

  it('without a header is a plain box: the body alone, not a region', async () => {
    const { bare, loader } = setup();
    expect(bare.querySelector('lg-panel-header')).toBeNull();
    expect(bare.hasAttribute('role')).toBeFalse();
    expect(bare.hasAttribute('aria-labelledby')).toBeFalse();
    const panels = await loader.getAllHarnesses(LgPanelHarness);
    expect(await panels[2].getTitle()).toBeNull();
    expect(await panels[2].getBodyText()).toBe('Only a body.');
  });

  it('puts the header first, extras after the title at the end, and the content in the body', async () => {
    const { panel, loader } = setup();
    const header = panel.querySelector('lg-panel-header')!;
    expect(panel.firstElementChild).toBe(header);
    expect(header.nextElementSibling?.classList).toContain('lg-panel__body');
    const title = header.querySelector<HTMLElement>('lg-panel-title')!;
    expect(getComputedStyle(title).flexGrow).toBe('1');

    const harness = await loader.getHarness(
      LgPanelHarness.with({ title: 'Pending loot' }),
    );
    expect(await harness.getHeaderExtras()).toBe('4 items');
    expect(await harness.getBodyText()).toContain(
      'Retreat to secure your pending loot.',
    );
  });

  it('align="end" sets the title at the end of the head', () => {
    const { fixture, panel } = setup();
    const header = panel.querySelector<HTMLElement>('lg-panel-header')!;
    expect(header.hasAttribute('data-align')).toBeFalse();
    fixture.componentInstance.align.set('end');
    fixture.detectChanges();
    expect(header.getAttribute('data-align')).toBe('end');
    expect(getComputedStyle(header).textAlign).toBe('end');
  });

  it('pads the head and the body to its inset, and a Panel inside it one step in', () => {
    const { panel, inner } = setup();
    const body = panel.querySelector<HTMLElement>(':scope > .lg-panel__body')!;
    const head = panel.querySelector<HTMLElement>(':scope > lg-panel-header')!;
    expect(getComputedStyle(body).paddingTop).toBe('16px');
    expect(getComputedStyle(head).paddingLeft).toBe('16px');
    const innerBody = inner.querySelector<HTMLElement>('.lg-panel__body')!;
    expect(getComputedStyle(innerBody).paddingTop).toBe('12px');
  });

  it('flush drops the body padding and hands the inset to a list inside', async () => {
    const { fixture, panel, loader } = setup();
    fixture.componentInstance.flush.set(true);
    fixture.detectChanges();
    const body = panel.querySelector<HTMLElement>(':scope > .lg-panel__body')!;
    expect(panel.classList).toContain('lg-panel--flush');
    expect(getComputedStyle(body).paddingTop).toBe('0px');
    expect(
      getComputedStyle(body).getPropertyValue('--lg-list-pad').trim(),
    ).toBe('16px');
    const harness = await loader.getHarness(
      LgPanelHarness.with({ title: 'Pending loot' }),
    );
    expect(await harness.isFlush()).toBeTrue();
  });

  it('density sets data-density on the Panel', async () => {
    const { fixture, panel, loader } = setup();
    expect(panel.hasAttribute('data-density')).toBeFalse();
    fixture.componentInstance.density.set('compact');
    fixture.detectChanges();
    expect(panel.getAttribute('data-density')).toBe('compact');
    const harness = await loader.getHarness(
      LgPanelHarness.with({ title: 'Pending loot' }),
    );
    expect(await harness.getDensity()).toBe('compact');
  });
});
