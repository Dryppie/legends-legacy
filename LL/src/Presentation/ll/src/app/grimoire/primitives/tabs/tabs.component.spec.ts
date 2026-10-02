import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_TABS } from './tabs.component';
import { LgTabNavHarness, LgTabsHarness } from '../../testing/tabs.harness';

@Component({
  imports: [...LG_TABS],
  template: `
    <lg-tabs label="Inventory" level="secondary" [(selected)]="tab">
      <button lgTab key="all" [count]="12">All</button>
      <button lgTab key="weapons">Weapons</button>
      <button lgTab key="armor" [count]="0">Armor</button>
    </lg-tabs>

    <lg-tabs label="Archive" [(selected)]="section">
      <button lgTab key="creatures">Creatures</button>
      <button lgTab key="regions">Regions</button>
      <lg-tab-panel key="creatures">Every creature you have met.</lg-tab-panel>
      <lg-tab-panel key="regions">Every region you have walked.</lg-tab-panel>
    </lg-tabs>

    <nav lgTabNav label="Archive sections">
      <a lgTabLink href="#creatures" aria-current="page">Creatures</a>
      <a lgTabLink href="#regions" [count]="2">Regions</a>
    </nav>
  `,
})
class TabsHost {
  readonly tab = signal('all');
  readonly section = signal('creatures');
}

describe('LgTabsComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [TabsHost] });
    const fixture = TestBed.createComponent(TabsHost);
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const tabs = await loader.getHarness(
      LgTabsHarness.with({ label: 'Inventory' }),
    );
    const archive = await loader.getHarness(
      LgTabsHarness.with({ label: 'Archive' }),
    );
    const nav = await loader.getHarness(LgTabNavHarness);
    return { fixture, host: fixture.componentInstance, tabs, archive, nav };
  }

  it('starts with the selected tab selected and as its one tab stop', fakeAsync(async () => {
    const { tabs } = await setup();

    expect(await tabs.getTabLabels()).toEqual(['All', 'Weapons', 'Armor']);
    expect(await tabs.selectedLabel()).toBe('All');
    expect(await tabs.tabStopLabels()).toEqual(['All']);
  }));

  it('a click selects the tab and focuses it', fakeAsync(async () => {
    const { host, tabs } = await setup();

    await tabs.click('Weapons');

    expect(host.tab()).toBe('weapons');
    expect(await tabs.selectedLabel()).toBe('Weapons');
    expect(await tabs.focusedLabel()).toBe('Weapons');
    expect(await tabs.tabStopLabels()).toEqual(['Weapons']);
  }));

  it('Right moves focus to the next tab and selects it', fakeAsync(async () => {
    const { host, tabs } = await setup();
    await tabs.click('Weapons');

    await tabs.pressKey('ArrowRight');

    expect(host.tab()).toBe('armor');
    expect(await tabs.selectedLabel()).toBe('Armor');
    expect(await tabs.focusedLabel()).toBe('Armor');
    expect(await tabs.tabStopLabels()).toEqual(['Armor']);
  }));

  it('Right on the last tab wraps to the first', fakeAsync(async () => {
    const { host, tabs } = await setup();
    await tabs.click('Weapons');
    await tabs.pressKey('ArrowRight');

    await tabs.pressKey('ArrowRight');

    expect(host.tab()).toBe('all');
    expect(await tabs.selectedLabel()).toBe('All');
    expect(await tabs.focusedLabel()).toBe('All');
    expect(await tabs.tabStopLabels()).toEqual(['All']);
  }));

  it('Left on the first tab wraps to the last; End and Home jump; Down does nothing', fakeAsync(async () => {
    const { host, tabs } = await setup();
    await tabs.click('All');

    await tabs.pressKey('ArrowLeft');
    expect(host.tab()).toBe('armor');
    await tabs.pressKey('Home');
    expect(host.tab()).toBe('all');
    await tabs.pressKey('End');
    expect(host.tab()).toBe('armor');
    await tabs.pressKey('ArrowDown');

    expect(host.tab()).toBe('armor');
    expect(await tabs.focusedLabel()).toBe('Armor');
  }));

  it('shows the selected tab’s panel, labelled by it, and the tab names its panel', fakeAsync(async () => {
    const { fixture, archive } = await setup();

    expect(await archive.getShownPanel()).toEqual({
      tab: 'Creatures',
      text: 'Every creature you have met.',
    });
    await archive.click('Regions');
    expect(await archive.getShownPanel()).toEqual({
      tab: 'Regions',
      text: 'Every region you have walked.',
    });

    const tab = fixture.nativeElement.querySelector(
      'lg-tabs:nth-of-type(2) [role=tab]',
    ) as HTMLElement;
    const panel = document.getElementById(tab.getAttribute('aria-controls')!);
    expect(panel?.getAttribute('role')).toBe('tabpanel');
  }));

  it('a strip without panels controls nothing', fakeAsync(async () => {
    const { fixture } = await setup();
    const tab = fixture.nativeElement.querySelector(
      '[role=tab]',
    ) as HTMLElement;

    expect(tab.hasAttribute('aria-controls')).toBeFalse();
    expect(tab.getAttribute('type')).toBe('button');
  }));

  it('route tabs are links named by the nav; the current one is the one marked aria-current', fakeAsync(async () => {
    const { nav } = await setup();

    expect(await nav.getLabel()).toBe('Archive sections');
    expect(await nav.getLinkLabels()).toEqual(['Creatures', 'Regions']);
    expect(await nav.currentLabel()).toBe('Creatures');
  }));
});
